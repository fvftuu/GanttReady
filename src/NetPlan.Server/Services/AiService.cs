using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace NetPlan.Server.Services;

public class AiOptions
{
    public const string Section = "Ai";
    public string Provider { get; set; } = "OpenAI";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public string ApiFormat { get; set; } = "openai";  // "openai" | "anthropic"
}

public class AiService : IAiService
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // 重试配置：429/502/503 自动重试 1 次，间隔 1 秒
    private static readonly int[] _retryStatusCodes = { 429, 502, 503 };
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(1);
    private const int _timeoutSec = 300; // AI 请求超时（秒）

    public AiService(HttpClient http, IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, ct);
        if (_retryStatusCodes.Contains((int)response.StatusCode))
        {
            response.Dispose();
            await Task.Delay(_retryDelay, ct);
            // 需要重新创建 request，因为 HttpRequestMessage 只能发送一次
            var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
            if (request.Content != null)
                retryRequest.Content = new StringContent(await request.Content.ReadAsStringAsync(ct), Encoding.UTF8, "application/json");
            foreach (var header in request.Headers)
                retryRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            response = await _http.SendAsync(retryRequest, ct);
        }
        return response;
    }

    private AiOptions ResolveOptions(AiOptions? overrideOptions)
    {
        if (overrideOptions != null && !string.IsNullOrEmpty(overrideOptions.Endpoint))
            return overrideOptions;
        return _options;
    }

    public async Task<string> ChatAsync(List<AiMessage> messages, double temperature = 0.3, AiOptions? overrideOptions = null)
    {
        var opts = ResolveOptions(overrideOptions);

        if (string.IsNullOrEmpty(opts.ApiKey) && opts.Provider != "Ollama")
            return "⚠️ 请先在分析页面的「AI 设置」中配置并保存 API 连接。";

        HttpRequestMessage request;
        var format = opts.ApiFormat?.ToLower() ?? "openai";

        if (format == "anthropic")
        {
            // Anthropic API 格式
            var anthropicBody = new
            {
                model = opts.Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                max_tokens = 2048
            };
            var json = JsonSerializer.Serialize(anthropicBody, _json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            request = new HttpRequestMessage(HttpMethod.Post, $"{opts.Endpoint.TrimEnd('/')}/messages")
            {
                Content = content
            };
            if (!string.IsNullOrEmpty(opts.ApiKey))
                request.Headers.Add("x-api-key", opts.ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
        }
        else
        {
            // OpenAI 兼容格式（默认）
            var openaiBody = new
            {
                model = opts.Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                temperature,
                max_tokens = 16384
            };
            var json = JsonSerializer.Serialize(openaiBody, _json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            request = new HttpRequestMessage(HttpMethod.Post, $"{opts.Endpoint.TrimEnd('/')}/chat/completions")
            {
                Content = content
            };
            if (!string.IsNullOrEmpty(opts.ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSec));
            var response = await SendWithRetryAsync(request, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
                return $"⚠️ AI 服务返回错误 ({response.StatusCode}): {body}";

            using var doc = JsonDocument.Parse(body);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var text = choice.GetProperty("message").GetProperty("content").GetString();
            return text?.Trim() ?? "（AI 返回为空）";
        }
        catch (OperationCanceledException)
        {
            return $"⚠️ AI 服务请求超时（{_timeoutSec}秒），请检查网络或 API 地址。";
        }
        catch (Exception ex)
        {
            return $"⚠️ AI 服务调用失败: {ex.Message}";
        }
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, double temperature = 0.3, AiOptions? overrideOptions = null)
    {
        return await ChatAsync(new List<AiMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userMessage }
        }, temperature, overrideOptions);
    }

    // ==================== 带工具调用的 Chat ====================

    public async Task<AiChatResult> ChatWithToolsAsync(List<AiMessage> messages, List<AiToolDefinition> tools, double temperature = 0.3, AiOptions? overrideOptions = null)
    {
        var opts = ResolveOptions(overrideOptions);

        if (string.IsNullOrEmpty(opts.ApiKey) && !string.Equals(opts.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
            return new AiChatResult { Text = "⚠️ 请先在分析页面的「AI 设置」中配置并保存 API 连接。" };

        var format = opts.ApiFormat?.ToLower() ?? "openai";

        if (format == "anthropic")
            return await ChatWithToolsAnthropic(messages, tools, opts, temperature);

        return await ChatWithToolsOpenAI(messages, tools, opts, temperature);
    }

    private Dictionary<string, object?> MsgToDict(AiMessage msg)
    {
        var d = new Dictionary<string, object?>
        {
            ["role"] = msg.Role
        };
        // DeepSeek/OpenAI 要求带 tool_calls 的消息不能有 content 字段（null 也不行）
        if (msg.ToolCalls == null || msg.ToolCalls.Count == 0)
            d["content"] = msg.Content;
        if (msg.ToolCalls?.Count > 0)
        {
            d["tool_calls"] = msg.ToolCalls.Select(tc => new
            {
                id = tc.Id,
                type = tc.Type,
                function = new { name = tc.FunctionName, arguments = tc.FunctionArguments }
            }).ToList();
        }
        if (!string.IsNullOrEmpty(msg.ToolCallId))
        {
            d["tool_call_id"] = msg.ToolCallId;
        }
        // reasoning_content（DeepSeek 思考模式/GLM 等）必须在后续请求中回传
        if (!string.IsNullOrEmpty(msg.ReasoningContent))
        {
            d["reasoning_content"] = msg.ReasoningContent;
        }
        return d;
    }

    private async Task<AiChatResult> ChatWithToolsOpenAI(List<AiMessage> messages, List<AiToolDefinition> tools, AiOptions opts, double temperature)
    {
        var toolDefs = tools.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.Parameters
            }
        }).ToList();

        var body = new Dictionary<string, object?>
        {
            ["model"] = opts.Model,
            ["messages"] = messages.Select(MsgToDict).ToList(),
            ["tools"] = toolDefs,
            ["temperature"] = temperature
        };

        var json = JsonSerializer.Serialize(body, _json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{opts.Endpoint.TrimEnd('/')}/chat/completions")
        {
            Content = content
        };
        if (!string.IsNullOrEmpty(opts.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSec));
            var response = await SendWithRetryAsync(request, cts.Token);
            var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
                return new AiChatResult { Text = $"⚠️ AI 服务返回错误 ({response.StatusCode}): {responseBody}" };

            using var doc = JsonDocument.Parse(responseBody);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            // 获取 reasoning_content（GLM 等模型的 thinking 模式，回传时必须保留）
            var reasoningContent = message.TryGetProperty("reasoning_content", out var rcEl) ? rcEl.GetString() : null;

            // 检查 tool_calls
            if (message.TryGetProperty("tool_calls", out var toolCallsEl) && toolCallsEl.GetArrayLength() > 0)
            {
                var calls = new List<AiToolCall>();
                foreach (var tc in toolCallsEl.EnumerateArray())
                {
                    var fn = tc.GetProperty("function");
                    var tcId = tc.TryGetProperty("id", out var idEl)
                        ? idEl.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(tcId))
                        tcId = "call_fallback_" + Guid.NewGuid().ToString("N")[..12];
                    calls.Add(new AiToolCall
                    {
                        Id = tcId,
                        Type = tc.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "function" : "function",
                        FunctionName = fn.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "",
                        FunctionArguments = fn.TryGetProperty("arguments", out var argsEl) ? argsEl.GetString() ?? "{}" : "{}"
                    });
                }
                return new AiChatResult
                {
                    ToolCalls = calls,
                    ReasoningContent = reasoningContent
                };
            }

            var text = message.GetProperty("content").GetString();
            return new AiChatResult { Text = text?.Trim() ?? "（AI 返回为空）" };
        }
        catch (OperationCanceledException)
        {
            return new AiChatResult { Text = $"⚠️ AI 服务请求超时（{_timeoutSec}秒），请检查网络或 API 地址。" };
        }
        catch (Exception ex)
        {
            return new AiChatResult { Text = $"⚠️ AI 服务调用失败: {ex.Message}" };
        }
    }

    public async Task<AiChatResult> ChatWithToolsStreamAsync(List<AiMessage> messages, List<AiToolDefinition> tools, Action<string> onChunk, double temperature = 0.3, AiOptions? overrideOptions = null)
    {
        var opts = ResolveOptions(overrideOptions);
        if (string.IsNullOrEmpty(opts.ApiKey) && !string.Equals(opts.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
            return new AiChatResult { Text = "⚠️ 请先在分析页面的「AI 设置」中配置并保存 API 连接。" };

        var format = opts.ApiFormat?.ToLower() ?? "openai";
        if (format == "anthropic")
        {
            // Anthropic 暂不支持流式工具调用，降级为非流式
            return await ChatWithToolsAsync(messages, tools, temperature, opts);
        }

        return await ChatWithToolsStreamOpenAI(messages, tools, opts, temperature, onChunk);
    }

    private async Task<AiChatResult> ChatWithToolsStreamOpenAI(List<AiMessage> messages, List<AiToolDefinition> tools, AiOptions opts, double temperature, Action<string> onChunk)
    {
        var toolDefs = tools.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.Parameters
            }
        }).ToList();

        var body = new Dictionary<string, object?>
        {
            ["model"] = opts.Model,
            ["messages"] = messages.Select(MsgToDict).ToList(),
            ["tools"] = toolDefs,
            ["temperature"] = temperature,
            ["stream"] = true
        };

        var json = JsonSerializer.Serialize(body, _json);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{opts.Endpoint.TrimEnd('/')}/chat/completions")
        {
            Content = httpContent
        };
        if (!string.IsNullOrEmpty(opts.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSec));
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                return new AiChatResult { Text = $"⚠️ AI 服务返回错误 ({response.StatusCode}): {errorBody}" };
            }

            using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new StreamReader(stream);

            var fullText = new StringBuilder();
            var reasoningContent = new StringBuilder();
            var toolCallsAccumulator = new Dictionary<int, (string Id, string Name, StringBuilder Args)>();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cts.Token);
                if (line == null) break;
                if (!line.StartsWith("data: ")) continue;

                var data = line[6..];
                if (data == "[DONE]") break;

                using var doc = JsonDocument.Parse(data);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0) continue;

                var delta = choices[0].GetProperty("delta");

                // Reasoning content（DeepSeek 思考模式，必须在后续请求中回传）
                if (delta.TryGetProperty("reasoning_content", out var rcEl) && rcEl.ValueKind == JsonValueKind.String)
                {
                    reasoningContent.Append(rcEl.GetString());
                }

                // Text content
                if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                {
                    var chunk = contentEl.GetString() ?? "";
                    fullText.Append(chunk);
                    onChunk(chunk);
                }

                // Tool calls
                if (delta.TryGetProperty("tool_calls", out var toolCallsEl))
                {
                    foreach (var tc in toolCallsEl.EnumerateArray())
                    {
                        var index = tc.GetProperty("index").GetInt32();

                        if (!toolCallsAccumulator.ContainsKey(index))
                        {
                            var id = tc.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                            if (string.IsNullOrEmpty(id))
                                id = "call_fallback_" + Guid.NewGuid().ToString("N")[..12];
                            var name = tc.TryGetProperty("function", out var fnEl) && fnEl.TryGetProperty("name", out var nameEl)
                                ? nameEl.GetString() ?? "" : "";
                            toolCallsAccumulator[index] = (id, name, new StringBuilder());
                        }

                        if (tc.TryGetProperty("function", out var fn))
                        {
                            if (fn.TryGetProperty("arguments", out var argEl) && argEl.ValueKind == JsonValueKind.String)
                            {
                                toolCallsAccumulator[index].Args.Append(argEl.GetString());
                            }
                        }
                    }
                }
            }

            // 如果有工具调用，返回工具调用结果
            if (toolCallsAccumulator.Count > 0)
            {
                var calls = toolCallsAccumulator.OrderBy(kv => kv.Key).Select(kv => new AiToolCall
                {
                    Id = kv.Value.Id,
                    Type = "function",
                    FunctionName = kv.Value.Name,
                    FunctionArguments = kv.Value.Args.ToString()
                }).ToList();

                return new AiChatResult
                {
                    ToolCalls = calls,
                    Text = fullText.Length > 0 ? fullText.ToString() : null,
                    ReasoningContent = reasoningContent.Length > 0 ? reasoningContent.ToString() : null
                };
            }

            return new AiChatResult
            {
                Text = fullText.Length > 0 ? fullText.ToString().Trim() : "（AI 返回为空）",
                ReasoningContent = reasoningContent.Length > 0 ? reasoningContent.ToString() : null
            };
        }
        catch (OperationCanceledException)
        {
            return new AiChatResult { Text = $"⚠️ AI 服务请求超时（{_timeoutSec}秒），请检查网络或 API 地址。" };
        }
        catch (Exception ex)
        {
            return new AiChatResult { Text = $"⚠️ AI 服务调用失败: {ex.Message}" };
        }
    }

    private async Task<AiChatResult> ChatWithToolsAnthropic(List<AiMessage> messages, List<AiToolDefinition> tools, AiOptions opts, double temperature)
    {
        // Anthropic Tool Use API
        // 文档：https://docs.anthropic.com/en/docs/build-with-claude/tool-use

        // 1. 提取 system prompt，转换消息格式
        string? systemPrompt = null;
        var anthropicMessages = new List<object>();

        var i = 0;
        while (i < messages.Count)
        {
            var msg = messages[i];

            if (msg.Role == "system")
            {
                systemPrompt = msg.Content;
                i++;
                continue;
            }

            if (msg.Role == "tool")
            {
                // Anthropic 要求 tool result 合并为一条 user 消息，content 为数组
                var contentBlocks = new List<object>();
                while (i < messages.Count && messages[i].Role == "tool")
                {
                    var toolMsg = messages[i];
                    contentBlocks.Add(new
                    {
                        type = "tool_result",
                        tool_use_id = toolMsg.ToolCallId ?? "",
                        content = toolMsg.Content
                    });
                    i++;
                }
                anthropicMessages.Add(new { role = "user", content = contentBlocks });
                continue;
            }

            if (msg.Role == "assistant" && msg.ToolCalls?.Count > 0)
            {
                // Assistant 带工具调用 → content 数组（text + 多个 tool_use）
                var contentBlocks = new List<object>();
                if (!string.IsNullOrEmpty(msg.Content))
                    contentBlocks.Add(new { type = "text", text = msg.Content });
                foreach (var tc in msg.ToolCalls)
                {
                    object? input;
                    try { input = JsonSerializer.Deserialize<object>(tc.FunctionArguments); }
                    catch { input = tc.FunctionArguments; }
                    contentBlocks.Add(new
                    {
                        type = "tool_use",
                        id = tc.Id,
                        name = tc.FunctionName,
                        input
                    });
                }
                anthropicMessages.Add(new { role = "assistant", content = contentBlocks });
                i++;
                continue;
            }

            // 普通 user / assistant 消息
            anthropicMessages.Add(new { role = msg.Role, content = msg.Content ?? "" });
            i++;
        }

        // 2. 转换工具定义为 Anthropic 格式
        var anthropicTools = tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = t.Parameters
        }).ToList();

        // 3. 构建请求体
        var body = new Dictionary<string, object?>
        {
            ["model"] = opts.Model,
            ["max_tokens"] = 4096,
            ["messages"] = anthropicMessages,
            ["tools"] = anthropicTools,
            ["tool_choice"] = new { type = "auto" }
        };

        if (!string.IsNullOrEmpty(systemPrompt))
            body["system"] = systemPrompt;

        var json = JsonSerializer.Serialize(body, _json);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{opts.Endpoint.TrimEnd('/')}/messages")
        {
            Content = httpContent
        };
        if (!string.IsNullOrEmpty(opts.ApiKey))
            request.Headers.Add("x-api-key", opts.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        // 4. 发送请求并解析响应
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSec));
            var response = await SendWithRetryAsync(request, cts.Token);
            var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
                return new AiChatResult { Text = $"⚠️ Anthropic API 返回错误 ({response.StatusCode}): {responseBody}" };

            using var doc = JsonDocument.Parse(responseBody);
            var contentArray = doc.RootElement.GetProperty("content");

            string? text = null;
            var toolCalls = new List<AiToolCall>();

            foreach (var block in contentArray.EnumerateArray())
            {
                var type = block.GetProperty("type").GetString();
                if (type == "text")
                {
                    text = block.GetProperty("text").GetString();
                }
                else if (type == "tool_use")
                {
                    toolCalls.Add(new AiToolCall
                    {
                        Id = block.GetProperty("id").GetString() ?? "",
                        Type = "function",
                        FunctionName = block.GetProperty("name").GetString() ?? "",
                        FunctionArguments = block.GetProperty("input").GetRawText()
                    });
                }
            }

            if (toolCalls.Count > 0)
            {
                return new AiChatResult
                {
                    ToolCalls = toolCalls,
                    Text = text
                };
            }

            return new AiChatResult { Text = text?.Trim() ?? "（AI 返回为空）" };
        }
        catch (OperationCanceledException)
        {
            return new AiChatResult { Text = $"⚠️ Anthropic API 请求超时（{_timeoutSec}秒），请检查网络或 API 地址。" };
        }
        catch (Exception ex)
        {
            return new AiChatResult { Text = $"⚠️ Anthropic API 调用失败: {ex.Message}" };
        }
    }
}
