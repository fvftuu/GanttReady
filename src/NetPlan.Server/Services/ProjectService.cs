using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public class ProjectService : IProjectService
{
    private readonly NetPlanDbContext _db;
    private readonly IScheduleEngine _scheduleEngine;
    private readonly CalendarService _calendar;

    public ProjectService(NetPlanDbContext db, IScheduleEngine scheduleEngine, CalendarService calendar)
    {
        _db = db;
        _scheduleEngine = scheduleEngine;
        _calendar = calendar;
    }

    #region 项目 CRUD

    public async Task<List<Project>> GetAllProjectsAsync()
    {
        return await _db.Projects
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _db.Projects
            .Include(p => p.Tasks)
            .Include(p => p.Resources)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        // 初始化默认列定义
        await InitializeDefaultColumnsAsync(project.Id);

        return project;
    }

    public async Task<Project> UpdateProjectAsync(Project project)
    {
        var existing = await _db.Projects.FindAsync(project.Id);
        if (existing == null)
            throw new InvalidOperationException($"Project {project.Id} not found");

        existing.Code = project.Code;
        existing.Name = project.Name;
        existing.PlanStartDate = project.PlanStartDate;
        existing.PlanEndDate = project.PlanEndDate;
        existing.ActualStartDate = project.ActualStartDate;
        existing.ActualEndDate = project.ActualEndDate;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteProjectAsync(int id)
    {
        var project = await _db.Projects
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Predecessors)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Successors)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.ResourceAssignments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return;

        // 按依赖顺序删除：先删除关系/分配，再删除任务/资源，最后删除项目
        var taskRelations = project.Tasks.SelectMany(t => t.Predecessors)
            .Union(project.Tasks.SelectMany(t => t.Successors))
            .DistinctBy(r => r.Id);
        _db.TaskRelations.RemoveRange(taskRelations);

        var assignments = project.Tasks.SelectMany(t => t.ResourceAssignments);
        _db.ResourceAssignments.RemoveRange(assignments);

        var resources = await _db.Resources.Where(r => r.ProjectId == id).ToListAsync();
        _db.Resources.RemoveRange(resources);

        var columns = await _db.ColumnDefinitions.Where(c => c.ProjectId == id).ToListAsync();
        _db.ColumnDefinitions.RemoveRange(columns);

        _db.Tasks.RemoveRange(project.Tasks);
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
    }

    #endregion

    public async Task<List<ProjectSummary>> GetProjectSummariesAsync()
    {
        var projects = await _db.Projects
            .Include(p => p.Tasks)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return projects.Select(p => new ProjectSummary
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            PlanStartDate = p.PlanStartDate,
            PlanEndDate = p.PlanEndDate,
            TaskCount = p.Tasks.Count,
            ResourceCount = _db.Resources.Count(r => r.ProjectId == p.Id),
            CompletedTaskCount = p.Tasks.Count(t => t.CompletionPercentage >= 100),
            TotalPlanCost = (double)p.Tasks.Sum(t => t.BudgetCost),
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }

    #region 任务 CRUD

    public async Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId)
    {
        return await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.Predecessors)
                .ThenInclude(r => r.PredecessorTask)
            .Include(t => t.Successors)
                .ThenInclude(r => r.SuccessorTask)
            .Include(t => t.ResourceAssignments)
                .ThenInclude(a => a.Resource)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id)
    {
        return await _db.Tasks
            .Include(t => t.Predecessors)
            .Include(t => t.Successors)
            .Include(t => t.ResourceAssignments)
                .ThenInclude(a => a.Resource)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        var maxOrder = await _db.Tasks
            .Where(t => t.ProjectId == task.ProjectId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        task.SortOrder = maxOrder + 1;
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
    {
        var existing = await _db.Tasks.FindAsync(task.Id);
        if (existing == null)
            throw new InvalidOperationException($"Task {task.Id} not found");

        existing.Code = task.Code;
        existing.Name = task.Name;
        existing.SortOrder = task.SortOrder;
        existing.ParentTaskId = task.ParentTaskId;
        existing.OutlineLevel = task.OutlineLevel;
        existing.ResponsiblePerson = task.ResponsiblePerson;
        existing.CompletionPercentage = task.CompletionPercentage;
        existing.IsMilestone = task.IsMilestone;
        existing.IsManualSchedule = task.IsManualSchedule;
        existing.Notes = task.Notes;
        existing.PlanStartDate = task.PlanStartDate;
        existing.PlanEndDate = task.PlanEndDate;
        existing.PlanDuration = task.PlanDuration;
        existing.ActualStartDate = task.ActualStartDate;
        existing.ActualEndDate = task.ActualEndDate;
        existing.ActualDuration = task.ActualDuration;
        existing.ExtraData = task.ExtraData;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteTaskAsync(int id)
    {
        var task = await _db.Tasks
            .Include(t => t.Predecessors)
            .Include(t => t.Successors)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return;

        // 先删除所有关联的任务关系（前后置）
        var allRelations = task.Predecessors.Concat(task.Successors).ToList();
        _db.TaskRelations.RemoveRange(allRelations);

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region 任务关系 CRUD

    public async Task<List<TaskRelation>> GetRelationsByProjectIdAsync(int projectId)
    {
        return await _db.TaskRelations
            .Where(r => r.ProjectId == projectId)
            .Include(r => r.PredecessorTask)
            .Include(r => r.SuccessorTask)
            .ToListAsync();
    }

    public async Task<List<TaskRelation>> GetRelationsByTaskIdAsync(int taskId)
    {
        return await _db.TaskRelations
            .Where(r => r.PredecessorTaskId == taskId || r.SuccessorTaskId == taskId)
            .Include(r => r.PredecessorTask)
            .Include(r => r.SuccessorTask)
            .ToListAsync();
    }

    public async Task<TaskRelation> CreateRelationAsync(TaskRelation relation)
    {
        _db.TaskRelations.Add(relation);
        await _db.SaveChangesAsync();
        return relation;
    }

    public async Task DeleteRelationAsync(int id)
    {
        var relation = await _db.TaskRelations.FindAsync(id);
        if (relation != null)
        {
            _db.TaskRelations.Remove(relation);
            await _db.SaveChangesAsync();
        }
    }

    #endregion

    #region 基准对比

    public async Task<List<Baseline>> GetBaselinesAsync(int projectId)
    {
        return await _db.Baselines
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.Number)
            .ToListAsync();
    }

    public async Task<List<BaselineTask>> GetBaselineTasksAsync(int baselineId)
    {
        return await _db.BaselineTasks
            .Where(bt => bt.BaselineId == baselineId)
            .ToListAsync();
    }

    public async Task<int> SaveBaselineAsync(int projectId, int number, string name)
    {
        // 删除同编号的旧基准
        var existing = await _db.Baselines.FirstOrDefaultAsync(b => b.ProjectId == projectId && b.Number == number);
        if (existing != null)
        {
            _db.BaselineTasks.RemoveRange(await _db.BaselineTasks.Where(bt => bt.BaselineId == existing.Id).ToListAsync());
            _db.Baselines.Remove(existing);
            await _db.SaveChangesAsync();
        }

        var baseline = new Baseline { ProjectId = projectId, Number = number, Name = name };
        _db.Baselines.Add(baseline);
        await _db.SaveChangesAsync(); // 先保存获取 Id

        // 复制当前所有任务的计划日期
        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        foreach (var task in tasks)
        {
            _db.BaselineTasks.Add(new BaselineTask
            {
                BaselineId = baseline.Id,
                TaskId = task.Id,
                PlanStartDate = task.PlanStartDate,
                PlanEndDate = task.PlanEndDate,
                PlanDuration = task.PlanDuration
            });
        }
        await _db.SaveChangesAsync();
        return baseline.Id;
    }

    public async Task DeleteBaselineAsync(int baselineId)
    {
        var tasks = await _db.BaselineTasks.Where(bt => bt.BaselineId == baselineId).ToListAsync();
        _db.BaselineTasks.RemoveRange(tasks);
        var baseline = await _db.Baselines.FindAsync(baselineId);
        if (baseline != null) _db.Baselines.Remove(baseline);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region 调度计算

    public async Task<int> CalculateScheduleAsync(int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        var allTasks = await GetTasksByProjectIdAsync(projectId);
        var relations = await GetRelationsByProjectIdAsync(projectId);

        // 父任务（有子任务的任务）不参与 CPM 计算，只由 RecalculateParentTaskDates 汇总
        var parentTaskIds = allTasks.Where(t => t.ParentTaskId.HasValue)
                                    .Select(t => t.ParentTaskId.Value)
                                    .ToHashSet();
        var leafTasks = allTasks.Where(t => !parentTaskIds.Contains(t.Id)).ToList();

        // 保留至少一端是叶子任务的关系（父任务也可有前置/后置关系）
        var leafTaskIds = leafTasks.Select(t => t.Id).ToHashSet();
        var leafRelations = relations.Where(r => leafTaskIds.Contains(r.PredecessorTaskId)
                                               || leafTaskIds.Contains(r.SuccessorTaskId)).ToList();

        int projectDuration = _scheduleEngine.Calculate(leafTasks, leafRelations, project.PlanStartDate);

        // 一次性读取日历数据，避免循环内反复查库
        var calBits = await _calendar.GetWorkDayBitsAsync(projectId);
        var calHolidays = await _calendar.GetHolidaysAsync(projectId);

        // 将调度结果（ES/EF偏移天数）写入叶子任务的实际日期（父任务由 RecalculateParentTaskDates 汇总）
        foreach (var task in leafTasks)
        {
            if (task.EarlyStart.HasValue && task.EarlyFinish.HasValue)
            {
                // ES=0 且非手动排程 → 无前置任务，保留用户已设的 PlanStartDate
                DateTime calStart;
                if (task.IsManualSchedule)
                {
                    calStart = project.PlanStartDate.AddDays(task.EarlyStart.Value);
                }
                else if (task.EarlyStart.GetValueOrDefault() > 0)
                {
                    calStart = _calendar.AddWorkingDays(calBits, project.PlanStartDate, task.EarlyStart.Value, calHolidays);
                }
                else
                {
                    calStart = task.PlanStartDate; // 保留用户已设的起始日
                }
                var calEnd = _calendar.AddWorkingDays(calBits, calStart, task.PlanDuration, calHolidays);
                task.PlanStartDate = calStart;
                task.PlanEndDate = calEnd;
            }
        }

        // 重新计算父任务日期：父任务的 PlanStartDate = 子任务最早开始, PlanEndDate = 子任务最晚完成
        await RecalculateParentTaskDatesAsync(projectId, allTasks);

        // 第二遍：父任务的紧后任务（叶子）需要根据父任务的实际完成日调整起始日
        var parentTaskIdSet = allTasks.Where(t => allTasks.Any(c => c.ParentTaskId == t.Id)).Select(t => t.Id).ToHashSet();
        foreach (var rel in leafRelations)
        {
            if (parentTaskIdSet.Contains(rel.PredecessorTaskId) && leafTaskIds.Contains(rel.SuccessorTaskId))
            {
                var predTask = allTasks.FirstOrDefault(t => t.Id == rel.PredecessorTaskId);
                var succTask = allTasks.FirstOrDefault(t => t.Id == rel.SuccessorTaskId);
                if (predTask != null && succTask != null && predTask.PlanEndDate > succTask.PlanStartDate)
                {
                    // 推后紧后任务的日期
                    var diff = (predTask.PlanEndDate - succTask.PlanStartDate).Days + rel.Lag;
                    if (diff > 0)
                    {
                        succTask.PlanStartDate = predTask.PlanEndDate.AddDays(rel.Lag);
                        succTask.PlanEndDate = _calendar.AddWorkingDays(calBits, succTask.PlanStartDate, succTask.PlanDuration, calHolidays);
                    }
                }
            }
        }
        // 父任务调整后，重新汇总一次父任务日期
        await RecalculateParentTaskDatesAsync(projectId, allTasks);

        // 修正项目开始日：不能晚于最早任务的开始日
        var minTaskDate = allTasks.Min(t => t.PlanStartDate);
        if (minTaskDate < project.PlanStartDate)
            project.PlanStartDate = minTaskDate;

        // 更新项目结束日期（用日历计算）
        project.PlanEndDate = _calendar.AddWorkingDays(calBits, project.PlanStartDate, projectDuration, calHolidays);
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return projectDuration;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// 递归向上汇总父任务日期：父任务的 PlanStartDate = 所有直接子任务最早的 PlanStartDate，
    /// PlanEndDate = 所有直接子任务最晚的 PlanEndDate，PlanDuration 自动重算。
    /// 支持多层嵌套（父→子→孙），逐层向上冒泡。
    /// </summary>
    private async Task RecalculateParentTaskDatesAsync(int projectId, List<TaskItem> tasks)
    {
        var parentTasks = tasks.Where(t => tasks.Any(c => c.ParentTaskId == t.Id)).ToList();
        if (!parentTasks.Any()) return;

        // 按 OutlineLevel 降序排列，确保子级先于父级处理（最深层优先）
        parentTasks = parentTasks.OrderByDescending(t => t.OutlineLevel).ToList();

        foreach (var parent in parentTasks)
        {
            var children = tasks.Where(t => t.ParentTaskId == parent.Id).ToList();
            if (!children.Any()) continue;

            var minStart = children.Min(t => t.PlanStartDate);
            var maxEnd = children.Max(t => t.PlanEndDate);

            parent.PlanStartDate = minStart;
            parent.PlanEndDate = maxEnd;
            parent.PlanDuration = await _calendar.GetWorkingDaysCountAsync(projectId, minStart, maxEnd);

            // 父任务的关键路径判断：任一子任务关键则父任务为关键，时差取子任务最小值
            parent.IsCritical = children.Any(t => t.IsCritical);
            parent.TotalFloat = children.Min(t => t.TotalFloat);
        }
    }

    /// <summary>
    /// 根据资源分配重新计算任务的预算金额。
    /// 公式：人工/设备 = Σ(分配数量 × 每小时成本 × 计划工期 × 8小时)，
    ///       材料 = Σ(分配数量 × 单价)。
    /// </summary>
    public async Task RecalculateTaskBudgetCostAsync(int taskId)
    {
        var task = await _db.Tasks
            .Include(t => t.ResourceAssignments)
                .ThenInclude(a => a.Resource)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return;

        decimal totalCost = 0;
        foreach (var assignment in task.ResourceAssignments)
        {
            var resource = assignment.Resource;
            if (resource == null) continue;

            if (resource.Type == ResourceType.Material)
            {
                // 材料：数量 × 单价
                totalCost += assignment.Quantity * resource.UnitPrice;
            }
            else if (resource.Type == ResourceType.Measure)
            {
                // 措施：数量 × 单价（一次性费用，不乘工期）
                totalCost += assignment.Quantity * resource.UnitPrice;
            }
            else
            {
                // 人工/设备：数量 × 每小时成本 × 计划工期(天) × 8小时/天
                var hourlyCost = resource.HourlyCost ?? resource.UnitPrice;
                totalCost += assignment.Quantity * hourlyCost * task.PlanDuration * 8;
            }
        }

        task.BudgetCost = totalCost;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// 批量重算项目下所有任务的预算金额
    /// </summary>
    public async Task RecalculateAllTaskBudgetsAsync(int projectId)
    {
        var tasks = await _db.Tasks
            .Include(t => t.ResourceAssignments)
                .ThenInclude(a => a.Resource)
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        foreach (var task in tasks)
        {
            decimal totalCost = 0;
            foreach (var assignment in task.ResourceAssignments)
            {
                var resource = assignment.Resource;
                if (resource == null) continue;

                if (resource.Type == ResourceType.Material)
                    totalCost += assignment.Quantity * resource.UnitPrice;
                else if (resource.Type == ResourceType.Measure)
                {
                    var dailyCost = resource.HourlyCost ?? resource.UnitPrice;
                    totalCost += assignment.Quantity * dailyCost * task.PlanDuration;
                }
                else
                {
                    var hourlyCost = resource.HourlyCost ?? resource.UnitPrice;
                    totalCost += assignment.Quantity * hourlyCost * task.PlanDuration * 8;
                }
            }
            task.BudgetCost = totalCost;
        }
        await _db.SaveChangesAsync();
    }

    #endregion

    #region 列定义

    public async Task<List<ColumnDefinition>> GetColumnDefinitionsAsync(int projectId, string viewName)
    {
        return await _db.ColumnDefinitions
            .Where(c => c.ProjectId == projectId && c.ViewName == viewName)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task UpdateColumnDefinitionAsync(ColumnDefinition column)
    {
        var existing = await _db.ColumnDefinitions.FindAsync(column.Id);
        if (existing != null)
        {
            existing.IsVisible = column.IsVisible;
            existing.IsEditable = column.IsEditable;
            existing.Width = column.Width;
            existing.SortOrder = column.SortOrder;
            await _db.SaveChangesAsync();
        }
    }

    private async Task InitializeDefaultColumnsAsync(int projectId)
    {
        var defaultColumns = new List<ColumnDefinition>
        {
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "Code", DisplayName = "任务代号", Width = 80, SortOrder = 1, IsVisible = true, IsEditable = true },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "Name", DisplayName = "任务名称", Width = 200, SortOrder = 2, IsVisible = true, IsEditable = true },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "PlanStartDate", DisplayName = "计划开始", Width = 100, SortOrder = 3, IsVisible = true, IsEditable = true },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "PlanEndDate", DisplayName = "计划完成", Width = 100, SortOrder = 4, IsVisible = true, IsEditable = true },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "PlanDuration", DisplayName = "计划工期", Width = 80, SortOrder = 5, IsVisible = true, IsEditable = true },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "ActualStartDate", DisplayName = "实际开始", Width = 100, SortOrder = 6, IsVisible = true, IsEditable = true },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "ActualEndDate", DisplayName = "实际完成", Width = 100, SortOrder = 7, IsVisible = true, IsEditable = true },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "TotalFloat", DisplayName = "总时差", Width = 80, SortOrder = 8, IsVisible = true, IsEditable = false },
            new() { ProjectId = projectId, ViewName = "Gantt", FieldName = "IsCritical", DisplayName = "关键", Width = 60, SortOrder = 9, IsVisible = true, IsEditable = false },
        };

        _db.ColumnDefinitions.AddRange(defaultColumns);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region 导出

    public async Task<string> ExportTasksToExcelAsync(int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        var tasks = await GetTasksByProjectIdAsync(projectId);

        // 将任务数据保存到临时文件，供下载
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"NetPlan_Export_{project.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        
        using var writer = new StreamWriter(tempFilePath, false, System.Text.Encoding.UTF8);
        
        // 写入CSV头（带BOM以便Excel正确识别中文）
        await writer.WriteLineAsync("\uFEFF序号,代号,任务名称,计划开始,计划完成,工期(天),实际开始,实际完成,总时差,最早开始,最早完成,最迟开始,最迟完成,关键任务");
        
        // 写入数据行
        int index = 1;
        foreach (var task in tasks)
        {
            var line = $"{index},{EscapeCsv(task.Code)},{EscapeCsv(task.Name)},{task.PlanStartDate:yyyy-MM-dd},{task.PlanEndDate:yyyy-MM-dd},{task.PlanDuration},{task.ActualStartDate?.ToString("yyyy-MM-dd") ?? ""},{task.ActualEndDate?.ToString("yyyy-MM-dd") ?? ""},{task.TotalFloat ?? 0},{task.EarlyStart},{task.EarlyFinish},{task.LateStart},{task.LateFinish},{(task.IsCritical ? "是" : "否")}";
            await writer.WriteLineAsync(line);
            index++;
        }

        return tempFilePath;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        // 如果包含逗号、引号或换行符，需要用引号包裹
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    #endregion
}
