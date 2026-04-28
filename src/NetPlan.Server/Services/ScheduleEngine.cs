using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

/// <summary>
/// 进度计划调度引擎 - 实现关键路径法（CPM）
/// </summary>
public class ScheduleEngine : IScheduleEngine
{
    public int Calculate(List<TaskItem> tasks, List<TaskRelation> relations, DateTime projectStartDate)
    {
        if (tasks == null || tasks.Count == 0)
            return 0;

        // Step 1: 重置所有计算字段
        foreach (var task in tasks)
        {
            task.EarlyStart = null;
            task.EarlyFinish = null;
            task.LateStart = null;
            task.LateFinish = null;
            task.TotalFloat = null;
            task.FreeFloat = null;
            task.IsCritical = false;
        }

        // Step 2: 正向计算 - 计算最早时间（按拓扑排序顺序）
        var sortedTasks = TopologicalSort(tasks, relations);
        foreach (var task in sortedTasks)
        {
            CalculateEarlyTimes(task, relations, projectStartDate);
        }

        // Step 3: 确定项目工期
        int projectDuration = sortedTasks.Max(t => t.EarlyFinish) ?? 0;

        // Step 4: 反向计算 - 计算最迟时间（逆拓扑排序顺序）
        foreach (var task in sortedTasks.AsEnumerable().Reverse())
        {
            CalculateLateTimes(task, relations, projectDuration);
        }

        // Step 5: 计算时差
        foreach (var task in tasks)
        {
            CalculateFloat(task);
        }

        // Step 6: 识别关键路径
        IdentifyCriticalPath(tasks);

        return projectDuration;
    }

    public void CalculateEarlyTimes(TaskItem task, List<TaskRelation> relations, DateTime projectStartDate)
    {
        // 获取所有紧前任务
        var predecessors = relations.Where(r => r.SuccessorTaskId == task.Id).ToList();

        if (predecessors.Count == 0)
        {
            // 起始任务
            task.EarlyStart = 0;
            task.EarlyFinish = task.PlanDuration;
        }
        else
        {
            int maxEarlyFinish = int.MinValue;

            foreach (var predecessor in predecessors)
            {
                if (predecessor.PredecessorTask.EarlyFinish == null)
                    continue;

                int ef = predecessor.PredecessorTask.EarlyFinish.Value;
                int lag = predecessor.Lag;

                switch (predecessor.Type)
                {
                    case RelationType.FS:
                        // successor.ES = predecessor.EF + lag
                        ef = predecessor.PredecessorTask.EarlyFinish.Value + lag;
                        break;
                    case RelationType.SS:
                        // successor.ES = predecessor.ES + lag
                        ef = predecessor.PredecessorTask.EarlyStart.Value + lag;
                        break;
                    case RelationType.SF:
                        // successor.LF = predecessor.EF + lag (这里计算的是 LS)
                        ef = predecessor.PredecessorTask.EarlyStart.Value + lag - task.PlanDuration;
                        break;
                    case RelationType.FF:
                        // successor.LF = predecessor.EF + lag (这里计算的是 LS)
                        ef = predecessor.PredecessorTask.EarlyFinish.Value + lag - task.PlanDuration;
                        break;
                }

                if (ef > maxEarlyFinish)
                    maxEarlyFinish = ef;
            }

            if (maxEarlyFinish == int.MinValue)
            {
                task.EarlyStart = 0;
            }
            else
            {
                task.EarlyStart = maxEarlyFinish;
            }

            task.EarlyFinish = task.EarlyStart + task.PlanDuration;
        }
    }

    public void CalculateLateTimes(TaskItem task, List<TaskRelation> relations, int projectDuration)
    {
        // 获取所有紧后任务
        var successors = relations.Where(r => r.PredecessorTaskId == task.Id).ToList();

        if (successors.Count == 0)
        {
            // 结束任务
            task.LateFinish = projectDuration;
            task.LateStart = projectDuration - task.PlanDuration;
        }
        else
        {
            int minLateStart = int.MaxValue;

            foreach (var successor in successors)
            {
                if (successor.SuccessorTask.LateStart == null)
                    continue;

                int ls = successor.SuccessorTask.LateStart.Value;
                int lag = successor.Lag;

                switch (successor.Type)
                {
                    case RelationType.FS:
                        // predecessor.LF = successor.LS - lag
                        ls = successor.SuccessorTask.LateStart.Value - lag;
                        break;
                    case RelationType.SS:
                        // predecessor.LF = successor.LS - lag + predecessor.Duration
                        ls = successor.SuccessorTask.LateStart.Value - lag + task.PlanDuration;
                        break;
                    case RelationType.SF:
                        // predecessor.LF = successor.LF - lag
                        ls = successor.SuccessorTask.LateFinish.Value - lag + task.PlanDuration;
                        break;
                    case RelationType.FF:
                        // predecessor.LF = successor.LF - lag + predecessor.Duration
                        ls = successor.SuccessorTask.LateFinish.Value - lag;
                        break;
                }

                if (ls < minLateStart)
                    minLateStart = ls;
            }

            if (minLateStart == int.MaxValue)
            {
                task.LateFinish = projectDuration;
            }
            else
            {
                task.LateFinish = minLateStart;
            }

            task.LateStart = task.LateFinish - task.PlanDuration;
        }
    }

    public void CalculateFloat(TaskItem task)
    {
        if (task.EarlyStart.HasValue && task.LateStart.HasValue)
        {
            task.TotalFloat = task.LateStart - task.EarlyStart;
        }
    }

    public void IdentifyCriticalPath(List<TaskItem> tasks)
    {
        foreach (var task in tasks)
        {
            // 总时差为 0 的任务为关键工序
            task.IsCritical = task.TotalFloat == 0;
        }
    }

    /// <summary>
    /// 拓扑排序 - 确保前置任务在后续任务之前处理
    /// </summary>
    private List<TaskItem> TopologicalSort(List<TaskItem> tasks, List<TaskRelation> relations)
    {
        var result = new List<TaskItem>();
        var visited = new HashSet<int>();
        var taskDict = tasks.ToDictionary(t => t.Id);

        void Visit(TaskItem task)
        {
            if (visited.Contains(task.Id))
                return;

            visited.Add(task.Id);

            // 先访问所有紧前任务
            var predecessors = relations.Where(r => r.SuccessorTaskId == task.Id);
            foreach (var predecessor in predecessors)
            {
                if (taskDict.ContainsKey(predecessor.PredecessorTaskId))
                {
                    Visit(taskDict[predecessor.PredecessorTaskId]);
                }
            }

            result.Add(task);
        }

        foreach (var task in tasks)
        {
            Visit(task);
        }

        return result;
    }
}
