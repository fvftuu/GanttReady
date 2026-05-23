namespace NetPlan.Server.Services;

/// <summary>
/// AI 服务 —— 调用 LLM 实现自然语言交互
/// </summary>
public interface IAiService
{
    /// <summary>
    /// 通用 AI 调用。可传入覆盖配置（来自用户界面设置）。
    /// </summary>
    Task<string> ChatAsync(List<AiMessage> messages, double temperature = 0.3, AiOptions? overrideOptions = null);

    /// <summary>
    /// 快捷方法：单条 system prompt + 用户输入
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, string userMessage, double temperature = 0.3, AiOptions? overrideOptions = null);

    /// <summary>
    /// 带工具调用（function calling）的 AI 对话
    /// </summary>
    Task<AiChatResult> ChatWithToolsAsync(List<AiMessage> messages, List<AiToolDefinition> tools, double temperature = 0.3, AiOptions? overrideOptions = null);
}

public class AiMessage
{
    public string Role { get; set; } = "user";  // system / user / assistant / tool
    public string Content { get; set; } = "";

    /// <summary>仅 assistant 消息使用：AI 调用的工具列表</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<AiToolCall>? ToolCalls { get; set; }

    /// <summary>仅 tool 消息使用：对应哪个工具调用</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>GLM/DeepSeek 等模型的推理过程内容（thinking/reasoning），回传时必须保留</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningContent { get; set; }
}

/// <summary>AI 返回结果：可能是文本，也可能是工具调用</summary>
public class AiChatResult
{
    public string? Text { get; set; }
    public string? ReasoningContent { get; set; }
    public List<AiToolCall>? ToolCalls { get; set; }
    public bool IsToolCall => ToolCalls != null && ToolCalls.Count > 0;
}

/// <summary>AI 请求调用一个工具</summary>
public class AiToolCall
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "function";
    public string FunctionName { get; set; } = "";
    public string FunctionArguments { get; set; } = ""; // JSON string
}

/// <summary>工具定义（JSON schema），发送给 AI 的 tools 参数</summary>
public class AiToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object Parameters { get; set; } = new { };
}
