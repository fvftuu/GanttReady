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
        var project = await _db.Projects.FindAsync(id);
        if (project != null)
        {
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }
    }

    #endregion

    #region 任务 CRUD

    public async Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId)
    {
        return await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.Predecessors)
                .ThenInclude(r => r.PredecessorTask)
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
        var task = await _db.Tasks.FindAsync(id);
        if (task != null)
        {
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
        }
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
}
