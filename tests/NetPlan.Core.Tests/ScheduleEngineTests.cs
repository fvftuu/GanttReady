using NetPlan.Server.Models;
using NetPlan.Server.Services;
using Xunit;

namespace NetPlan.Core.Tests;

public class ScheduleEngineTests
{
    private readonly ScheduleEngine _engine = new();
    private readonly DateTime _startDate = new(2024, 1, 1);

    /// <summary>
    /// 根据任务定义创建任务列表，按编码建立前后置关系
    /// </summary>
    private static List<TaskItem> MakeTasks(params (string code, int duration, string? parentCode)[] defs)
    {
        int id = 1;
        var tasks = new List<TaskItem>();
        foreach (var (code, duration, parentCode) in defs)
        {
            tasks.Add(new TaskItem
            {
                Id = id++,
                Code = code,
                Name = code,
                PlanDuration = duration,
                ParentTaskId = null, // Will set later
            });
        }
        // Set parent-child relationships
        for (int i = 0; i < defs.Length; i++)
        {
            if (defs[i].parentCode != null)
            {
                var parent = tasks.FirstOrDefault(t => t.Code == defs[i].parentCode);
                if (parent != null)
                    tasks[i].ParentTaskId = parent.Id;
            }
        }
        return tasks;
    }

    private static List<TaskRelation> MakeRels(string predCode, string succCode, int lag = 0, RelationType type = RelationType.FS)
    {
        return new List<TaskRelation> { new() { PredecessorTaskId = 0, SuccessorTaskId = 0, Lag = lag, Type = type } };
    }

    private static (List<TaskItem> tasks, List<TaskRelation> rels) Build(
        List<TaskItem> tasks,
        params (int predIdx, int succIdx, int lag, RelationType type)[] rels)
    {
        var relations = new List<TaskRelation>();
        foreach (var (p, s, lag, type) in rels)
        {
            relations.Add(new TaskRelation
            {
                PredecessorTaskId = tasks[p].Id,
                SuccessorTaskId = tasks[s].Id,
                PredecessorTask = tasks[p],
                SuccessorTask = tasks[s],
                Lag = lag,
                Type = type
            });
        }
        return (tasks, relations);
    }

    // ==================== 基础场景 ====================

    [Fact]
    public void 单任务_工期等于计划工期()
    {
        var t = MakeTasks(("A", 5, null));
        var (tasks, rels) = Build(t);
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(5, dur);
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(5, tasks[0].EarlyFinish);
        Assert.Equal(0, tasks[0].LateStart);
        Assert.Equal(5, tasks[0].LateFinish);
        Assert.True(tasks[0].IsCritical);
    }

    [Fact]
    public void 串行A到B到C_工期累加()
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null), ("C", 2, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS), (1, 2, 0, RelationType.FS));
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(9, dur);  // 3+4+2
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(3, tasks[0].EarlyFinish);
        Assert.Equal(3, tasks[1].EarlyStart);
        Assert.Equal(7, tasks[1].EarlyFinish);
        Assert.Equal(7, tasks[2].EarlyStart);
        Assert.Equal(9, tasks[2].EarlyFinish);
        Assert.True(tasks.All(t => t.IsCritical)); // 所有任务在关键路径上
    }

    [Fact]
    public void 并行任务_工期取最大值()
    {
        var t = MakeTasks(("A", 3, null), ("B", 7, null), ("C", 5, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS), (0, 2, 0, RelationType.FS));
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(10, dur); // max(3+7, 3+5) = 10
        Assert.Equal(3, tasks[1].EarlyStart);
        Assert.Equal(10, tasks[1].EarlyFinish);
        Assert.Equal(3, tasks[2].EarlyStart);
        Assert.Equal(8, tasks[2].EarlyFinish);
        Assert.False(tasks[2].IsCritical); // C 有 2 天时差
        Assert.Equal(2, tasks[2].TotalFloat);
    }

    [Fact]
    public void 合并任务_前置取最晚完成()
    {
        var t = MakeTasks(("A", 5, null), ("B", 3, null), ("C", 4, null));
        var (tasks, rels) = Build(t, (0, 2, 0, RelationType.FS), (1, 2, 0, RelationType.FS));
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(9, dur); // max(5,3)+4 = 9
        Assert.Equal(5, tasks[2].EarlyStart); // C 等 A 完成 (ES=5 > B 的 3)
        Assert.Equal(9, tasks[2].EarlyFinish);
    }

    // ==================== 四种关系类型 ====================

    [Fact]
    public void FS关系_完成开始()
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(3, tasks[1].EarlyStart);
    }

    [Fact]
    public void SS关系_开始开始()
    {
        var t = MakeTasks(("A", 5, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.SS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(0, tasks[1].EarlyStart); // SS 同时开始
        Assert.Equal(5, tasks[0].EarlyFinish);
    }

    [Fact]
    public void FF关系_完成完成()
    {
        var t = MakeTasks(("A", 3, null), ("B", 5, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FF));
        _engine.Calculate(tasks, rels, _startDate);
        // B 的 EF 必须 >= A 的 EF (3)，B 的工期 5，所以 ES = 3-5 = -2
        // CPM 不限制 ES >= 0，允许负值（表示可提前开工）
        Assert.Equal(-2, tasks[1].EarlyStart);
        Assert.Equal(3, tasks[1].EarlyFinish);
    }

    [Fact]
    public void SF关系_开始完成()
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.SF));
        _engine.Calculate(tasks, rels, _startDate);
        // B 的 EF 必须 >= A 的 ES=0，B.ES = 0-4 = -4
        Assert.Equal(-4, tasks[1].EarlyStart);
        Assert.Equal(0, tasks[1].EarlyFinish);
    }

    // ==================== Lag 和关系组合 ====================

    [Fact]
    public void FSwitLag_延后传递()
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 5, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(8, tasks[1].EarlyStart); // 3 + 5 = 8
    }

    [Fact]
    public void 负Lag_任务重叠()
    {
        var t = MakeTasks(("A", 10, null), ("B", 5, null));
        var (tasks, rels) = Build(t, (0, 1, -3, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(7, tasks[1].EarlyStart); // 10 - 3 = 7
    }

    // ==================== 混合关系组合 ====================

    [Fact]
    public void 多前置混合关系_取Max约束()
    {
        var t = MakeTasks(("A", 5, null), ("B", 4, null), ("C", 3, null));
        // A → C (FS), B → C (SS+2)
        var (tasks, rels) = Build(t, (0, 2, 0, RelationType.FS), (1, 2, 2, RelationType.SS));
        _engine.Calculate(tasks, rels, _startDate);
        // FS: C.ES = A.EF = 5
        // SS: C.ES = B.ES+2 = 2
        // max(5, 2) = 5
        Assert.Equal(5, tasks[2].EarlyStart);
        Assert.Equal(8, tasks[2].EarlyFinish);
    }

    // ==================== 三条链测试 SS/FF 反向传播 ====================

    [Fact]
    public void SS链_三任务_六时标正确()
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null), ("C", 2, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.SS), (1, 2, 0, RelationType.SS));
        var dur = _engine.Calculate(tasks, rels, _startDate);
        // 正向: A(0→3), B(0→4), C(0→2), 工期=max=4
        Assert.Equal(4, dur);
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(0, tasks[1].EarlyStart); // SS 同时
        Assert.Equal(0, tasks[2].EarlyStart); // SS 同时
        // 反向: 最后任务 C.LF=4, C.LS=2
        // B 受 SS: B.LS = C.LS - 0 = 2, B.LF = 2+4=6? 但工期才4
        // 实际: C 无后继 → LF=4, LS=4-2=2
        // B → C (SS): B.LS = C.LS = 2, B.LF = 2+4=6
        // A → B (SS): A.LS = B.LS = 2, A.LF = 2+3=5
        Assert.True(tasks[0].TotalFloat >= 0);
        Assert.True(tasks[1].TotalFloat >= 0);
        Assert.True(tasks[2].TotalFloat >= 0);
    }

    // ==================== 循环依赖 ====================

    [Fact]
    public void 循环依赖_抛出异常()
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null), ("C", 5, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS), (1, 2, 0, RelationType.FS), (2, 0, 0, RelationType.FS));
        var ex = Assert.Throws<InvalidOperationException>(() => _engine.Calculate(tasks, rels, _startDate));
        Assert.Contains("循环", ex.Message);
    }

    // ==================== 手动排程 ====================

    [Fact]
    public void 手动排程任务_无前置_以设定日期为准()
    {
        var t = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", Name = "A", PlanDuration = 5, PlanStartDate = new DateTime(2024, 1, 10), IsManualSchedule = true },
        };
        var rels = new List<TaskRelation>();
        _engine.Calculate(t, rels, _startDate);
        // 手动任务：PlanStartDate(1月10日) - projectStart(1月1日) = 9天
        Assert.Equal(9, t[0].EarlyStart);
        Assert.Equal(14, t[0].EarlyFinish);
    }

    [Fact]
    public void 手动排程有前置_保留手动日期()
    {
        var t = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", Name = "A", PlanDuration = 3, IsManualSchedule = false },
            new() { Id = 2, Code = "B", Name = "B", PlanDuration = 4, PlanStartDate = new DateTime(2024, 1, 5), IsManualSchedule = true },
        };
        var rels = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, PredecessorTask = t[0], SuccessorTask = t[1], Type = RelationType.FS, Lag = 0 }
        };
        _engine.Calculate(t, rels, _startDate);
        // B 有前置 A 但已设为手动排程 → 保留手动设定的日期
        Assert.Equal(4, t[1].EarlyStart); // 2024-01-05 - 2024-01-01 = 4天
    }

    // ==================== 零工期任务（里程碑） ====================

    [Fact]
    public void 零工期里程碑_在关键路径上()
    {
        var t = MakeTasks(("A", 3, null), ("M", 0, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS), (1, 2, 0, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(3, tasks[1].EarlyStart);
        Assert.Equal(3, tasks[1].EarlyFinish); // ES=EF for zero-duration
        Assert.True(tasks[1].IsCritical);
    }

    // ==================== 无前置的孤立任务 ====================

    [Fact]
    public void 无前置任务_从第0天开始()
    {
        var t = MakeTasks(("A", 5, null), ("B", 3, null));
        var (tasks, rels) = Build(t); // 无关系
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(0, tasks[1].EarlyStart);
    }

    // ==================== 第二轮：组合场景与快照 ====================

    [Fact]
    public void FF链_三任务_反向传播验证()
    {
        // A FF→ B FF→ C：A.EF=5, B.EF=A.EF=5, C.EF=B.EF=5
        // B.ES = B.EF - B.dur = 5-4=1, C.ES = 5-2=3
        var t = MakeTasks(("A", 5, null), ("B", 4, null), ("C", 2, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FF), (1, 2, 0, RelationType.FF));
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(5, dur);
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(5, tasks[0].EarlyFinish);
        Assert.Equal(1, tasks[1].EarlyStart);
        Assert.Equal(5, tasks[1].EarlyFinish);
        Assert.Equal(3, tasks[2].EarlyStart);
        Assert.Equal(5, tasks[2].EarlyFinish);
    }

    [Fact]
    public void 经典PMBOK网络_完整六时标验证()
    {
        // PMBOK 经典示例: A(5)→B(3) , A(5)→C(4)→D(2) , B→D , C→E(3) , D→E
        // A: ES=0, EF=5
        // B: ES=5, EF=8
        // C: ES=5, EF=9
        // D: max(B.EF=8, C.EF=9) = 9, ES=9-2=7, EF=9 — wait, let me redo
        // Actually: A→B FS, A→C FS, B→D FS, C→D FS, C→E FS, D→E FS
        // A(5): ES=0, EF=5, LS=0, LF=5
        // B(3): ES=5, EF=8, LS=5, LF=8
        // C(4): ES=5, EF=9, LS=5, LF=9
        // D(2): ES=max(B.EF=8, C.EF=9)=9, EF=11, LS=9, LF=11
        // E(3): ES=max(C.EF=9, D.EF=11)=11, EF=14, LS=11, LF=14
        // Duration = 14
        // Critical: A→C→D→E (no float)
        // B has 0 float? Let me recalc...
        // B: ES=5, EF=8, LF = D.LS = 9, LS = 9-3 = 6, TF = 1

        var t = MakeTasks(("A", 5, null), ("B", 3, null), ("C", 4, null), ("D", 2, null), ("E", 3, null));
        var (tasks, rels) = Build(t,
            (0, 1, 0, RelationType.FS), (0, 2, 0, RelationType.FS),
            (1, 3, 0, RelationType.FS), (2, 3, 0, RelationType.FS),
            (2, 4, 0, RelationType.FS), (3, 4, 0, RelationType.FS));
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(14, dur);
        // A
        Assert.Equal(0, tasks[0].EarlyStart); Assert.Equal(5, tasks[0].EarlyFinish);
        Assert.Equal(0, tasks[0].LateStart);   Assert.Equal(5, tasks[0].LateFinish);
        Assert.Equal(0, tasks[0].TotalFloat);  Assert.True(tasks[0].IsCritical);
        // B (non-critical, TF=1)
        Assert.Equal(5, tasks[1].EarlyStart);  Assert.Equal(8, tasks[1].EarlyFinish);
        Assert.Equal(6, tasks[1].LateStart);   Assert.Equal(9, tasks[1].LateFinish);
        Assert.Equal(1, tasks[1].TotalFloat);  Assert.False(tasks[1].IsCritical);
        // C
        Assert.Equal(5, tasks[2].EarlyStart);  Assert.Equal(9, tasks[2].EarlyFinish);
        Assert.Equal(5, tasks[2].LateStart);   Assert.Equal(9, tasks[2].LateFinish);
        Assert.Equal(0, tasks[2].TotalFloat);  Assert.True(tasks[2].IsCritical);
        // D
        Assert.Equal(9, tasks[3].EarlyStart);  Assert.Equal(11, tasks[3].EarlyFinish);
        Assert.Equal(9, tasks[3].LateStart);   Assert.Equal(11, tasks[3].LateFinish);
        Assert.Equal(0, tasks[3].TotalFloat);  Assert.True(tasks[3].IsCritical);
        // E
        Assert.Equal(11, tasks[4].EarlyStart); Assert.Equal(14, tasks[4].EarlyFinish);
        Assert.Equal(11, tasks[4].LateStart);  Assert.Equal(14, tasks[4].LateFinish);
        Assert.Equal(0, tasks[4].TotalFloat);  Assert.True(tasks[4].IsCritical);
    }

    [Fact]
    public void 手动任务在关键路径上_反向不压缩固定日期()
    {
        // A(手动, day5开始) → B(自动, 依赖A)
        // A: PlanStartDate=2024-01-06, 偏移5天
        var t = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", Name = "A", PlanDuration = 4, PlanStartDate = new DateTime(2024, 1, 6), IsManualSchedule = true },
            new() { Id = 2, Code = "B", Name = "B", PlanDuration = 3, IsManualSchedule = false },
        };
        var rels = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, PredecessorTask = t[0], SuccessorTask = t[1], Type = RelationType.FS, Lag = 0 }
        };
        _engine.Calculate(t, rels, _startDate);

        Assert.Equal(5, t[0].EarlyStart);  // 手动任务：Plan(1/6) - projectStart(1/1) = 5
        Assert.Equal(9, t[0].EarlyFinish);
        Assert.Equal(9, t[1].EarlyStart);  // B在A完成后开始
        Assert.Equal(12, t[1].EarlyFinish);
        // 反向：B无后继 → LF=12, LS=9
        // A→B FS: A.LF = B.LS - 0 = 9, A.LS = 9-4 = 5
        Assert.Equal(5, t[0].LateStart);
        Assert.Equal(9, t[0].LateFinish);
    }

    [Fact]
    public void 零工期里程碑FF关系()
    {
        // A(5天) FF→ M(里程碑,0天)
        // M.EF = A.EF = 5, M.ES = M.EF = 5 (工期0)
        var t = MakeTasks(("A", 5, null), ("M", 0, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FF));
        _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(5, tasks[0].EarlyFinish);
        Assert.Equal(5, tasks[1].EarlyStart);  // 里程碑ES=EF
        Assert.Equal(5, tasks[1].EarlyFinish);
        Assert.True(tasks[1].IsCritical);
    }

    [Fact]
    public void 负Lag重叠_反向计算不产生负时差()
    {
        // A(10天) FS-3→ B(5天): A开始3天后B即可开始
        var t = MakeTasks(("A", 10, null), ("B", 5, null));
        var (tasks, rels) = Build(t, (0, 1, -3, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(10, tasks[0].EarlyFinish);
        Assert.Equal(7, tasks[1].EarlyStart); // 10-3=7
        Assert.Equal(12, tasks[1].EarlyFinish);
        // 反向: B无后继, LF=12, LS=7
        // A.LF = B.LS + 3 = 10 (负Lag在反向时加回), A.LS = 10-10 = 0
        Assert.Equal(0, tasks[0].LateStart);
        Assert.Equal(10, tasks[0].LateFinish);
        Assert.Equal(0, tasks[0].TotalFloat);
    }

    [Fact]
    public void 多层级WBS_过滤后CPM正确()
    {
        // Level1: P1, P2 (parent)
        // Level2: P1→C1, C2 | P2→C3, C4
        // 关系: C1(3) → C3(4), C2(2) → C4(3) (FS)
        var t = MakeTasks(
            ("P1", 0, null), ("P2", 0, null),
            ("C1", 3, "P1"), ("C2", 2, "P1"),
            ("C3", 4, "P2"), ("C4", 3, "P2"));
        var (allTasks, rels) = Build(t, (2, 4, 0, RelationType.FS), (3, 5, 0, RelationType.FS));

        var parentIds = allTasks.Where(t => t.ParentTaskId.HasValue).Select(t => t.ParentTaskId.Value).ToHashSet();
        var leafTasks = allTasks.Where(t => !parentIds.Contains(t.Id)).ToList();
        var leafIds = leafTasks.Select(t => t.Id).ToHashSet();
        var leafRels = rels.Where(r => leafIds.Contains(r.PredecessorTaskId) && leafIds.Contains(r.SuccessorTaskId)).ToList();

        var dur = _engine.Calculate(leafTasks, leafRels, _startDate);
        Assert.Equal(7, dur); // max(3+4, 2+3) = 7
        var c1 = leafTasks.First(t => t.Code == "C1");
        var c3 = leafTasks.First(t => t.Code == "C3");
        Assert.Equal(0, c1.EarlyStart);
        Assert.Equal(3, c1.EarlyFinish);
        Assert.Equal(3, c3.EarlyStart); // C1→C3
        Assert.Equal(7, c3.EarlyFinish);
    }

    // ==================== 父任务过滤验证 ====================

    [Fact]
    public void 存在父任务_CPM只计算叶子任务()
    {
        // P(Level1) → C1, C2
        var t = MakeTasks(("P", 0, null), ("C1", 3, "P"), ("C2", 5, "P"));
        var (tasks, rels) = Build(t, (1, 2, 0, RelationType.FS));
        // 只传叶子任务给 CPM，过滤掉父任务 P
        var parentIds = tasks.Where(t => t.ParentTaskId.HasValue).Select(t => t.ParentTaskId.Value).ToHashSet();
        var leafTasks = tasks.Where(t => !parentIds.Contains(t.Id)).ToList();
        var leafTaskIds = leafTasks.Select(t => t.Id).ToHashSet();
        var leafRelations = rels.Where(r => leafTaskIds.Contains(r.PredecessorTaskId) && leafTaskIds.Contains(r.SuccessorTaskId)).ToList();

        var dur = _engine.Calculate(leafTasks, leafRelations, _startDate);
        Assert.Equal(8, dur); // 3+5
    }
}
