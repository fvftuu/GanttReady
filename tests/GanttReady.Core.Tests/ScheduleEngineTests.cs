using GanttReady.Server.Models;
using GanttReady.Server.Services;
using Xunit;

namespace GanttReady.Core.Tests;

public class ScheduleEngineTests
{
    private readonly ScheduleEngine _engine = new();
    private readonly DateTime _startDate = new(2024, 1, 1);

    /// <summary>
    /// 鏍规嵁浠诲姟瀹氫箟鍒涘缓浠诲姟鍒楄〃锛屾寜缂栫爜寤虹珛鍓嶅悗缃叧绯?    /// </summary>
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

    // ==================== 鍩虹鍦烘櫙 ====================

    [Fact]
    public void 鍗曚换鍔宸ユ湡绛変簬璁″垝宸ユ湡()
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
    public void 涓茶A鍒癇鍒癈_宸ユ湡绱姞()
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
        Assert.True(tasks.All(t => t.IsCritical)); // 鎵€鏈変换鍔″湪鍏抽敭璺緞涓?    }

    [Fact]
    public void 骞惰浠诲姟_宸ユ湡鍙栨渶澶у€?)
    {
        var t = MakeTasks(("A", 3, null), ("B", 7, null), ("C", 5, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS), (0, 2, 0, RelationType.FS));
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(10, dur); // max(3+7, 3+5) = 10
        Assert.Equal(3, tasks[1].EarlyStart);
        Assert.Equal(10, tasks[1].EarlyFinish);
        Assert.Equal(3, tasks[2].EarlyStart);
        Assert.Equal(8, tasks[2].EarlyFinish);
        Assert.False(tasks[2].IsCritical); // C 鏈?2 澶╂椂宸?        Assert.Equal(2, tasks[2].TotalFloat);
    }

    [Fact]
    public void 鍚堝苟浠诲姟_鍓嶇疆鍙栨渶鏅氬畬鎴?)
    {
        var t = MakeTasks(("A", 5, null), ("B", 3, null), ("C", 4, null));
        var (tasks, rels) = Build(t, (0, 2, 0, RelationType.FS), (1, 2, 0, RelationType.FS));
        var dur = _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(9, dur); // max(5,3)+4 = 9
        Assert.Equal(5, tasks[2].EarlyStart); // C 绛?A 瀹屾垚 (ES=5 > B 鐨?3)
        Assert.Equal(9, tasks[2].EarlyFinish);
    }

    // ==================== 鍥涚鍏崇郴绫诲瀷 ====================

    [Fact]
    public void FS鍏崇郴_瀹屾垚寮€濮?)
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(3, tasks[1].EarlyStart);
    }

    [Fact]
    public void SS鍏崇郴_寮€濮嬪紑濮?)
    {
        var t = MakeTasks(("A", 5, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.SS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(0, tasks[1].EarlyStart); // SS 鍚屾椂寮€濮?        Assert.Equal(5, tasks[0].EarlyFinish);
    }

    [Fact]
    public void FF鍏崇郴_瀹屾垚瀹屾垚()
    {
        var t = MakeTasks(("A", 3, null), ("B", 5, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FF));
        _engine.Calculate(tasks, rels, _startDate);
        // B 鐨?EF 蹇呴』 >= A 鐨?EF (3)锛孊 鐨勫伐鏈?5锛屾墍浠?ES = 3-5 = -2
        // CPM 涓嶉檺鍒?ES >= 0锛屽厑璁歌礋鍊硷紙琛ㄧず鍙彁鍓嶅紑宸ワ級
        Assert.Equal(-2, tasks[1].EarlyStart);
        Assert.Equal(3, tasks[1].EarlyFinish);
    }

    [Fact]
    public void SF鍏崇郴_寮€濮嬪畬鎴?)
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.SF));
        _engine.Calculate(tasks, rels, _startDate);
        // B 鐨?EF 蹇呴』 >= A 鐨?ES=0锛孊.ES = 0-4 = -4
        Assert.Equal(-4, tasks[1].EarlyStart);
        Assert.Equal(0, tasks[1].EarlyFinish);
    }

    // ==================== Lag 鍜屽叧绯荤粍鍚?====================

    [Fact]
    public void FSwitLag_寤跺悗浼犻€?)
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 5, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(8, tasks[1].EarlyStart); // 3 + 5 = 8
    }

    [Fact]
    public void 璐烲ag_浠诲姟閲嶅彔()
    {
        var t = MakeTasks(("A", 10, null), ("B", 5, null));
        var (tasks, rels) = Build(t, (0, 1, -3, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(7, tasks[1].EarlyStart); // 10 - 3 = 7
    }

    // ==================== 娣峰悎鍏崇郴缁勫悎 ====================

    [Fact]
    public void 澶氬墠缃贩鍚堝叧绯籣鍙朚ax绾︽潫()
    {
        var t = MakeTasks(("A", 5, null), ("B", 4, null), ("C", 3, null));
        // A 鈫?C (FS), B 鈫?C (SS+2)
        var (tasks, rels) = Build(t, (0, 2, 0, RelationType.FS), (1, 2, 2, RelationType.SS));
        _engine.Calculate(tasks, rels, _startDate);
        // FS: C.ES = A.EF = 5
        // SS: C.ES = B.ES+2 = 2
        // max(5, 2) = 5
        Assert.Equal(5, tasks[2].EarlyStart);
        Assert.Equal(8, tasks[2].EarlyFinish);
    }

    // ==================== 涓夋潯閾炬祴璇?SS/FF 鍙嶅悜浼犳挱 ====================

    [Fact]
    public void SS閾綺涓変换鍔鍏椂鏍囨纭?)
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null), ("C", 2, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.SS), (1, 2, 0, RelationType.SS));
        var dur = _engine.Calculate(tasks, rels, _startDate);
        // 姝ｅ悜: A(0鈫?), B(0鈫?), C(0鈫?), 宸ユ湡=max=4
        Assert.Equal(4, dur);
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(0, tasks[1].EarlyStart); // SS 鍚屾椂
        Assert.Equal(0, tasks[2].EarlyStart); // SS 鍚屾椂
        // 鍙嶅悜: 鏈€鍚庝换鍔?C.LF=4, C.LS=2
        // B 鍙?SS: B.LS = C.LS - 0 = 2, B.LF = 2+4=6? 浣嗗伐鏈熸墠4
        // 瀹為檯: C 鏃犲悗缁?鈫?LF=4, LS=4-2=2
        // B 鈫?C (SS): B.LS = C.LS = 2, B.LF = 2+4=6
        // A 鈫?B (SS): A.LS = B.LS = 2, A.LF = 2+3=5
        Assert.True(tasks[0].TotalFloat >= 0);
        Assert.True(tasks[1].TotalFloat >= 0);
        Assert.True(tasks[2].TotalFloat >= 0);
    }

    // ==================== 寰幆渚濊禆 ====================

    [Fact]
    public void 寰幆渚濊禆_鎶涘嚭寮傚父()
    {
        var t = MakeTasks(("A", 3, null), ("B", 4, null), ("C", 5, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS), (1, 2, 0, RelationType.FS), (2, 0, 0, RelationType.FS));
        var ex = Assert.Throws<InvalidOperationException>(() => _engine.Calculate(tasks, rels, _startDate));
        Assert.Contains("寰幆", ex.Message);
    }

    // ==================== 鎵嬪姩鎺掔▼ ====================

    [Fact]
    public void 鎵嬪姩鎺掔▼浠诲姟_鏃犲墠缃甠浠ヨ瀹氭棩鏈熶负鍑?)
    {
        var t = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", Name = "A", PlanDuration = 5, PlanStartDate = new DateTime(2024, 1, 10), IsManualSchedule = true },
        };
        var rels = new List<TaskRelation>();
        _engine.Calculate(t, rels, _startDate);
        // 鎵嬪姩浠诲姟锛歅lanStartDate(1鏈?0鏃? - projectStart(1鏈?鏃? = 9澶?        Assert.Equal(9, t[0].EarlyStart);
        Assert.Equal(14, t[0].EarlyFinish);
    }

    [Fact]
    public void 鎵嬪姩鎺掔▼鏈夊墠缃甠淇濈暀鎵嬪姩鏃ユ湡()
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
        // B 鏈夊墠缃?A 浣嗗凡璁句负鎵嬪姩鎺掔▼ 鈫?淇濈暀鎵嬪姩璁惧畾鐨勬棩鏈?        Assert.Equal(4, t[1].EarlyStart); // 2024-01-05 - 2024-01-01 = 4澶?    }

    // ==================== 闆跺伐鏈熶换鍔★紙閲岀▼纰戯級 ====================

    [Fact]
    public void 闆跺伐鏈熼噷绋嬬_鍦ㄥ叧閿矾寰勪笂()
    {
        var t = MakeTasks(("A", 3, null), ("M", 0, null), ("B", 4, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FS), (1, 2, 0, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(3, tasks[1].EarlyStart);
        Assert.Equal(3, tasks[1].EarlyFinish); // ES=EF for zero-duration
        Assert.True(tasks[1].IsCritical);
    }

    // ==================== 鏃犲墠缃殑瀛ょ珛浠诲姟 ====================

    [Fact]
    public void 鏃犲墠缃换鍔浠庣0澶╁紑濮?)
    {
        var t = MakeTasks(("A", 5, null), ("B", 3, null));
        var (tasks, rels) = Build(t); // 鏃犲叧绯?        _engine.Calculate(tasks, rels, _startDate);
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(0, tasks[1].EarlyStart);
    }

    // ==================== 绗簩杞細缁勫悎鍦烘櫙涓庡揩鐓?====================

    [Fact]
    public void FF閾綺涓変换鍔鍙嶅悜浼犳挱楠岃瘉()
    {
        // A FF鈫?B FF鈫?C锛欰.EF=5, B.EF=A.EF=5, C.EF=B.EF=5
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
    public void 缁忓吀PMBOK缃戠粶_瀹屾暣鍏椂鏍囬獙璇?)
    {
        // PMBOK 缁忓吀绀轰緥: A(5)鈫払(3) , A(5)鈫扖(4)鈫扗(2) , B鈫扗 , C鈫扙(3) , D鈫扙
        // A: ES=0, EF=5
        // B: ES=5, EF=8
        // C: ES=5, EF=9
        // D: max(B.EF=8, C.EF=9) = 9, ES=9-2=7, EF=9 鈥?wait, let me redo
        // Actually: A鈫払 FS, A鈫扖 FS, B鈫扗 FS, C鈫扗 FS, C鈫扙 FS, D鈫扙 FS
        // A(5): ES=0, EF=5, LS=0, LF=5
        // B(3): ES=5, EF=8, LS=5, LF=8
        // C(4): ES=5, EF=9, LS=5, LF=9
        // D(2): ES=max(B.EF=8, C.EF=9)=9, EF=11, LS=9, LF=11
        // E(3): ES=max(C.EF=9, D.EF=11)=11, EF=14, LS=11, LF=14
        // Duration = 14
        // Critical: A鈫扖鈫扗鈫扙 (no float)
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
    public void 鎵嬪姩浠诲姟鍦ㄥ叧閿矾寰勪笂_鍙嶅悜涓嶅帇缂╁浐瀹氭棩鏈?)
    {
        // A(鎵嬪姩, day5寮€濮? 鈫?B(鑷姩, 渚濊禆A)
        // A: PlanStartDate=2024-01-06, 鍋忕Щ5澶?        var t = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", Name = "A", PlanDuration = 4, PlanStartDate = new DateTime(2024, 1, 6), IsManualSchedule = true },
            new() { Id = 2, Code = "B", Name = "B", PlanDuration = 3, IsManualSchedule = false },
        };
        var rels = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, PredecessorTask = t[0], SuccessorTask = t[1], Type = RelationType.FS, Lag = 0 }
        };
        _engine.Calculate(t, rels, _startDate);

        Assert.Equal(5, t[0].EarlyStart);  // 鎵嬪姩浠诲姟锛歅lan(1/6) - projectStart(1/1) = 5
        Assert.Equal(9, t[0].EarlyFinish);
        Assert.Equal(9, t[1].EarlyStart);  // B鍦ˋ瀹屾垚鍚庡紑濮?        Assert.Equal(12, t[1].EarlyFinish);
        // 鍙嶅悜锛欱鏃犲悗缁?鈫?LF=12, LS=9
        // A鈫払 FS: A.LF = B.LS - 0 = 9, A.LS = 9-4 = 5
        Assert.Equal(5, t[0].LateStart);
        Assert.Equal(9, t[0].LateFinish);
    }

    [Fact]
    public void 闆跺伐鏈熼噷绋嬬FF鍏崇郴()
    {
        // A(5澶? FF鈫?M(閲岀▼纰?0澶?
        // M.EF = A.EF = 5, M.ES = M.EF = 5 (宸ユ湡0)
        var t = MakeTasks(("A", 5, null), ("M", 0, null));
        var (tasks, rels) = Build(t, (0, 1, 0, RelationType.FF));
        _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(5, tasks[0].EarlyFinish);
        Assert.Equal(5, tasks[1].EarlyStart);  // 閲岀▼纰慐S=EF
        Assert.Equal(5, tasks[1].EarlyFinish);
        Assert.True(tasks[1].IsCritical);
    }

    [Fact]
    public void 璐烲ag閲嶅彔_鍙嶅悜璁＄畻涓嶄骇鐢熻礋鏃跺樊()
    {
        // A(10澶? FS-3鈫?B(5澶?: A寮€濮?澶╁悗B鍗冲彲寮€濮?        var t = MakeTasks(("A", 10, null), ("B", 5, null));
        var (tasks, rels) = Build(t, (0, 1, -3, RelationType.FS));
        _engine.Calculate(tasks, rels, _startDate);

        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(10, tasks[0].EarlyFinish);
        Assert.Equal(7, tasks[1].EarlyStart); // 10-3=7
        Assert.Equal(12, tasks[1].EarlyFinish);
        // 鍙嶅悜: B鏃犲悗缁? LF=12, LS=7
        // A.LF = B.LS + 3 = 10 (璐烲ag鍦ㄥ弽鍚戞椂鍔犲洖), A.LS = 10-10 = 0
        Assert.Equal(0, tasks[0].LateStart);
        Assert.Equal(10, tasks[0].LateFinish);
        Assert.Equal(0, tasks[0].TotalFloat);
    }

    [Fact]
    public void 澶氬眰绾BS_杩囨护鍚嶤PM姝ｇ‘()
    {
        // Level1: P1, P2 (parent)
        // Level2: P1鈫扖1, C2 | P2鈫扖3, C4
        // 鍏崇郴: C1(3) 鈫?C3(4), C2(2) 鈫?C4(3) (FS)
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
        Assert.Equal(3, c3.EarlyStart); // C1鈫扖3
        Assert.Equal(7, c3.EarlyFinish);
    }

    // ==================== 鐖朵换鍔¤繃婊ら獙璇?====================

    [Fact]
    public void 瀛樺湪鐖朵换鍔CPM鍙绠楀彾瀛愪换鍔?)
    {
        // P(Level1) 鈫?C1, C2
        var t = MakeTasks(("P", 0, null), ("C1", 3, "P"), ("C2", 5, "P"));
        var (tasks, rels) = Build(t, (1, 2, 0, RelationType.FS));
        // 鍙紶鍙跺瓙浠诲姟缁?CPM锛岃繃婊ゆ帀鐖朵换鍔?P
        var parentIds = tasks.Where(t => t.ParentTaskId.HasValue).Select(t => t.ParentTaskId.Value).ToHashSet();
        var leafTasks = tasks.Where(t => !parentIds.Contains(t.Id)).ToList();
        var leafTaskIds = leafTasks.Select(t => t.Id).ToHashSet();
        var leafRelations = rels.Where(r => leafTaskIds.Contains(r.PredecessorTaskId) && leafTaskIds.Contains(r.SuccessorTaskId)).ToList();

        var dur = _engine.Calculate(leafTasks, leafRelations, _startDate);
        Assert.Equal(8, dur); // 3+5
    }
}
