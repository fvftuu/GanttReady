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

    public AiService(HttpClient http, IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;
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
            return "⚠️ AI 服务未配置。请点击「AI 设置」填写 API 地址和密钥。";

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
                temperature
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
            var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"⚠️ AI 服务返回错误 ({response.StatusCode}): {body}";

            using var doc = JsonDocument.Parse(body);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var text = choice.GetProperty("message").GetProperty("content").GetString();
            return text?.Trim() ?? "（AI 返回为空）";
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
            return new AiChatResult { Text = "⚠️ AI 服务未配置。请点击「AI 设置」填写 API 地址和密钥。" };

        var format = opts.ApiFormat?.ToLower() ?? "openai";

        if (format == "anthropic")
            return await ChatWithToolsAnthropic(messages, tools, opts, temperature);

        return await ChatWithToolsOpenAI(messages, tools, opts, temperature);
    }

    private Dictionary<string, object?> MsgToDict(AiMessage msg)
    {
        var d = new Dictionary<string, object?>
        {
            ["role"] = msg.Role,
            ["content"] = msg.Content
        };
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
            ["temperature"] = temperature,
            ["parallel_tool_calls"] = false
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
            var response = await _http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

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
                    calls.Add(new AiToolCall
                    {
                        Id = tc.GetProperty("id").GetString() ?? "",
                        Type = tc.GetProperty("type").GetString() ?? "function",
                        FunctionName = fn.GetProperty("name").GetString() ?? "",
                        FunctionArguments = fn.GetProperty("arguments").GetString() ?? "{}"
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
        catch (Exception ex)
        {
            return new AiChatResult { Text = $"⚠️ AI 服务调用失败: {ex.Message}" };
        }
    }

    private async Task<AiChatResult> ChatWithToolsAnthropic(List<AiMessage> messages, List<AiToolDefinition> tools, AiOptions opts, double temperature)
    {
        // Anthropic tools 格式不同，暂降级为普通对话
        var text = await ChatAsync(messages, temperature, opts);
        return new AiChatResult { Text = text };
    }
}
