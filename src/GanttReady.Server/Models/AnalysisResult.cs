namespace GanttReady.Server.Models;

/// <summary>
/// 项目分析结果
/// </summary>
public class ProjectAnalysisResult
{
    public int ProjectDurationDays { get; set; }
    public int TotalTasks { get; set; }
    public DateTime EarliestFinish { get; set; }
    public List<TaskItem> CriticalPathTasks { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public List<TaskItem> OnTimeTasks { get; set; } = new();
    public List<TaskItem> EarlyStartTasks { get; set; } = new();
    public List<TaskItem> LateStartTasks { get; set; } = new();
    public List<MilestoneInfo> Milestones { get; set; } = new();
}

/// <summary>
/// 里程碑信息
/// </summary>
public class MilestoneInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
