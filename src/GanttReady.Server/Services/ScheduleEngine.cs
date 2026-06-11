using GanttReady.Server.Models;

namespace GanttReady.Server.Services;

/// <summary>
/// 进度计划调度引擎 - 实现关键路径法（CPM）
/// </summary>
/// <remarks>
/// <b>时间参数存储设计</b>: ES/EF/LS/LF/TF/FF 使用 <c>int?</c> 存储<b>相对天数偏移</b>（相对于 Project.PlanStartDate），
/// 而非 <c>DateTime?</c>。原因：① CPM 算法天然以天为单位运算，避免日期边界复杂性；
/// ② 绝对日期通过 <c>PlanStartDate.AddDays(offset)</c> 转换，由 <c>CalculateScheduleAsync</c> 负责写回；
/// ③ 支持跨项目对比和计算不依赖具体日历。
/// </remarks>
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
        List<TaskItem> sortedTasks;
        try
        {
            sortedTasks = TopologicalSort(tasks, relations);
        }
        catch (InvalidOperationException ex)
        {
            // 循环检测 → 向外层抛出明确异常
            throw new InvalidOperationException($"网络图存在逻辑回路，无法计算进度：{ex.Message}");
        }
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
            CalculateFloat(task, relations);
        }

        // Step 6: 识别关键路径
        IdentifyCriticalPath(tasks);

        return projectDuration;
    }

    public void CalculateEarlyTimes(TaskItem task, List<TaskRelation> relations, DateTime projectStartDate)
    {
        // 获取所有紧前任务
        var predecessors = relations.Where(r => r.SuccessorTaskId == task.Id).ToList();

        // 手动排程任务：以当前 PlanStartDate 作为最早开始基线
        int manualEarlyStart = -1;
        if (task.IsManualSchedule)
        {
            manualEarlyStart = Math.Max(0, (task.PlanStartDate - projectStartDate).Days);
        }

        int cpmEarlyStart;
        if (predecessors.Count == 0)
        {
            cpmEarlyStart = 0;
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
                        ef = predecessor.PredecessorTask.EarlyFinish.Value + lag;
                        break;
                    case RelationType.SS:
                        ef = predecessor.PredecessorTask.EarlyStart.Value + lag;
                        break;
                    case RelationType.SF:
                        ef = predecessor.PredecessorTask.EarlyStart.Value + lag - task.PlanDuration;
                        break;
                    case RelationType.FF:
                        ef = predecessor.PredecessorTask.EarlyFinish.Value + lag - task.PlanDuration;
                        break;
                }

                if (ef > maxEarlyFinish)
                    maxEarlyFinish = ef;
            }

            cpmEarlyStart = maxEarlyFinish == int.MinValue ? 0 : maxEarlyFinish;
        }

        // 手动排程任务保留用户设定的日期（即使有紧前任务也尊重手动设置）
        if (task.IsManualSchedule && manualEarlyStart >= 0)
        {
            task.EarlyStart = manualEarlyStart;
            task.EarlyFinish = manualEarlyStart + task.PlanDuration;
        }
        else if (predecessors.Count > 0)
        {
            task.EarlyStart = cpmEarlyStart;
        }
        else if (task.IsManualSchedule)
        {
            // 无紧前任务 + 手动指定了开始时间 → 以手动设置的开始时间为准
            task.EarlyStart = manualEarlyStart;
        }
        else
        {
            // 无紧前任务 + 未手动指定 → 从第0天开始
            task.EarlyStart = cpmEarlyStart;
        }
        task.EarlyFinish = task.EarlyStart + task.PlanDuration;
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
                        // predecessor.LF = successor.LF - lag
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

    public void CalculateFloat(TaskItem task, List<TaskRelation> relations)
    {
        if (task.EarlyStart.HasValue && task.LateStart.HasValue)
        {
            task.TotalFloat = task.LateStart - task.EarlyStart;

            // 计算自由时差 = min(所有紧后任务的ES/EF - 关系调整) - 本任务的EF/ES
            var successors = relations.Where(r => r.PredecessorTaskId == task.Id).ToList();
            int? minSuccessorStart = null;
            foreach (var s in successors)
            {
                var succTask = s.SuccessorTask;
                if (succTask?.EarlyStart == null) continue;
                int adjusted;
                switch (s.Type)
                {
                    case RelationType.SS:
                        adjusted = succTask.EarlyStart.Value - (task.EarlyStart.Value + s.Lag);
                        break;
                    case RelationType.FF:
                        adjusted = (succTask.EarlyFinish ?? succTask.EarlyStart.Value) - (task.EarlyFinish.Value + s.Lag);
                        break;
                    case RelationType.SF:
                        adjusted = (succTask.EarlyFinish ?? succTask.EarlyStart.Value) - (task.EarlyStart.Value + s.Lag);
                        break;
                    default: // FS
                        adjusted = succTask.EarlyStart.Value - (task.EarlyFinish.Value + s.Lag);
                        break;
                }
                if (minSuccessorStart == null || adjusted < minSuccessorStart)
                    minSuccessorStart = adjusted;
            }
            task.FreeFloat = minSuccessorStart ?? task.TotalFloat; // 无紧后任务时=总时差
        }
    }

    public void IdentifyCriticalPath(List<TaskItem> tasks)
    {
        foreach (var task in tasks)
        {
            // 总时差 ≤ 0 的任务为关键工序（含负时差 = 超约束路径）
            task.IsCritical = task.TotalFloat <= 0;
        }
    }

    /// <summary>
    /// 拓扑排序 - 确保前置任务在后续任务之前处理，检测循环回路
    /// </summary>
    /// <exception cref="InvalidOperationException">当检测到循环依赖时抛出，包含循环节点列表</exception>
    private List<TaskItem> TopologicalSort(List<TaskItem> tasks, List<TaskRelation> relations)
    {
        var result = new List<TaskItem>();
        var visited = new HashSet<int>();     // 黑色：已完成
        var inStack = new HashSet<int>();     // 灰色：当前递归栈中
        var taskDict = tasks.ToDictionary(t => t.Id);

        void Visit(TaskItem task)
        {
            if (visited.Contains(task.Id))
                return;

            // 回边检测：当前任务在递归栈中 → 循环
            if (inStack.Contains(task.Id))
            {
                // 收集循环路径中的任务名称
                var cycleTasks = new List<string>();
                foreach (var t in tasks)
                {
                    if (inStack.Contains(t.Id) || t.Id == task.Id)
                        cycleTasks.Add($"{t.Code}({t.Name})");
                }
                throw new InvalidOperationException(
                    $"检测到循环回路：{string.Join(" → ", cycleTasks)}"
                );
            }

            inStack.Add(task.Id);

            // 先访问所有紧前任务
            var predecessors = relations.Where(r => r.SuccessorTaskId == task.Id);
            foreach (var predecessor in predecessors)
            {
                if (taskDict.ContainsKey(predecessor.PredecessorTaskId))
                {
                    Visit(taskDict[predecessor.PredecessorTaskId]);
                }
            }

            inStack.Remove(task.Id);
            visited.Add(task.Id);
            result.Add(task);
        }

        foreach (var task in tasks)
        {
            Visit(task);
        }

        return result;
    }
}
