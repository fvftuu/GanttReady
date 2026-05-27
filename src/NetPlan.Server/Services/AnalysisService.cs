using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public class AnalysisService : IAnalysisService
{
    private readonly NetPlanDbContext _db;

    public AnalysisService(NetPlanDbContext db)
    {
        _db = db;
    }

    public async Task<ProjectOverview> GetProjectOverviewAsync(int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var overview = new ProjectOverview
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TotalTasks = tasks.Count,
            CriticalTasks = tasks.Count(t => t.IsCritical),
            TotalFloat = tasks.Max(t => t.TotalFloat) ?? 0,
            PlanStartDate = project.PlanStartDate,
            PlanEndDate = project.PlanEndDate,
            DelayedTasks = tasks.Count(t => t.ActualEndDate.HasValue && t.ActualEndDate > t.PlanEndDate),
            AcceleratedTasks = tasks.Count(t => t.ActualEndDate.HasValue && t.ActualEndDate < t.PlanEndDate)
        };

        var completedTasks = tasks.Count(t => t.ActualEndDate.HasValue);
        if (tasks.Count > 0)
        {
            overview.ProjectProgress = (double)completedTasks / tasks.Count * 100;
        }

        var criticalTasks = tasks.Where(t => t.IsCritical).ToList();
        if (criticalTasks.Any())
        {
            overview.CriticalPathLength = criticalTasks.Max(t => t.EarlyFinish ?? 0) -
                                          criticalTasks.Min(t => t.EarlyStart ?? 0);
        }

        return overview;
    }

    public async Task<ResourceSummary> GetResourceSummaryAsync(int projectId)
    {
        var resources = await _db.Resources
            .Where(r => r.ProjectId == projectId)
            .ToListAsync();

        var summary = new ResourceSummary
        {
            ProjectId = projectId,
            LaborCount = resources.Count(r => r.Type == ResourceType.Labor),
            MaterialCount = resources.Count(r => r.Type == ResourceType.Material),
            EquipmentCount = resources.Count(r => r.Type == ResourceType.Equipment),
            MeasureCount = resources.Count(r => r.Type == ResourceType.Measure)
        };

        foreach (var resource in resources)
        {
            var cost = resource.Quantity * resource.UnitPrice;

            switch (resource.Type)
            {
                case ResourceType.Labor:
                    summary.TotalLaborCost += cost;
                    break;
                case ResourceType.Material:
                    summary.TotalMaterialCost += cost;
                    break;
                case ResourceType.Equipment:
                    summary.TotalEquipmentCost += cost;
                    break;
                case ResourceType.Measure:
                    summary.TotalMeasureCost += cost;
                    break;
            }
        }

        summary.TotalCost = summary.TotalLaborCost + summary.TotalMaterialCost + summary.TotalEquipmentCost;

        return summary;
    }

    public async Task<List<ResourceDetail>> GetResourceDetailsAsync(int projectId)
    {
        var resources = await _db.Resources
            .Where(r => r.ProjectId == projectId)
            .ToListAsync();

        return resources.Select(r => new ResourceDetail
        {
            ResourceId = r.Id,
            ResourceName = r.Name,
            ResourceType = r.Type.ToString(),
            Unit = r.Unit ?? "",
            TotalQuantity = r.Quantity,
            UnitPrice = r.UnitPrice,
            TotalCost = r.Quantity * r.UnitPrice,
            AssignedTasks = 0
        }).OrderBy(r => r.ResourceType).ThenBy(r => r.ResourceName).ToList();
    }

    public async Task<List<TaskProgressDetail>> GetTaskProgressDetailsAsync(int projectId)
    {
        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        return tasks.Select(t => new TaskProgressDetail
        {
            TaskId = t.Id,
            TaskCode = t.Code,
            TaskName = t.Name,
            PlanStart = t.PlanStartDate,
            PlanEnd = t.PlanEndDate,
            PlanDuration = t.PlanDuration,
            ActualStart = t.ActualStartDate,
            ActualEnd = t.ActualEndDate,
            ActualDuration = t.ActualDuration,
            ProgressPercentage = CalculateProgress(t),
            TotalFloat = t.TotalFloat,
            IsCritical = t.IsCritical,
            Status = DetermineStatus(t)
        }).ToList();
    }

    private double CalculateProgress(TaskItem task)
    {
        if (!task.ActualEndDate.HasValue && !task.ActualStartDate.HasValue)
            return 0;

        if (task.ActualEndDate.HasValue)
            return 100;

        if (task.ActualStartDate.HasValue)
        {
            var today = DateTime.Today;
            var totalDays = (task.PlanEndDate - task.PlanStartDate).Days;
            if (totalDays <= 0) return 0;

            var elapsedDays = (today - task.ActualStartDate.Value).Days;
            return Math.Min(100, Math.Max(0, (double)elapsedDays / totalDays * 100));
        }

        return 0;
    }

    private string DetermineStatus(TaskItem task)
    {
        if (task.ActualEndDate.HasValue)
        {
            if (task.ActualEndDate > task.PlanEndDate)
                return "延期";
            if (task.ActualEndDate < task.PlanEndDate)
                return "提前";
            return "按时";
        }

        if (task.ActualStartDate.HasValue)
        {
            var today = DateTime.Today;
            if (today > task.PlanEndDate)
                return "严重延期";
            if (today > task.PlanStartDate)
                return "进行中";
            return "未开始";
        }

        return "未开始";
    }

    public ProjectAnalysisResult AnalyzeProject(Project project, List<TaskItem> tasks)
    {
        var today = DateTime.Today;
        var result = new ProjectAnalysisResult
        {
            TotalTasks = tasks.Count,
            Tasks = tasks,
            CriticalPathTasks = tasks.Where(t => t.IsCritical).ToList(),
            // 按实际完成状态分类，不再使用 TotalFloat
            EarlyStartTasks = tasks.Where(t => t.CompletionPercentage >= 100
                && t.ActualEndDate.HasValue && t.ActualEndDate.Value.Date < t.PlanEndDate.Date).ToList(),
            OnTimeTasks = tasks.Where(t => t.CompletionPercentage >= 100
                && (!t.ActualEndDate.HasValue || t.ActualEndDate.Value.Date == t.PlanEndDate.Date)).ToList(),
            LateStartTasks = tasks.Where(t => t.CompletionPercentage < 100
                && today > t.PlanEndDate).ToList(),
            Milestones = tasks.Where(t => t.IsMilestone)
                .Select(t => new MilestoneInfo { Name = t.Name, Date = t.PlanEndDate })
                .ToList()
        };

        if (tasks.Count > 0)
        {
            var firstTask = tasks.OrderBy(t => t.PlanStartDate).First();
            var lastTask = tasks.OrderByDescending(t => t.PlanEndDate).First();
            result.EarliestFinish = lastTask.PlanEndDate;
            result.ProjectDurationDays = (lastTask.PlanEndDate - firstTask.PlanStartDate).Days + 1;
        }

        return result;
    }

    public async Task<ResourceLevelingResult> LevelResourcesAsync(int projectId)
    {
        var result = new ResourceLevelingResult();
        var assignments = await _db.ResourceAssignments
            .Include(a => a.Task)
            .Include(a => a.Resource)
            .Where(a => a.Task.ProjectId == projectId)
            .OrderBy(a => a.Task.EarlyStart)
            .ToListAsync();

        if (!assignments.Any())
        {
            result.Summary = "没有资源分配数据";
            return result;
        }

        var byResource = assignments.GroupBy(a => a.ResourceId);
        foreach (var resourceGroup in byResource)
        {
            var resource = resourceGroup.First().Resource;
            if (resource == null) continue;
            decimal capacity = resource.Quantity > 0 ? resource.Quantity : 1;

            var monthlyUsage = new Dictionary<string, (decimal Usage, List<ResourceAssignment> Tasks)>();
            foreach (var a in resourceGroup)
            {
                var cursor = a.Task.PlanStartDate;
                var end = cursor.AddDays(a.Task.PlanDuration);
                while (cursor < end)
                {
                    var key = cursor.ToString("yyyy-MM");
                    if (!monthlyUsage.ContainsKey(key))
                        monthlyUsage[key] = (0, new());
                    var (usage, tasks) = monthlyUsage[key];
                    monthlyUsage[key] = (usage + a.Quantity, tasks.Append(a).ToList());
                    cursor = cursor.AddMonths(1);
                }
            }

            foreach (var (month, (usage, tasks)) in monthlyUsage)
            {
                if (usage <= capacity) continue;
                result.ConflictsDetected++;

                var sorted = tasks.OrderBy(a => a.Task.TotalFloat ?? int.MaxValue)
                                  .ThenBy(a => a.Task.EarlyStart).ToList();
                foreach (var a in sorted.Skip(1))
                {
                    result.Adjustments.Add(new LevelingAdjustment
                    {
                        TaskCode = a.Task.Code,
                        TaskName = a.Task.Name,
                        OriginalStart = a.Task.PlanStartDate,
                        AdjustedStart = a.Task.PlanStartDate.AddMonths(1),
                        DelayDays = 30,
                        ResourceName = resource.Name,
                        Reason = string.Format("{0} 超分配({1:F1}>{2:F1})", month, usage, capacity)
                    });
                    result.ConflictsResolved++;
                }
            }
        }

        result.Summary = result.ConflictsDetected > 0
            ? string.Format("检测到 {0} 个冲突，建议推迟 {1} 个任务", result.ConflictsDetected, result.ConflictsResolved)
            : "未检测到资源冲突";
        return result;
    }

    // ==================== 新增：GB/T 13400.3-2009 分析 ====================

    /// <summary>
    /// 挣值分析 (§11.2.3)
    /// 当前系统未追踪实际成本(AC)，使用简化版 EVM：
    ///   PV = 按计划应完成工作量比例 × 总预算
    ///   EV = 实际完成工作量比例 × 总预算
    ///   SPI = EV / PV
    /// </summary>
    public async Task<EarnedValueResult> GetEarnedValueAsync(int projectId)
    {
        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        var assignments = await _db.ResourceAssignments
            .Include(a => a.Resource)
            .Where(a => a.Task.ProjectId == projectId)
            .ToListAsync();

        // BAC = 所有任务计划预算之和（使用系统已计算的 BudgetCost）
        double bac = tasks.Sum(t => (double)t.BudgetCost);
        var statusDate = DateTime.Today;

        double pv = 0; // 计划价值
        double ev = 0; // 挣值

        foreach (var task in tasks)
        {
            // 该任务预算：使用系统已计算的 BudgetCost，包含人工/材料/设备全部成本
            double taskBudget = (double)task.BudgetCost;
            if (taskBudget <= 0) taskBudget = 1;

            // PV: 到状态日期为止，计划应完成的比例
            double plannedPct = CalcPlannedPct(task, statusDate);
            pv += taskBudget * plannedPct;

            // EV: 实际完成比例（基于 CompletionPercentage 或实际日期）
            double actualPct = task.CompletionPercentage / 100.0;
            ev += taskBudget * actualPct;
        }

        var spi = pv > 0 ? ev / pv : 1.0;
        var sv = ev - pv;

        // 实际成本：优先用 TaskItem.ActualCost，有部分数据时回退到 BudgetCost
        // 如果没有任何 ActualCost 数据，AC 退化为 EV（假设按预算执行，CPI=1.0）
        bool hasActualCostData = tasks.Any(t => t.ActualCost.HasValue && t.ActualCost.Value > 0);
        double ac;
        if (hasActualCostData)
            ac = tasks.Sum(t => (double)(t.ActualCost ?? t.BudgetCost));
        else
            ac = ev;

        var cpi = ac > 0 ? ev / ac : 1.0;
        var cv = ev - ac;
        var csi = spi * cpi;

        // 月度趋势
        var monthlyTrend = new List<MonthlyEVM>();
        if (tasks.Any())
        {
            var minDate = tasks.Min(t => t.PlanStartDate);
            var maxDate = tasks.Max(t => t.PlanEndDate);
            var cursor = new DateTime(minDate.Year, minDate.Month, 1);
            while (cursor <= maxDate)
            {
                double mpv = 0, mev = 0;
                var monthEnd = cursor.AddMonths(1).AddDays(-1);
                foreach (var task in tasks)
                {
                    double tb = (double)task.BudgetCost;
                    if (tb <= 0) tb = 1;
                    mpv += tb * CalcPlannedPct(task, monthEnd);
                    mev += tb * (task.CompletionPercentage / 100.0);
                }
                monthlyTrend.Add(new MonthlyEVM
                {
                    Month = cursor.ToString("yyyy-MM"),
                    PV = Math.Round(mpv, 1),
                    EV = Math.Round(mev, 1),
                    SPI = Math.Round(mpv > 0 ? mev / mpv : 1, 3)
                });
                cursor = cursor.AddMonths(1);
            }
        }

        return new EarnedValueResult
        {
            PlannedValue = Math.Round(pv, 1),
            EarnedValue = Math.Round(ev, 1),
            SchedulePerformanceIndex = Math.Round(spi, 3),
            ScheduleVariance = Math.Round(sv, 1),
            BudgetAtCompletion = Math.Round(bac, 1),
            EstimateAtCompletion = Math.Round(bac / Math.Max(0.01, spi), 1),
            ActualCost = Math.Round(ac, 1),
            CostPerformanceIndex = Math.Round(cpi, 3),
            CostVariance = Math.Round(cv, 1),
            CostScheduleIndex = Math.Round(csi, 3),
            StatusDate = statusDate,
            MonthlyTrend = monthlyTrend
        };
    }

    private static double CalcPlannedPct(TaskItem task, DateTime asOfDate)
    {
        if (asOfDate < task.PlanStartDate) return 0;
        if (asOfDate >= task.PlanEndDate) return 1;
        var totalDays = (task.PlanEndDate - task.PlanStartDate).TotalDays;
        if (totalDays <= 0) return 1;
        var elapsed = (asOfDate - task.PlanStartDate).TotalDays;
        return Math.Min(1, elapsed / totalDays);
    }

    /// <summary>
    /// 前锋线累计进度曲线 (§11.2.3)
    /// </summary>
    public async Task<ProgressCurveResult> GetProgressCurveAsync(int projectId)
    {
        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        if (!tasks.Any())
            return new ProgressCurveResult();

        var minDate = tasks.Min(t => t.PlanStartDate);
        var maxDate = tasks.Max(t => t.PlanEndDate);
        var totalDays = (maxDate - minDate).Days + 1;

        var plannedCurve = new List<ProgressCurvePoint>();
        var actualCurve = new List<ProgressCurvePoint>();

        for (int d = 0; d <= totalDays; d++)
        {
            var date = minDate.AddDays(d);
            double plannedCumPct = 0, actualCumPct = 0;
            double totalWeight = tasks.Count;

            foreach (var task in tasks)
            {
                // 计划完成：到该日期计划应完成的任务比例
                if (date >= task.PlanEndDate)
                    plannedCumPct += 1;
                else if (date > task.PlanStartDate)
                {
                    var dur = (task.PlanEndDate - task.PlanStartDate).TotalDays;
                    plannedCumPct += dur > 0 ? (date - task.PlanStartDate).TotalDays / dur : 1;
                }

                // 实际完成：基于 CompletionPercentage 和实际日期
                if (task.ActualEndDate.HasValue && date >= task.ActualEndDate.Value)
                    actualCumPct += 1;
                else if (task.ActualStartDate.HasValue && date >= task.ActualStartDate.Value)
                {
                    var actDur = task.ActualEndDate.HasValue
                        ? (task.ActualEndDate.Value - task.ActualStartDate.Value).TotalDays
                        : (DateTime.Today - task.ActualStartDate.Value).TotalDays;
                    var progressed = task.ActualEndDate.HasValue
                        ? 1.0
                        : Math.Min(1, Math.Max(0, task.CompletionPercentage / 100.0));
                    actualCumPct += progressed;
                }
            }

            plannedCurve.Add(new ProgressCurvePoint
            {
                Date = date,
                CumulativePct = Math.Round(plannedCumPct / Math.Max(1, totalWeight) * 100, 1)
            });
            actualCurve.Add(new ProgressCurvePoint
            {
                Date = date,
                CumulativePct = Math.Round(actualCumPct / Math.Max(1, totalWeight) * 100, 1)
            });
        }

        // 找最近有实际进度的日期
        var progressDate = tasks.Where(t => t.ActualStartDate.HasValue)
            .Select(t => t.ActualEndDate ?? DateTime.Today)
            .DefaultIfEmpty(DateTime.Today)
            .Max();

        var lastPlanned = plannedCurve.LastOrDefault(p => p.Date <= progressDate);
        var lastActual = actualCurve.LastOrDefault(p => p.Date <= progressDate);

        return new ProgressCurveResult
        {
            PlannedCurve = plannedCurve,
            ActualCurve = actualCurve,
            ProgressDate = progressDate,
            PlannedPct = lastPlanned?.CumulativePct ?? 0,
            ActualPct = lastActual?.CumulativePct ?? 0,
            DeviationDays = CalcDeviationDays(tasks, progressDate)
        };
    }

    private static int CalcDeviationDays(List<TaskItem> tasks, DateTime progressDate)
    {
        // 计算前锋线偏差：实际进度与计划进度的时间差
        var plannedTotal = 0.0;
        var actualTotal = 0.0;
        foreach (var task in tasks)
        {
            plannedTotal += CalcPlannedPct(task, progressDate);
            actualTotal += task.CompletionPercentage / 100.0;
        }
        var n = Math.Max(1, tasks.Count);
        var plannedAvg = plannedTotal / n;
        var actualAvg = actualTotal / n;
        // 按项目总工期折算偏差天数
        if (tasks.Any())
        {
            var totalDur = (tasks.Max(t => t.PlanEndDate) - tasks.Min(t => t.PlanStartDate)).TotalDays;
            return (int)Math.Round((actualAvg - plannedAvg) * totalDur);
        }
        return 0;
    }

    /// <summary>
    /// 工期偏差分析 (§9.1)
    /// </summary>
    public async Task<ScheduleVarianceResult> GetScheduleVarianceAsync(int projectId)
    {
        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        var items = new List<ScheduleVarianceItem>();
        var today = DateTime.Today;

        foreach (var task in tasks)
        {
            var actualEnd = task.ActualEndDate;
            int? varianceDays = null;
            string status;

            if (actualEnd.HasValue)
            {
                // 已完成任务
                varianceDays = (task.PlanEndDate - actualEnd.Value).Days;
                if (varianceDays > 0)
                    status = "ahead";
                else if (varianceDays == 0)
                    status = "ontime";
                else
                    status = "behind";
            }
            else if (today > task.PlanEndDate)
            {
                // 未完成且已过计划结束日期 → 延后
                varianceDays = (task.PlanEndDate - today).Days; // 负值
                status = "behind";
            }
            else if (today < task.PlanStartDate)
            {
                // 尚未到开始日期 → 未开始，不纳入偏差统计
                varianceDays = null;
                status = "not_started";
            }
            else
            {
                // 进行中但未到期 → 不纳入偏差统计
                varianceDays = null;
                status = "in_progress";
            }

            items.Add(new ScheduleVarianceItem
            {
                TaskCode = task.Code,
                TaskName = task.Name,
                PlanStart = task.PlanStartDate,
                PlanEnd = task.PlanEndDate,
                ActualEnd = actualEnd,
                VarianceDays = varianceDays,
                Status = status,
                IsCritical = task.IsCritical
            });
        }

        return new ScheduleVarianceResult
        {
            Items = items.OrderBy(i => i.VarianceDays ?? 0).ToList(),
            AheadCount = items.Count(i => i.Status == "ahead"),
            OnTimeCount = items.Count(i => i.Status == "ontime"),
            BehindCount = items.Count(i => i.Status == "behind"),
            NotStartedCount = items.Count(i => i.Status == "not_started"),
            InProgressCount = items.Count(i => i.Status == "in_progress"),
            TotalDelayDays = items.Where(i => i.VarianceDays < 0).Sum(i => Math.Abs(i.VarianceDays ?? 0)),
            TotalAheadDays = items.Where(i => i.VarianceDays > 0).Sum(i => i.VarianceDays ?? 0)
        };
    }

    /// <summary>
    /// 阶段完成率分析 (§12.1)
    /// 双检测方案：优先使用 OutlineLevel（大纲级别），回退到 WBS 代码结构。
    /// 
    /// OutlineLevel 路径：
    ///   - 检测是否有 OL≥2 的任务（存在大纲层级结构）
    ///   - OL=2 作为阶段分组节点，OL≥3 归入上方最近的 OL=2 组
    ///   - 若有里程碑，在 OL 组内再按里程碑二次切分
    /// 
    /// WBS 回退路径（所有任务 OL=1 时触发）：
    ///   1. 平级无里程碑 → 全体任务作为一个阶段分析
    ///   2. 多层无里程碑 → 按代码层级划分阶段
    ///   3. 平级有里程碑 → 按里程碑任务切分阶段
    ///   4. 多层有里程碑 → 按代码层级+里程碑同时切分阶段
    /// </summary>
    public async Task<StageCompletionResult> GetStageCompletionAsync(int projectId)
    {
        var allTasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        if (!allTasks.Any())
            return new StageCompletionResult { DetectionMethod = "无任务数据" };

        var today = DateTime.Today;
        bool hasMilestones = allTasks.Any(t => t.IsMilestone);
        bool useOutlineLevel = HasSubstantialOutlineGroups(allTasks);

        var stages = new List<StageInfo>();
        string detectionMethod;

        if (useOutlineLevel)
        {
            // ===== OutlineLevel 路径：仅当能产生有意义分组时使用 =====
            Console.WriteLine($"[StageCompletion] ProjectId={projectId} mode=OutlineLevel hasMilestones={hasMilestones}");

            if (hasMilestones)
            {
                detectionMethod = "按大纲级别+里程碑";
                GroupByOutlineLevelAndMilestone(stages, allTasks, today);
            }
            else
            {
                detectionMethod = "按大纲级别";
                GroupByOutlineLevel(stages, allTasks, today);
            }
        }
        else
        {
            // ===== WBS 代码分析路径：统一用 DetermineCodeGroupingLevel 判断 =====
            var codes = allTasks.Select(t => t.Code).ToList();
            int groupingLevel = DetermineCodeGroupingLevel(codes);

            Console.WriteLine($"[StageCompletion] ProjectId={projectId} mode=WBS hasMilestones={hasMilestones} groupingLevel={groupingLevel}");

            if (groupingLevel >= 2 && !hasMilestones)
            {
                detectionMethod = "按代码层级";
                GroupByCodePrefix(stages, allTasks, groupingLevel, today);
            }
            else if (groupingLevel >= 2 && hasMilestones)
            {
                detectionMethod = "按代码层级+里程碑";
                GroupByCodePrefixAndMilestone(stages, allTasks, groupingLevel, today);
            }
            else if (groupingLevel <= 1 && hasMilestones)
            {
                detectionMethod = "按里程碑";
                GroupByMilestone(stages, allTasks, today);
            }
            else
            {
                detectionMethod = "全体分析";
                BuildStageInfo(stages, allTasks, "总体", today);
            }
        }

        int completedStages = stages.Count(s => s.Status == "completed");
        double overallPct = stages.Any()
            ? stages.Sum(s => s.ActualCompletionPct * s.TotalTasks) / stages.Sum(s => s.TotalTasks)
            : 0;

        return new StageCompletionResult
        {
            Stages = stages,
            CompletedStages = completedStages,
            TotalStages = stages.Count,
            OverallPct = Math.Round(overallPct, 1),
            DetectionMethod = detectionMethod
        };
    }

    /// <summary>
    /// 判断 OutlineLevel 是否能产生有意义的阶段分组
    /// 条件：≥2 个 OL=2 分组锚点，且每组至少 2 个任务（含锚点自身）
    /// 避免 OutlineLevel 录入不一致时产生无意义阶段
    /// </summary>
    private static bool HasSubstantialOutlineGroups(List<Models.TaskItem> tasks)
    {
        var ol2Anchors = tasks.Where(t => t.OutlineLevel == 2).ToList();
        if (ol2Anchors.Count < 2) return false;

        // 按 SortOrder 顺序扫描：遇到 OL=2 新开一组，OL≥3 归入当前组
        var groupSizes = new List<int>();
        int? currentSize = null;

        foreach (var t in tasks.OrderBy(t => t.SortOrder))
        {
            if (t.OutlineLevel == 2)
            {
                if (currentSize.HasValue)
                    groupSizes.Add(currentSize.Value);
                currentSize = 1;
            }
            else if (t.OutlineLevel >= 3 && currentSize.HasValue)
            {
                currentSize++;
            }
        }
        if (currentSize.HasValue)
            groupSizes.Add(currentSize.Value);

        return groupSizes.Count(g => g >= 2) >= 2;
    }

    /// <summary>
    /// 按 OutlineLevel 分组：OL=2 作为阶段分组节点，OL≥3 归入上方最近的 OL=2 组。
    /// OL=1 任务作为独立阶段单独处理。
    /// </summary>
    private static void GroupByOutlineLevel(List<StageInfo> stages, List<Models.TaskItem> tasks, DateTime today)
    {
        List<Models.TaskItem>? currentGroup = null;
        char label = 'A';

        void FlushGroup()
        {
            if (currentGroup != null && currentGroup.Any())
            {
                BuildStageInfo(stages, currentGroup, ((char)(label++)).ToString(), today);
                currentGroup = null;
            }
        }

        foreach (var task in tasks)
        {
            if (task.OutlineLevel == 2)
            {
                FlushGroup();
                currentGroup = new List<Models.TaskItem> { task };
            }
            else if (task.OutlineLevel >= 3)
            {
                currentGroup?.Add(task);
            }
            else // OutlineLevel == 1
            {
                FlushGroup();
                // OL=1 独立作为单任务阶段
                BuildStageInfo(stages, new List<Models.TaskItem> { task }, ((char)(label++)).ToString(), today);
            }
        }

        FlushGroup();
    }

    /// <summary>
    /// 按 OutlineLevel + 里程碑分组：先在 OL=2 层级分组，再在每组内按里程碑二次切分。
    /// </summary>
    private static void GroupByOutlineLevelAndMilestone(List<StageInfo> stages, List<Models.TaskItem> tasks, DateTime today)
    {
        List<Models.TaskItem>? currentGroup = null;
        int stageSeq = 0;
        char groupLabel = 'A';

        void FlushGroup()
        {
            if (currentGroup != null && currentGroup.Any())
            {
                var subgroupStages = new List<StageInfo>();
                GroupByMilestone(subgroupStages, currentGroup, today);
                foreach (var s in subgroupStages)
                {
                    s.StageCode = $"{groupLabel}-{s.StageCode}";
                    s.StageNo = ++stageSeq;
                }
                stages.AddRange(subgroupStages);
                groupLabel = (char)(groupLabel + 1);
                currentGroup = null;
            }
        }

        foreach (var task in tasks)
        {
            if (task.OutlineLevel == 2)
            {
                FlushGroup();
                currentGroup = new List<Models.TaskItem> { task };
            }
            else if (task.OutlineLevel >= 3)
            {
                currentGroup?.Add(task);
            }
            else // OutlineLevel == 1
            {
                FlushGroup();
                // OL=1 独立作为单任务组，在其中按里程碑切分
                currentGroup = new List<Models.TaskItem> { task };
                FlushGroup();
            }
        }

        FlushGroup();
    }

    /// <summary>
    /// 根据任务代码结构确定有意义的阶段分组层级
    /// 例如：代码 1.3.1.1, 1.3.1.2, 1.3.2.1, 1.4.1.1 →
    ///   深度1="1" (只有1组) → 不够
    ///   深度2="1.3","1.4" (2组各有≥2子任务) → 选择深度2
    ///   返回分组层级（代码段数），只有1段时返回1
    /// 注意：单任务组（父级汇总任务）不计入组数阈值
    /// </summary>


    private static int DetermineCodeGroupingLevel(List<string> codes)
    {
        if (!codes.Any()) return 1;

        var parsed = codes
            .Select(c => new { Code = c, Parts = SplitCodeParts(c) })
            .Where(x => x.Parts.Count > 1)
            .ToList();

        if (!parsed.Any()) return 1;

        int maxDepth = parsed.Max(x => x.Parts.Count);

        // 从深度2开始，找到至少有2个"有效组"（每组≥2任务）的层级
        for (int depth = 2; depth <= maxDepth; depth++)
        {
            var prefixGroups = parsed
                .Select(x => new { Prefix = string.Join(".", x.Parts.Take(depth)) })
                .GroupBy(x => x.Prefix)
                .Select(g => new { Prefix = g.Key, Count = g.Count() })
                .ToList();

            // 至少2个组各有≥2个任务（单任务组是父级汇总，不算有效阶段）
            int substantialGroups = prefixGroups.Count(g => g.Count >= 2);
            if (substantialGroups >= 2)
                return depth;
        }

        // 退路：没有找到有效分组 → 返回1（无有意义分组，走里程碑/全体分析）
        return 1;
    }

    /// <summary>
    /// 切分代码为多段，支持 "A1"/"A" 和 "1.3.1.1" 两种格式
    /// </summary>
    private static List<string> SplitCodeParts(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new List<string> { code };

        // 数字点分隔格式：1.3.1.1 → [1, 3, 1, 1]
        if (char.IsDigit(code[0]) && code.Contains('.'))
            return code.Split('.').ToList();

        // 字母+数字格式：A1 → [A, 1] 或纯字母 A → [A]
        var parts = new List<string>();
        int i = 0;
        while (i < code.Length)
        {
            if (char.IsLetter(code[i]))
            {
                int start = i;
                while (i < code.Length && char.IsLetter(code[i])) i++;
                parts.Add(code.Substring(start, i - start));
            }
            else if (char.IsDigit(code[i]))
            {
                int start = i;
                while (i < code.Length && char.IsDigit(code[i])) i++;
                parts.Add(code.Substring(start, i - start));
            }
            else
            {
                i++;
            }
        }
        return parts.Count > 0 ? parts : new List<string> { code };
    }

    /// <summary>
    /// 按代码前缀分组（无里程碑场景）
    /// </summary>
    private static void GroupByCodePrefix(List<StageInfo> stages, List<Models.TaskItem> tasks,
        int level, DateTime today)
    {
        var groups = tasks
            .GroupBy(t =>
            {
                var parts = SplitCodeParts(t.Code);
                return parts.Count >= level
                    ? string.Join(".", parts.Take(level))
                    : t.Code;
            })
            .OrderBy(g => g.Min(t => t.SortOrder))
            .ToList();

        char label = 'A';
        foreach (var group in groups)
        {
            var stageTasks = group.OrderBy(t => t.SortOrder).ToList();
            // 将代码前缀（如 "1.2", "A"）作为阶段建议名
            BuildStageInfo(stages, stageTasks, ((char)(label++)).ToString(), today, group.Key);
        }
    }

    /// <summary>
    /// 按代码前缀分组，在每组内再按里程碑切分（多层级+有里程碑场景）
    /// </summary>
    private static void GroupByCodePrefixAndMilestone(List<StageInfo> stages,
        List<Models.TaskItem> tasks, int level, DateTime today)
    {
        var groups = tasks
            .GroupBy(t =>
            {
                var parts = SplitCodeParts(t.Code);
                return parts.Count >= level
                    ? string.Join(".", parts.Take(level))
                    : t.Code;
            })
            .OrderBy(g => g.Min(t => t.SortOrder))
            .ToList();

        int stageSeq = 0;
        char groupLabel = 'A';
        foreach (var group in groups)
        {
            var groupTasks = group.OrderBy(t => t.SortOrder).ToList();
            var subgroupStages = new List<StageInfo>();
            GroupByMilestone(subgroupStages, groupTasks, today);
            foreach (var s in subgroupStages)
            {
                s.StageCode = $"{groupLabel}-{s.StageCode}";
                s.StageNo = ++stageSeq;
                // 保留 BuildStageInfo 生成的任务名，只有无意义时才用代码前缀
                if (string.IsNullOrEmpty(s.StageName))
                    s.StageName = group.Key;
            }
            stages.AddRange(subgroupStages);
            groupLabel = (char)(groupLabel + 1);
        }
    }

    /// <summary>
    /// 按里程碑任务切分：在排序后的任务列表中，遇到里程碑任务就以此为分界点
    /// 里程碑本身归入前一段，作为该阶段的锚点
    /// </summary>
    private static void GroupByMilestone(List<StageInfo> stages, List<Models.TaskItem> tasks, DateTime today)
    {
        var currentSegment = new List<Models.TaskItem>();
        char code = 'A';

        foreach (var task in tasks)
        {
            currentSegment.Add(task);
            if (task.IsMilestone)
            {
                BuildStageInfo(stages, new List<Models.TaskItem>(currentSegment), code.ToString(), today);
                code = (char)(code + 1);
                currentSegment.Clear();
            }
        }

        // Remaining tasks after the last milestone
        if (currentSegment.Any())
        {
            BuildStageInfo(stages, currentSegment, code.ToString(), today);
        }
    }

    private static void BuildStageInfo(List<StageInfo> stages, List<Models.TaskItem> tasks,
        string code, DateTime today, string? suggestedName = null)
    {
        int total = tasks.Count;
        int completed = tasks.Count(t => t.ActualEndDate.HasValue);
        int inProgress = tasks.Count(t =>
            !t.ActualEndDate.HasValue &&
            (t.ActualStartDate.HasValue || t.PlanStartDate <= today));

        // 计划完成率：基于时间进度计算（已过工期/总工期），而非取 CompletionPercentage 平均值
        double plannedPct = tasks.Any()
            ? tasks.Average(t =>
            {
                if (today <= t.PlanStartDate) return 0;
                if (today >= t.PlanEndDate) return 100;
                var totalDays = (t.PlanEndDate - t.PlanStartDate).TotalDays;
                if (totalDays <= 0) return 100;
                var elapsed = (today - t.PlanStartDate).TotalDays;
                return Math.Min(100, Math.Max(0, elapsed / totalDays * 100));
            })
            : 0;
        double actualPct = total > 0 ? (double)completed / total * 100 : 0;

        var plannedEnd = tasks.Max(t => (DateTime?)t.PlanEndDate);
        var actualEnd = completed == total ? tasks.Max(t => t.ActualEndDate) : null;

        int? delayDays = null;
        if (actualEnd.HasValue && plannedEnd.HasValue)
            delayDays = (actualEnd.Value - plannedEnd.Value).Days;
        else if (completed < total && plannedEnd.HasValue && plannedEnd.Value < today)
            delayDays = (today - plannedEnd.Value).Days;

        string status = completed == total ? "completed"
            : completed > 0 || inProgress > 0
                ? (delayDays.HasValue && delayDays > 0 ? "delayed" : "in_progress")
                : "pending";

        // Stage name: 优先用调用者提供的建议名，其次用第一个任务的名称（无长度限制）
        var stageName = suggestedName;
        if (string.IsNullOrEmpty(stageName))
        {
            var representative = tasks.OrderBy(t => t.SortOrder).FirstOrDefault();
            stageName = representative?.Name ?? $"阶段 {code}";
        }

        stages.Add(new StageInfo
        {
            StageNo = stages.Count + 1,
            StageCode = code,
            StageName = stageName,
            TotalTasks = total,
            CompletedTasks = completed,
            InProgressTasks = inProgress,
            PlannedCompletionPct = Math.Round(plannedPct, 1),
            ActualCompletionPct = Math.Round(actualPct, 1),
            PlannedEndDate = plannedEnd,
            ActualEndDate = actualEnd,
            DelayDays = delayDays,
            Status = status
        });
    }

    /// <summary>
    /// 资源月度负荷矩阵 (§10.1.1)
    /// </summary>
    public async Task<ResourceLoadResult> GetResourceLoadAsync(int projectId)
    {
        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        var assignments = await _db.ResourceAssignments
            .Include(a => a.Resource)
            .Where(a => a.Task.ProjectId == projectId)
            .ToListAsync();

        if (!tasks.Any() || !assignments.Any())
            return new ResourceLoadResult();

        var minDate = tasks.Min(t => t.PlanStartDate);
        var maxDate = tasks.Max(t => t.PlanEndDate);

        // 生成月份列表
        var months = new List<string>();
        var cursor = new DateTime(minDate.Year, minDate.Month, 1);
        while (cursor <= maxDate)
        {
            months.Add(cursor.ToString("yyyy-MM"));
            cursor = cursor.AddMonths(1);
        }

        // 按资源分组
        var byResource = assignments.GroupBy(a => a.ResourceId)
            .Select(g => new { Resource = g.First().Resource, Assignments = g.ToList() })
            .Where(r => r.Resource != null)
            .ToList();

        var resourceNames = byResource.Select(r => r.Resource!.Name).ToList();
        var capacities = byResource.Select(r => r.Resource!.Quantity).ToList();
        var loadMatrix = new List<List<double>>();

        foreach (var resGroup in byResource)
        {
            var row = new List<double>();
            foreach (var month in months)
            {
                var monthStart = DateTime.Parse(month + "-01");
                var monthEnd = monthStart.AddMonths(1);
                double usage = 0;
                foreach (var a in resGroup.Assignments)
                {
                    var task = tasks.FirstOrDefault(t => t.Id == a.TaskId);
                    if (task == null) continue;
                    // 任务与月份的重叠天数
                    var overlapStart = task.PlanStartDate > monthStart ? task.PlanStartDate : monthStart;
                    var overlapEnd = task.PlanEndDate < monthEnd ? task.PlanEndDate : monthEnd;
                    if (overlapStart < overlapEnd)
                    {
                        var daysInMonth = (monthEnd - monthStart).TotalDays;
                        var overlapDays = (overlapEnd - overlapStart).TotalDays;
                        usage += (double)a.Quantity * overlapDays / daysInMonth;
                    }
                }
                var capacity = (double)(resGroup.Resource!.Quantity > 0 ? resGroup.Resource.Quantity : 1);
                row.Add(Math.Round(usage / capacity, 2));
            }
            loadMatrix.Add(row);
        }

        return new ResourceLoadResult
        {
            Months = months,
            ResourceNames = resourceNames,
            LoadMatrix = loadMatrix,
            Capacities = capacities
        };
    }

    // ========== 报告导出 ==========

    public async Task<byte[]> ExportAnalysisReportAsync(int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId)
            ?? throw new InvalidOperationException($"Project {projectId} not found");

        // 并行加载全部分析数据
        var overviewTask = GetProjectOverviewAsync(projectId);
        var evmTask = GetEarnedValueAsync(projectId);
        var curveTask = GetProgressCurveAsync(projectId);
        var varianceTask = GetScheduleVarianceAsync(projectId);
        var stageTask = GetStageCompletionAsync(projectId);
        var loadTask = GetResourceLoadAsync(projectId);
        var levelingTask = LevelResourcesAsync(projectId);
        var detailsTask = GetTaskProgressDetailsAsync(projectId);

        await Task.WhenAll(overviewTask, evmTask, curveTask, varianceTask, stageTask, loadTask, levelingTask, detailsTask);

        var overview = overviewTask.Result;
        var evm = evmTask.Result;
        var curve = curveTask.Result;
        var variance = varianceTask.Result;
        var stage = stageTask.Result;
        var load = loadTask.Result;
        var leveling = levelingTask.Result;
        var details = detailsTask.Result;

        using var wb = new XLWorkbook();

        // 样式定义
        var headerStyle = new Action<IXLCell>(c =>
        {
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            c.Style.Font.FontColor = XLColor.White;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        });
        var numberStyle = new Action<IXLCell>(c => c.Style.NumberFormat.Format = "#,##0.00");
        var pctStyle = new Action<IXLCell>(c => c.Style.NumberFormat.Format = "0.0%");
        var dateStyle = new Action<IXLCell>(c => c.Style.NumberFormat.Format = "yyyy-MM-dd");

        // ===== Sheet 1: 项目概览 =====
        {
            var ws = wb.Worksheets.Add("概览");
            var headers = new[] { "指标", "值" };
            for (int i = 0; i < headers.Length; i++) headerStyle(ws.Cell(1, i + 1).SetValue(headers[i]));
            var rows = new (string, object)[]
            {
                ("项目名称", overview.ProjectName),
                ("总任务数", overview.TotalTasks),
                ("关键任务数", overview.CriticalTasks),
                ("关键路径长度(天)", overview.CriticalPathLength),
                ("总时差(天)", overview.TotalFloat),
                ("计划开始", overview.PlanStartDate.ToString("yyyy-MM-dd")),
                ("计划结束", overview.PlanEndDate.ToString("yyyy-MM-dd")),
                ("项目进度(%)", overview.ProjectProgress?.ToString("F1") ?? "--"),
                ("延后任务数", overview.DelayedTasks),
                ("提前任务数", overview.AcceleratedTasks),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                ws.Cell(i + 2, 1).SetValue(rows[i].Item1);
                ws.Cell(i + 2, 2).SetValue(rows[i].Item2.ToString()!);
            }
            ws.Columns().AdjustToContents();
        }

        // ===== Sheet 2: 挣值分析 (EVM) =====
        {
            var ws = wb.Worksheets.Add("挣值分析");
            var headers = new[] { "指标", "值", "说明" };
            for (int i = 0; i < headers.Length; i++) headerStyle(ws.Cell(1, i + 1).SetValue(headers[i]));
            var rows = new (string, object, string)[]
            {
                ("计划价值 PV", evm.PlannedValue.ToString("F2"), "按计划应完成的工作预算"),
                ("挣值 EV", evm.EarnedValue.ToString("F2"), "实际完成的工作预算"),
                ("实际成本 AC", evm.ActualCost.ToString("F2"), "实际发生的成本"),
                ("进度偏差 SV = EV-PV", evm.ScheduleVariance.ToString("F2"), evm.ScheduleVariance >= 0 ? "提前" : "滞后"),
                ("成本偏差 CV = EV-AC", evm.CostVariance.ToString("F2"), evm.CostVariance >= 0 ? "节约" : "超支"),
                ("进度绩效 SPI = EV/PV", evm.SchedulePerformanceIndex.ToString("F4"), evm.SchedulePerformanceIndex >= 1 ? "进度良好" : "进度滞后"),
                ("成本绩效 CPI = EV/AC", evm.CostPerformanceIndex.ToString("F4"), evm.CostPerformanceIndex >= 1 ? "成本节约" : "成本超支"),
                ("综合绩效 CSI = SPI×CPI", evm.CostScheduleIndex.ToString("F4"), evm.CostScheduleIndex >= 1 ? "健康" : "需关注"),
                ("完工预算 BAC", evm.BudgetAtCompletion.ToString("F2"), "总预算"),
                ("完工估算 EAC = BAC/SPI", evm.EstimateAtCompletion.ToString("F2"), "预估最终成本"),
                ("状态日期", evm.StatusDate.ToString("yyyy-MM-dd"), ""),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                ws.Cell(i + 2, 1).SetValue(rows[i].Item1);
                ws.Cell(i + 2, 2).SetValue(rows[i].Item2.ToString()!);
                ws.Cell(i + 2, 3).SetValue(rows[i].Item3);
            }
            // SPI月度趋势
            if (evm.MonthlyTrend.Any())
            {
                int r = rows.Length + 3;
                ws.Cell(r, 1).SetValue("月度SPI趋势");
                ws.Range(r, 1, r, 3).Merge().Style.Font.Bold = true;
                r++;
                headerStyle(ws.Cell(r, 1).SetValue("月份"));
                headerStyle(ws.Cell(r, 2).SetValue("PV"));
                headerStyle(ws.Cell(r, 3).SetValue("SPI"));
                r++;
                foreach (var m in evm.MonthlyTrend)
                {
                    ws.Cell(r, 1).SetValue(m.Month);
                    numberStyle(ws.Cell(r, 2).SetValue(m.PV));
                    pctStyle(ws.Cell(r, 3).SetValue(m.SPI));
                    r++;
                }
            }
            ws.Columns().AdjustToContents();
        }

        // ===== Sheet 3: 进度曲线 =====
        {
            var ws = wb.Worksheets.Add("进度曲线");
            // 汇总
            var summaryRow = 1;
            ws.Cell(summaryRow, 1).SetValue("前锋线日期");
            ws.Cell(summaryRow, 2).SetValue(curve.ProgressDate.ToString("yyyy-MM-dd"));
            summaryRow++;
            ws.Cell(summaryRow, 1).SetValue("计划完成%");
            ws.Cell(summaryRow, 2).SetValue(curve.PlannedPct);
            summaryRow++;
            ws.Cell(summaryRow, 1).SetValue("实际完成%");
            ws.Cell(summaryRow, 2).SetValue(curve.ActualPct);
            summaryRow++;
            ws.Cell(summaryRow, 1).SetValue("偏差天数");
            ws.Cell(summaryRow, 2).SetValue(curve.DeviationDays);
            // 计划曲线
            summaryRow += 2;
            ws.Cell(summaryRow, 1).SetValue("计划累计完成%");
            ws.Range(summaryRow, 1, summaryRow, 2).Merge().Style.Font.Bold = true;
            summaryRow++;
            headerStyle(ws.Cell(summaryRow, 1).SetValue("日期"));
            headerStyle(ws.Cell(summaryRow, 2).SetValue("累计%"));
            summaryRow++;
            foreach (var p in curve.PlannedCurve)
            {
                ws.Cell(summaryRow, 1).SetValue(p.Date.ToString("yyyy-MM-dd"));
                pctStyle(ws.Cell(summaryRow, 2).SetValue(p.CumulativePct / 100));
                summaryRow++;
            }
            // 实际曲线
            summaryRow++;
            ws.Cell(summaryRow, 1).SetValue("实际累计完成%");
            ws.Range(summaryRow, 1, summaryRow, 2).Merge().Style.Font.Bold = true;
            summaryRow++;
            headerStyle(ws.Cell(summaryRow, 1).SetValue("日期"));
            headerStyle(ws.Cell(summaryRow, 2).SetValue("累计%"));
            summaryRow++;
            foreach (var p in curve.ActualCurve)
            {
                ws.Cell(summaryRow, 1).SetValue(p.Date.ToString("yyyy-MM-dd"));
                pctStyle(ws.Cell(summaryRow, 2).SetValue(p.CumulativePct / 100));
                summaryRow++;
            }
            ws.Columns().AdjustToContents();
        }

        // ===== Sheet 4: 工期偏差 =====
        {
            var ws = wb.Worksheets.Add("工期偏差");
            var headers = new[] { "任务代码", "任务名称", "计划结束", "实际结束", "偏差(天)", "状态", "关键路径" };
            for (int i = 0; i < headers.Length; i++) headerStyle(ws.Cell(1, i + 1).SetValue(headers[i]));
            int row = 2;
            foreach (var item in variance.Items)
            {
                ws.Cell(row, 1).SetValue(item.TaskCode);
                ws.Cell(row, 2).SetValue(item.TaskName);
                dateStyle(ws.Cell(row, 3).SetValue(item.PlanEnd));
                ws.Cell(row, 4).SetValue(item.ActualEnd?.ToString("yyyy-MM-dd") ?? "--");
                ws.Cell(row, 5).SetValue(item.VarianceDays);
                ws.Cell(row, 6).SetValue(item.Status switch { "ahead" => "提前", "behind" => "延后", _ => "按时" });
                ws.Cell(row, 7).SetValue(item.IsCritical ? "是" : "否");
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        // ===== Sheet 5: 阶段完成率 =====
        {
            var ws = wb.Worksheets.Add("阶段完成率");
            // 汇总
            ws.Cell(1, 1).SetValue("阶段完成");
            ws.Cell(1, 2).SetValue($"{stage.CompletedStages}/{stage.TotalStages}");
            ws.Cell(2, 1).SetValue("整体完成率");
            ws.Cell(2, 2).SetValue($"{stage.OverallPct:F1}%");
            ws.Cell(3, 1).SetValue("检测方式");
            ws.Cell(3, 2).SetValue(stage.DetectionMethod == "code_prefix" ? "代码前缀分组" : "顺序分组");
            // 明细
            int sr = 5;
            var sHeaders = new[] { "阶段", "名称", "任务完成/进行中/总数", "计划完成%", "实际完成%", "计划结束", "实际结束", "延滞(天)", "状态" };
            for (int i = 0; i < sHeaders.Length; i++) headerStyle(ws.Cell(sr, i + 1).SetValue(sHeaders[i]));
            sr++;
            foreach (var s in stage.Stages)
            {
                ws.Cell(sr, 1).SetValue(s.StageCode);
                ws.Cell(sr, 2).SetValue(s.StageName);
                ws.Cell(sr, 3).SetValue($"{s.CompletedTasks}+{s.InProgressTasks}/{s.TotalTasks}");
                ws.Cell(sr, 4).SetValue(s.PlannedCompletionPct / 100);
                ws.Cell(sr, 5).SetValue(s.ActualCompletionPct / 100);
                ws.Cell(sr, 6).SetValue(s.PlannedEndDate?.ToString("yyyy-MM-dd") ?? "--");
                ws.Cell(sr, 7).SetValue(s.ActualEndDate?.ToString("yyyy-MM-dd") ?? "--");
                ws.Cell(sr, 8).SetValue(s.DelayDays?.ToString() ?? "--");
                ws.Cell(sr, 9).SetValue(s.Status switch { "completed" => "已完成", "in_progress" => "进行中", "delayed" => "延滞", _ => "待开始" });
                sr++;
            }
            ws.Columns().AdjustToContents();
        }

        // ===== Sheet 6: 资源负荷矩阵 =====
        {
            var ws = wb.Worksheets.Add("资源负荷");
            // 表头
            ws.Cell(1, 1).SetValue("资源名称");
            for (int i = 0; i < load.Months.Count; i++)
                headerStyle(ws.Cell(1, i + 2).SetValue(load.Months[i]));
            // 数据行
            for (int i = 0; i < load.ResourceNames.Count; i++)
            {
                ws.Cell(i + 2, 1).SetValue(load.ResourceNames[i]);
                for (int j = 0; j < load.Months.Count; j++)
                {
                    var val = load.LoadMatrix[i][j];
                    var cell = ws.Cell(i + 2, j + 2).SetValue(val);
                    if (val >= 1.0) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD7D7");
                    else if (val >= 0.8) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
                }
            }
            ws.Columns().AdjustToContents();
        }

        // ===== Sheet 7: 资源平衡 =====
        {
            var ws = wb.Worksheets.Add("资源平衡");
            ws.Cell(1, 1).SetValue("检测冲突数");
            ws.Cell(1, 2).SetValue(leveling.ConflictsDetected);
            ws.Cell(2, 1).SetValue("已解决冲突数");
            ws.Cell(2, 2).SetValue(leveling.ConflictsResolved);
            ws.Cell(3, 1).SetValue("分析摘要");
            ws.Cell(3, 2).SetValue(leveling.Summary);

            if (leveling.Adjustments.Any())
            {
                int lr = 5;
                var lHeaders = new[] { "任务代码", "任务名称", "资源名称", "原计划开始", "建议推迟至", "推迟天数", "原因" };
                for (int i = 0; i < lHeaders.Length; i++) headerStyle(ws.Cell(lr, i + 1).SetValue(lHeaders[i]));
                lr++;
                foreach (var a in leveling.Adjustments)
                {
                    ws.Cell(lr, 1).SetValue(a.TaskCode);
                    ws.Cell(lr, 2).SetValue(a.TaskName);
                    ws.Cell(lr, 3).SetValue(a.ResourceName);
                    dateStyle(ws.Cell(lr, 4).SetValue(a.OriginalStart));
                    dateStyle(ws.Cell(lr, 5).SetValue(a.AdjustedStart));
                    ws.Cell(lr, 6).SetValue(a.DelayDays);
                    ws.Cell(lr, 7).SetValue(a.Reason);
                    lr++;
                }
            }
            ws.Columns().AdjustToContents();
        }

        // ===== Sheet 8: 工序进度明细 =====
        {
            var ws = wb.Worksheets.Add("工序明细");
            var headers = new[] { "任务代码", "任务名称", "计划开始", "计划结束", "工期(天)", "实际开始", "实际结束", "完成%", "时差", "关键路径", "状态" };
            for (int i = 0; i < headers.Length; i++) headerStyle(ws.Cell(1, i + 1).SetValue(headers[i]));
            int row = 2;
            foreach (var d in details)
            {
                ws.Cell(row, 1).SetValue(d.TaskCode);
                ws.Cell(row, 2).SetValue(d.TaskName);
                dateStyle(ws.Cell(row, 3).SetValue(d.PlanStart));
                dateStyle(ws.Cell(row, 4).SetValue(d.PlanEnd));
                ws.Cell(row, 5).SetValue(d.PlanDuration);
                ws.Cell(row, 6).SetValue(d.ActualStart?.ToString("yyyy-MM-dd") ?? "--");
                ws.Cell(row, 7).SetValue(d.ActualEnd?.ToString("yyyy-MM-dd") ?? "--");
                ws.Cell(row, 8).SetValue(d.ProgressPercentage / 100);
                ws.Cell(row, 9).SetValue(d.TotalFloat?.ToString() ?? "--");
                ws.Cell(row, 10).SetValue(d.IsCritical ? "是" : "否");
                ws.Cell(row, 11).SetValue(d.Status switch
                {
                    "completed" => "已完成",
                    "in_progress" => "进行中",
                    "delayed" => "延滞",
                    _ => "未开始"
                });
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
