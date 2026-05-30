using NetPlan.Server.Services;

namespace NetPlan.Server;

public class PromptRule
{
    public int Id { get; set; }
    public string Name { get; set; } = "未命名规则";
    public string Content { get; set; } = "";
    public bool IsActive { get; set; }
}

public static class PromptStorage
{
    private static string GetDir() => Path.Combine(Directory.GetCurrentDirectory(), "prompts");
    private static string GetFilePath(int projectId) => Path.Combine(GetDir(), $"rules_{projectId}.json");

    /// <summary>读取某项目的所有规则，不存在则返回空列表</summary>
    public static List<PromptRule> GetRules(int projectId)
    {
        var path = GetFilePath(projectId);
        if (!File.Exists(path)) return new List<PromptRule>();
        var json = File.ReadAllText(path);
        var rules = System.Text.Json.JsonSerializer.Deserialize<List<PromptRule>>(json);
        return rules ?? new List<PromptRule>();
    }

    /// <summary>保存规则列表</summary>
    private static void SaveRules(int projectId, List<PromptRule> rules)
    {
        Directory.CreateDirectory(GetDir());
        var json = System.Text.Json.JsonSerializer.Serialize(rules);
        File.WriteAllText(GetFilePath(projectId), json);
    }

    /// <summary>添加规则（最多10条），返回新规则ID</summary>
    public static int AddRule(int projectId, string name, string content)
    {
        var rules = GetRules(projectId);
        if (rules.Count >= 10) return -1; // 已达上限
        var newId = rules.Count > 0 ? rules.Max(r => r.Id) + 1 : 1;
        // 只有第一条规则时自动设为激活
        rules.Add(new PromptRule { Id = newId, Name = name, Content = content, IsActive = rules.Count == 0 });
        SaveRules(projectId, rules);
        return newId;
    }

    /// <summary>删除规则</summary>
    public static void DeleteRule(int projectId, int ruleId)
    {
        var rules = GetRules(projectId);
        rules.RemoveAll(r => r.Id == ruleId);
        // 如果删除了激活的规则，将第一条设为激活
        if (rules.Count > 0 && !rules.Any(r => r.IsActive))
            rules[0].IsActive = true;
        SaveRules(projectId, rules);
    }

    /// <summary>设置激活的规则</summary>
    public static void SetActiveRule(int projectId, int ruleId)
    {
        var rules = GetRules(projectId);
        foreach (var r in rules) r.IsActive = (r.Id == ruleId);
        SaveRules(projectId, rules);
    }

    /// <summary>获取激活规则的文案，没有则返回 null</summary>
    public static string? GetActiveContent(int projectId)
    {
        var rules = GetRules(projectId);
        var active = rules.FirstOrDefault(r => r.IsActive);
        return active?.Content;
    }

    /// <summary>获取当前是否有自定义规则在生效</summary>
    public static bool HasActiveRule(int projectId)
    {
        return GetRules(projectId).Any(r => r.IsActive);
    }

    /// <summary>获取默认模板文案</summary>
    public static string GetDefaultTemplate()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "analysis_prompt_template.txt");
        if (File.Exists(templatePath))
            return File.ReadAllText(templatePath);
        return ChatPromptBuilder.BuildAnalysisPrompt(null, false);
    }
}
