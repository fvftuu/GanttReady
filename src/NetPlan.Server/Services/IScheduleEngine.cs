using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public interface IScheduleEngine
{
    /// <summary>
    /// 执行进度计划计算（CPM 关键路径法）
    /// </summary>
    /// <param name="tasks">任务列表</param>
    /// <param name="relations">任务关系列表</param>
    /// <param name="projectStartDate">项目计划开始日期</param>
    /// <returns>项目工期（天）</returns>
    int Calculate(List<TaskItem> tasks, List<TaskRelation> relations, DateTime projectStartDate);

    /// <summary>
    /// 计算单个任务的最早时间
    /// </summary>
    void CalculateEarlyTimes(TaskItem task, List<TaskRelation> relations, DateTime projectStartDate);

    /// <summary>
    /// 计算单个任务的最迟时间
    /// </summary>
    void CalculateLateTimes(TaskItem task, List<TaskRelation> relations, int projectDuration);

    /// <summary>
    /// 计算任务时差
    /// </summary>
    void CalculateFloat(TaskItem task);

    /// <summary>
    /// 识别关键路径
    /// </summary>
    void IdentifyCriticalPath(List<TaskItem> tasks);
}
