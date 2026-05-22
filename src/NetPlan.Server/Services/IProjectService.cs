using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public interface IProjectService
{
    // 项目 CRUD
    Task<List<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project> CreateProjectAsync(Project project);
    Task<Project> UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(int id);

    // 任务 CRUD
    Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId);
    Task<TaskItem?> GetTaskByIdAsync(int id);
    Task<TaskItem> CreateTaskAsync(TaskItem task);
    Task<TaskItem> UpdateTaskAsync(TaskItem task);
    Task DeleteTaskAsync(int id);

    // 任务关系 CRUD
    Task<List<TaskRelation>> GetRelationsByProjectIdAsync(int projectId);
    Task<List<TaskRelation>> GetRelationsByTaskIdAsync(int taskId);
    Task<TaskRelation> CreateRelationAsync(TaskRelation relation);
    Task DeleteRelationAsync(int id);

    // 调度计算
    Task<int> CalculateScheduleAsync(int projectId);

    // 预算计算
    Task RecalculateTaskBudgetCostAsync(int taskId);
    Task RecalculateAllTaskBudgetsAsync(int projectId);

    // 保存所有变更
    Task SaveChangesAsync();

    // 列定义
    Task<List<ColumnDefinition>> GetColumnDefinitionsAsync(int projectId, string viewName);
    Task UpdateColumnDefinitionAsync(ColumnDefinition column);

    // 导出
    Task<string> ExportTasksToExcelAsync(int projectId);
}
