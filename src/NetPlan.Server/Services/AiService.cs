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
}
