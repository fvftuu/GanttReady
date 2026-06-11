using GanttReady.Server.Models;

namespace GanttReady.Server.Services;

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

    // ========== 新增：基于 GB/T 13400.3-2009 的分析方法 ==========

    /// <summary>
    /// 挣值分析 (§11.2.3)：计算 PV/EV/SPI/CPI
    /// </summary>
    Task<EarnedValueResult> GetEarnedValueAsync(int projectId);

    /// <summary>
    /// 前锋线累计进度曲线 (§11.2.3)：逐日计划累计完成% vs 实际累计完成%
    /// </summary>
    Task<ProgressCurveResult> GetProgressCurveAsync(int projectId);

    /// <summary>
    /// 工期偏差分析 (§9.1)：各任务计划 vs 实际偏差
    /// </summary>
    Task<ScheduleVarianceResult> GetScheduleVarianceAsync(int projectId);

    /// <summary>
    /// 阶段完成率分析 (§12.1)：基于里程碑的7阶段完成情况
    /// </summary>
    Task<StageCompletionResult> GetStageCompletionAsync(int projectId);

    /// <summary>
    /// 资源月度负荷矩阵 (§10.1.1)：资源×月份的二维使用矩阵
    /// </summary>
    Task<ResourceLoadResult> GetResourceLoadAsync(int projectId);

    /// <summary>
    /// 导出分析报告为 Excel (.xlsx)：包含全部分析模块的工作表
    /// </summary>
    Task<byte[]> ExportAnalysisReportAsync(int projectId);
}

// ==================== 现有 DTO ====================

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
    public decimal TotalMeasureCost { get; set; }
    public decimal TotalCost { get; set; }
    public int LaborCount { get; set; }
    public int MaterialCount { get; set; }
    public int EquipmentCount { get; set; }
    public int MeasureCount { get; set; }
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

// ==================== 新增 DTO：GB/T 13400.3-2009 分析 ====================

/// <summary>
/// 挣值分析结果 (§11.2.3)
/// </summary>
public class EarnedValueResult
{
    /// <summary>计划价值 (Planned Value)：按计划应完成的工作预算</summary>
    public double PlannedValue { get; set; }
    /// <summary>挣值 (Earned Value)：实际完成的工作预算</summary>
    public double EarnedValue { get; set; }
    /// <summary>进度绩效指数 SPI = EV/PV</summary>
    public double SchedulePerformanceIndex { get; set; }
    /// <summary>进度偏差 SV = EV - PV</summary>
    public double ScheduleVariance { get; set; }
    /// <summary>总预算 (Budget at Completion)</summary>
    public double BudgetAtCompletion { get; set; }
    /// <summary>预计完成估算 EAC = BAC / SPI</summary>
    public double EstimateAtCompletion { get; set; }
    /// <summary>状态日期</summary>
    public DateTime StatusDate { get; set; }
    /// <summary>实际成本 (Actual Cost)</summary>
    public double ActualCost { get; set; }
    /// <summary>成本绩效指数 CPI = EV/AC</summary>
    public double CostPerformanceIndex { get; set; }
    /// <summary>成本偏差 CV = EV - AC</summary>
    public double CostVariance { get; set; }
    /// <summary>综合绩效指数 CSI = SPI × CPI</summary>
    public double CostScheduleIndex { get; set; }
    /// <summary>SPI 趋势：各月 SPI 值（用于趋势图）</summary>
    public List<MonthlyEVM> MonthlyTrend { get; set; } = new();
}

public class MonthlyEVM
{
    public string Month { get; set; } = "";
    public double PV { get; set; }
    public double EV { get; set; }
    public double SPI { get; set; }
}

/// <summary>
/// 前锋线累计进度曲线 (§11.2.3)
/// </summary>
public class ProgressCurveResult
{
    public List<ProgressCurvePoint> PlannedCurve { get; set; } = new();
    public List<ProgressCurvePoint> ActualCurve { get; set; } = new();
    /// <summary>前锋线日期（取最近有实际进度的日期）</summary>
    public DateTime ProgressDate { get; set; }
    /// <summary>计划完成%</summary>
    public double PlannedPct { get; set; }
    /// <summary>实际完成%</summary>
    public double ActualPct { get; set; }
    /// <summary>偏差天数（正=提前，负=延后）</summary>
    public int DeviationDays { get; set; }
}

public class ProgressCurvePoint
{
    public DateTime Date { get; set; }
    public double CumulativePct { get; set; }
}

/// <summary>
/// 工期偏差分析 (§9.1)
/// </summary>
public class ScheduleVarianceResult
{
    public List<ScheduleVarianceItem> Items { get; set; } = new();
    /// <summary>提前完成任务数</summary>
    public int AheadCount { get; set; }
    /// <summary>按时完成任务数</summary>
    public int OnTimeCount { get; set; }
    /// <summary>延后完成任务数</summary>
    public int BehindCount { get; set; }
    /// <summary>尚未开始的任务数</summary>
    public int NotStartedCount { get; set; }
    /// <summary>进行中（未到期）任务数</summary>
    public int InProgressCount { get; set; }
    /// <summary>延后任务累计天数（绝对值之和）</summary>
    public int TotalDelayDays { get; set; }
    /// <summary>提前任务累计天数（绝对值之和）</summary>
    public int TotalAheadDays { get; set; }
}

public class ScheduleVarianceItem
{
    public string TaskCode { get; set; } = "";
    public string TaskName { get; set; } = "";
    public DateTime PlanStart { get; set; }
    public DateTime PlanEnd { get; set; }
    public DateTime? ActualEnd { get; set; }
    public int? VarianceDays { get; set; } // null = 未开始/进行中（不纳入偏差统计）
    public string Status { get; set; } = ""; // "ahead", "ontime", "behind", "not_started", "in_progress"
    public bool IsCritical { get; set; }
}

/// <summary>
/// 阶段完成率分析 (§12.1)：基于里程碑判断
/// </summary>
public class StageCompletionResult
{
    public List<StageInfo> Stages { get; set; } = new();
    /// <summary>已完成阶段数</summary>
    public int CompletedStages { get; set; }
    /// <summary>总阶段数（动态）</summary>
    public int TotalStages { get; set; }
    /// <summary>整体完成率（加权）</summary>
    public double OverallPct { get; set; }
    /// <summary>检测方法：code_prefix / milestone_fallback</summary>
    public string DetectionMethod { get; set; } = "";
}

public class StageInfo
{
    /// <summary>阶段序号</summary>
    public int StageNo { get; set; }
    /// <summary>阶段名称</summary>
    public string StageName { get; set; } = "";
    /// <summary>阶段代码（如 A、B、C）</summary>
    public string StageCode { get; set; } = "";
    /// <summary>计划完成%</summary>
    public double PlannedCompletionPct { get; set; }
    /// <summary>实际完成%</summary>
    public double ActualCompletionPct { get; set; }
    /// <summary>任务总数</summary>
    public int TotalTasks { get; set; }
    /// <summary>已完成任务数</summary>
    public int CompletedTasks { get; set; }
    /// <summary>进行中任务数</summary>
    public int InProgressTasks { get; set; }
    /// <summary>计划结束日期</summary>
    public DateTime? PlannedEndDate { get; set; }
    /// <summary>实际结束日期（全部完成时）</summary>
    public DateTime? ActualEndDate { get; set; }
    /// <summary>延滞天数（负数=提前）</summary>
    public int? DelayDays { get; set; }
    /// <summary>状态：completed/in_progress/delayed/pending</summary>
    public string Status { get; set; } = "pending";
}

/// <summary>
/// 资源月度负荷矩阵 (§10.1.1)
/// </summary>
public class ResourceLoadResult
{
    /// <summary>月份标签</summary>
    public List<string> Months { get; set; } = new();
    /// <summary>资源名称列表</summary>
    public List<string> ResourceNames { get; set; } = new();
    /// <summary>使用量矩阵 [资源索引][月份索引] = 使用量/容量比 (0-∞)</summary>
    public List<List<double>> LoadMatrix { get; set; } = new();
    /// <summary>容量列表</summary>
    public List<decimal> Capacities { get; set; } = new();
}
