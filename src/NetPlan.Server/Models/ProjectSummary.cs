namespace NetPlan.Server.Models;

public class ProjectSummary
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime PlanStartDate { get; set; }
    public DateTime PlanEndDate { get; set; }
    public int TaskCount { get; set; }
    public int ResourceCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public double TotalPlanCost { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Status => PlanEndDate < DateTime.Today ? 2 :  // 2 = 超期
                         PlanStartDate > DateTime.Today ? 0 :  // 0 = 未开始
                         1;                                     // 1 = 进行中
    public string StatusLabel => Status switch
    {
        0 => "未开始",
        1 => "进行中",
        2 => "已超期",
        _ => ""
    };
    public int DaysRemaining => (PlanEndDate - DateTime.Today).Days;
    public double CompletionPct => TaskCount > 0 ? Math.Round((double)CompletedTaskCount / TaskCount * 100, 0) : 0;
}
