using GanttReady.Server.Models;

namespace GanttReady.Server.Services;

public interface IScheduleEngine
{
    /// <summary>
    /// 执行进度计划计算（CPM 关键路径法）
    /// </summary>
    int Calculate(List<TaskItem> tasks, List<TaskRelation> relations, DateTime projectStartDate);

    void CalculateEarlyTimes(TaskItem task, List<TaskRelation> relations, DateTime projectStartDate);
    void CalculateLateTimes(TaskItem task, List<TaskRelation> relations, int projectDuration);
    void CalculateFloat(TaskItem task, List<TaskRelation> relations);
    void IdentifyCriticalPath(List<TaskItem> tasks);
}
