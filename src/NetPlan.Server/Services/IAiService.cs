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
}

public class AiMessage
{
    public string Role { get; set; } = "user";  // system / user / assistant
    public string Content { get; set; } = "";
}
