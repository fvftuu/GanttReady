using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public interface IAnalysisService
{
    /// <summary>
    /// 获取项目总体概况
    /// </summary>
    Task<ProjectOverview> GetProjectOverviewAsync(int projectId);

    /// <summary>
    /// 获取资源使用汇总
    /// </summary>
    Task<ResourceSummary> GetResourceSummaryAsync(int projectId);

    /// <summary>
    /// 获取资源使用明细
    /// </summary>
    Task<List<ResourceDetail>> GetResourceDetailsAsync(int projectId);

    /// <summary>
    /// 获取工序进度明细
    /// </summary>
    Task<List<TaskProgressDetail>> GetTaskProgressDetailsAsync(int projectId);

    /// <summary>
    /// 分析项目进度计划（关键路径、时差、里程碑）
    /// </summary>
    ProjectAnalysisResult AnalyzeProject(Project project, List<TaskItem> tasks);

    /// <summary>
    /// 资源平衡：逐月检测资源超分配，推迟低优先级任务
    /// </summary>
    Task<ResourceLevelingResult> LevelResourcesAsync(int projectId);
}

// 辅助 DTO 类
public class ProjectOverview
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CriticalTasks { get; set; }
    public double CriticalPathLength { get; set; }
    public int? TotalFloat { get; set; }
    public DateTime PlanStartDate { get; set; }
    public DateTime PlanEndDate { get; set; }
    public double? ProjectProgress { get; set; }
    public int DelayedTasks { get; set; }
    public int AcceleratedTasks { get; set; }
}

public class ResourceSummary
{
    public int ProjectId { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalEquipmentCost { get; set; }
    public decimal TotalCost { get; set; }
    public int LaborCount { get; set; }
    public int MaterialCount { get; set; }
    public int EquipmentCount { get; set; }
}

public class ResourceDetail
{
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalCost { get; set; }
    public int AssignedTasks { get; set; }
}

public class TaskProgressDetail
{
    public int TaskId { get; set; }
    public string TaskCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTime PlanStart { get; set; }
    public DateTime PlanEnd { get; set; }
    public int PlanDuration { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public int? ActualDuration { get; set; }
    public double ProgressPercentage { get; set; }
    public int? TotalFloat { get; set; }
    public bool IsCritical { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ResourceLevelingResult
{
    public List<LevelingAdjustment> Adjustments { get; set; } = new();
    public int ConflictsDetected { get; set; }
    public int ConflictsResolved { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class LevelingAdjustment
{
    public string TaskCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTime OriginalStart { get; set; }
    public DateTime AdjustedStart { get; set; }
    public int DelayDays { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
