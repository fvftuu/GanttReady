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
            EquipmentCount = resources.Count(r => r.Type == ResourceType.Equipment)
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

    /// <summary>
    /// 分析项目进度计划（关键路径、时差、里程碑）
    /// </summary>
    public ProjectAnalysisResult AnalyzeProject(Project project, List<TaskItem> tasks)
    {
        var result = new ProjectAnalysisResult
        {
            TotalTasks = tasks.Count,
            Tasks = tasks,
            CriticalPathTasks = tasks.Where(t => t.IsCritical).ToList(),
            OnTimeTasks = tasks.Where(t => t.TotalFloat is > 0 and <= 5).ToList(),
            EarlyStartTasks = tasks.Where(t => t.TotalFloat > 5).ToList(),
            LateStartTasks = tasks.Where(t => t.TotalFloat < 0).ToList(),
            Milestones = tasks.Where(t => t.PlanDuration == 1)
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
}
