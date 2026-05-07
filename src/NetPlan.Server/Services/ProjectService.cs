using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public class ProjectService : IProjectService
{
    private readonly NetPlanDbContext _db;
    private readonly IScheduleEngine _scheduleEngine;

    public ProjectService(NetPlanDbContext db, IScheduleEngine scheduleEngine)
    {
        _db = db;
        _scheduleEngine = scheduleEngine;
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
            .Where(r => r.PredecessorTask.ProjectId == projectId)
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

    #region 调度计算

    public async Task<int> CalculateScheduleAsync(int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        var tasks = await GetTasksByProjectIdAsync(projectId);
        var relations = await GetRelationsByProjectIdAsync(projectId);

        int projectDuration = _scheduleEngine.Calculate(tasks, relations, project.PlanStartDate);

        // 将调度结果（ES/EF偏移天数）写入任务的实际日期
        foreach (var task in tasks)
        {
            if (task.EarlyStart.HasValue && task.EarlyFinish.HasValue)
            {
                task.PlanStartDate = project.PlanStartDate.AddDays(task.EarlyStart.Value);
                task.PlanEndDate = project.PlanStartDate.AddDays(task.EarlyFinish.Value - 1);
            }
        }

        // 更新项目结束日期
        project.PlanEndDate = project.PlanStartDate.AddDays(projectDuration);
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return projectDuration;
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
