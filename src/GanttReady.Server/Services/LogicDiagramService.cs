using GanttReady.Server.Data;
using GanttReady.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace GanttReady.Server.Services;

/// <summary>
/// 逻辑图（竖排任务链图）布局计算服务
/// 根据任务紧前关系，自动计算节点的行列位置
/// </summary>
public class LogicDiagramService
{
    private readonly NetPlanDbContext _db;

    public LogicDiagramService(NetPlanDbContext db)
    {
        _db = db;
    }

    public async Task<LogicDiagramResult> GenerateAsync(int projectId)
    {
        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var relations = await _db.TaskRelations
            .Where(r => r.ProjectId == projectId)
            .ToListAsync();

        if (tasks.Count == 0) return new LogicDiagramResult();

        // 1. 建立前驱映射
        var predecessors = new Dictionary<int, List<int>>();
        var successors = new Dictionary<int, List<int>>();
        foreach (var t in tasks)
        {
            predecessors[t.Id] = new List<int>();
            successors[t.Id] = new List<int>();
        }
        foreach (var r in relations)
        {
            if (predecessors.ContainsKey(r.SuccessorTaskId))
                predecessors[r.SuccessorTaskId].Add(r.PredecessorTaskId);
            if (successors.ContainsKey(r.PredecessorTaskId))
                successors[r.PredecessorTaskId].Add(r.SuccessorTaskId);
        }

        // 2. 拓扑排序 + 分层（层数 = 最长路径长度）
        var level = new Dictionary<int, int>();
        var visited = new HashSet<int>();

        void AssignLevel(int taskId)
        {
            if (visited.Contains(taskId)) return;
            visited.Add(taskId);

            int maxPredLevel = -1;
            foreach (var predId in predecessors[taskId])
            {
                AssignLevel(predId);
                maxPredLevel = Math.Max(maxPredLevel, level[predId]);
            }
            level[taskId] = maxPredLevel + 1;
        }

        foreach (var t in tasks)
            AssignLevel(t.Id);

        int maxLevel = level.Values.DefaultIfEmpty(0).Max();

        // 3. 按层分组
        var levelGroups = new Dictionary<int, List<int>>();
        foreach (var t in tasks)
        {
            int lvl = level[t.Id];
            if (!levelGroups.ContainsKey(lvl))
                levelGroups[lvl] = new List<int>();
            levelGroups[lvl].Add(t.Id);
        }

        // 4. 计算坐标
        const int nodeWidth = 220;
        const int nodeHeight = 70;
        const int hGap = 30;
        const int vGap = 60;
        const int startX = 50;
        const int startY = 30;

        var taskMap = tasks.ToDictionary(t => t.Id);
        var nodes = new List<LogicNode>();
        var edges = new List<LogicEdge>();

        for (int lvl = 0; lvl <= maxLevel; lvl++)
        {
            if (!levelGroups.ContainsKey(lvl)) continue;
            var ids = levelGroups[lvl];
            int count = ids.Count;
            int totalWidth = count * nodeWidth + (count - 1) * hGap;
            int offsetX = startX + (levelGroups.Values.Max(g => g.Count) * nodeWidth + (levelGroups.Values.Max(g => g.Count) - 1) * hGap - totalWidth) / 2;

            for (int i = 0; i < count; i++)
            {
                var task = taskMap[ids[i]];
                int x = offsetX + i * (nodeWidth + hGap);
                int y = startY + lvl * (nodeHeight + vGap);
                nodes.Add(new LogicNode
                {
                    TaskId = task.Id,
                    Code = task.Code ?? "",
                    Name = task.Name ?? "",
                    PlanStartDate = task.PlanStartDate,
                    PlanEndDate = task.PlanEndDate,
                    X = x, Y = y,
                    Width = nodeWidth,
                    Height = nodeHeight
                });
            }
        }

        var nodeIndex = nodes.ToDictionary(n => n.TaskId);
        foreach (var r in relations)
        {
            if (nodeIndex.ContainsKey(r.PredecessorTaskId) && nodeIndex.ContainsKey(r.SuccessorTaskId))
            {
                edges.Add(new LogicEdge
                {
                    FromTaskId = r.PredecessorTaskId,
                    ToTaskId = r.SuccessorTaskId
                });
            }
        }

        return new LogicDiagramResult
        {
            Nodes = nodes,
            Edges = edges,
            TotalWidth = levelGroups.Values.Max(g => g.Count) * nodeWidth + (levelGroups.Values.Max(g => g.Count) - 1) * hGap + startX * 2,
            TotalHeight = startY + (maxLevel + 1) * (nodeHeight + vGap) + 30
        };
    }
}

public class LogicDiagramResult
{
    public List<LogicNode> Nodes { get; set; } = new();
    public List<LogicEdge> Edges { get; set; } = new();
    public int TotalWidth { get; set; } = 800;
    public int TotalHeight { get; set; } = 600;
}

public class LogicNode
{
    public int TaskId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class LogicEdge
{
    public int FromTaskId { get; set; }
    public int ToTaskId { get; set; }
}
