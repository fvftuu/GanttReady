using NetPlan.Server.Models;
using NetPlan.Server.Services;
using Xunit;

namespace NetPlan.Server.Tests;

public class ScheduleEngineTests
{
    private readonly ScheduleEngine _engine = new();
    private static readonly DateTime ProjectStart = new(2026, 1, 1);

    /// <summary>
    /// 连接 TaskRelation 的导航属性，模拟 EF Core Include 行为
    /// </summary>
    private static void WireRelations(List<TaskItem> tasks, List<TaskRelation> relations)
    {
        var dict = tasks.ToDictionary(t => t.Id);
        foreach (var r in relations)
        {
            r.PredecessorTask = dict[r.PredecessorTaskId];
            r.SuccessorTask = dict[r.SuccessorTaskId];
        }
    }

    [Fact]
    public void Calculate_SimpleFSChain_ReturnsCorrectDuration()
    {
        // A(10d) → B(20d) → C(15d)
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 10, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(9) },
            new() { Id = 2, Code = "B", PlanDuration = 20, PlanStartDate = ProjectStart.AddDays(10), PlanEndDate = ProjectStart.AddDays(29) },
            new() { Id = 3, Code = "C", PlanDuration = 15, PlanStartDate = ProjectStart.AddDays(30), PlanEndDate = ProjectStart.AddDays(44) },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, Type = RelationType.FS, Lag = 0 },
            new() { PredecessorTaskId = 2, SuccessorTaskId = 3, Type = RelationType.FS, Lag = 0 },
        };

        WireRelations(tasks, relations);
        var duration = _engine.Calculate(tasks, relations, ProjectStart);

        Assert.Equal(45, duration); // 10 + 20 + 15 = 45
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(10, tasks[0].EarlyFinish);
        Assert.Equal(10, tasks[1].EarlyStart);
        Assert.Equal(30, tasks[1].EarlyFinish);
        Assert.Equal(30, tasks[2].EarlyStart);
        Assert.Equal(45, tasks[2].EarlyFinish);
    }

    [Fact]
    public void Calculate_CriticalPath_IdentifiesCriticalTasks()
    {
        // A(10) → B(20) → D(5)
        // A(10) → C(5)  → D(5)
        // B is on critical path, C has float
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 10, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(9) },
            new() { Id = 2, Code = "B", PlanDuration = 20, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(19) },
            new() { Id = 3, Code = "C", PlanDuration = 5,  PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(4) },
            new() { Id = 4, Code = "D", PlanDuration = 5,  PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(4) },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, Type = RelationType.FS, Lag = 0 },
            new() { PredecessorTaskId = 1, SuccessorTaskId = 3, Type = RelationType.FS, Lag = 0 },
            new() { PredecessorTaskId = 2, SuccessorTaskId = 4, Type = RelationType.FS, Lag = 0 },
            new() { PredecessorTaskId = 3, SuccessorTaskId = 4, Type = RelationType.FS, Lag = 0 },
        };

        WireRelations(tasks, relations);
        _engine.Calculate(tasks, relations, ProjectStart);

        // Critical path: A→B→D = 35 days
        Assert.Equal(35, tasks[3].EarlyFinish);
        Assert.True(tasks[0].IsCritical); // A
        Assert.True(tasks[1].IsCritical); // B (longer path)
        Assert.False(tasks[2].IsCritical); // C (has float = 15)
        Assert.True(tasks[3].IsCritical); // D
        Assert.Equal(15, tasks[2].TotalFloat);
    }

    [Fact]
    public void Calculate_SSRelation_StartsInParallel()
    {
        // A(30d) --SS+5→ B(20d) — B starts 5 days after A starts
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 30, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(29) },
            new() { Id = 2, Code = "B", PlanDuration = 20, PlanStartDate = ProjectStart.AddDays(5), PlanEndDate = ProjectStart.AddDays(24) },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, Type = RelationType.SS, Lag = 5 },
        };

        WireRelations(tasks, relations);
        _engine.Calculate(tasks, relations, ProjectStart);

        Assert.Equal(5, tasks[1].EarlyStart);  // A.ES(0) + lag(5) = 5
        Assert.Equal(25, tasks[1].EarlyFinish); // 5 + 20 = 25
    }

    [Fact]
    public void Calculate_FFRelation_FinishesTogether()
    {
        // A(30d) --FF+0→ B(10d) — B must finish when A finishes
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 30, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(29) },
            new() { Id = 2, Code = "B", PlanDuration = 10, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(9) },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, Type = RelationType.FF, Lag = 0 },
        };

        WireRelations(tasks, relations);
        _engine.Calculate(tasks, relations, ProjectStart);

        // B must finish with A at day 30, so B starts at 30-10=20
        Assert.Equal(30, tasks[0].EarlyFinish);
        Assert.Equal(20, tasks[1].EarlyStart);
        Assert.Equal(30, tasks[1].EarlyFinish);
    }

    [Fact]
    public void Calculate_WithLag_FSRelation()
    {
        // A(10) --FS+5→ B(10) — B starts 5 days after A finishes
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 10, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(9) },
            new() { Id = 2, Code = "B", PlanDuration = 10, PlanStartDate = ProjectStart.AddDays(15), PlanEndDate = ProjectStart.AddDays(24) },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, Type = RelationType.FS, Lag = 5 },
        };

        WireRelations(tasks, relations);
        _engine.Calculate(tasks, relations, ProjectStart);

        Assert.Equal(10, tasks[0].EarlyFinish);  // A finishes at 10
        Assert.Equal(15, tasks[1].EarlyStart);   // B starts at 10 + 5 = 15
        Assert.Equal(25, tasks[1].EarlyFinish);  // B finishes at 15 + 10 = 25
    }

    [Fact]
    public void Calculate_ManualSchedule_RespectsManualDate()
    {
        // A(10d) → B(10d), B is manually set to start at day 30
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 10, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(9) },
            new() { Id = 2, Code = "B", PlanDuration = 10, PlanStartDate = ProjectStart.AddDays(30), PlanEndDate = ProjectStart.AddDays(39), IsManualSchedule = true },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, Type = RelationType.FS, Lag = 0 },
        };

        WireRelations(tasks, relations);
        _engine.Calculate(tasks, relations, ProjectStart);

        // CPM says B starts at 10 (A.EF=10), manual says 30 → max = 30
        Assert.Equal(30, tasks[1].EarlyStart);
        Assert.Equal(40, tasks[1].EarlyFinish);
    }

    [Fact]
    public void Calculate_ManualSchedule_CpmLaterThanManual_UsesCpm()
    {
        // CPM pushes B later than manual → use CPM value
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 40, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(39) },
            new() { Id = 2, Code = "B", PlanDuration = 10, PlanStartDate = ProjectStart.AddDays(5), PlanEndDate = ProjectStart.AddDays(14), IsManualSchedule = true },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 2, Type = RelationType.FS, Lag = 0 },
        };

        WireRelations(tasks, relations);
        _engine.Calculate(tasks, relations, ProjectStart);

        // CPM: B starts at A.EF=40, manual says 5 → max(40, 5) = 40
        Assert.Equal(40, tasks[1].EarlyStart);
    }

    [Fact]
    public void Calculate_TwoPredecessors_TakesMaxEarlyFinish()
    {
        // A(20d) → C(10d), B(5d) → C(10d), C starts after both finish
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 20, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(19) },
            new() { Id = 2, Code = "B", PlanDuration = 5,  PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(4) },
            new() { Id = 3, Code = "C", PlanDuration = 10, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(9) },
        };
        var relations = new List<TaskRelation>
        {
            new() { PredecessorTaskId = 1, SuccessorTaskId = 3, Type = RelationType.FS, Lag = 0 },
            new() { PredecessorTaskId = 2, SuccessorTaskId = 3, Type = RelationType.FS, Lag = 0 },
        };

        WireRelations(tasks, relations);
        _engine.Calculate(tasks, relations, ProjectStart);

        Assert.Equal(20, tasks[2].EarlyStart); // max(A.EF=20, B.EF=5) = 20
    }

    [Fact]
    public void Calculate_NoRelations_AllStartAtZero()
    {
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Code = "A", PlanDuration = 10, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(9) },
            new() { Id = 2, Code = "B", PlanDuration = 20, PlanStartDate = ProjectStart, PlanEndDate = ProjectStart.AddDays(19) },
        };

        var duration = _engine.Calculate(tasks, new List<TaskRelation>(), ProjectStart);

        Assert.Equal(20, duration); // max(A=10, B=20)
        Assert.Equal(0, tasks[0].EarlyStart);
        Assert.Equal(0, tasks[1].EarlyStart);
    }

    [Fact]
    public void Calculate_EmptyTaskList_ReturnsZero()
    {
        var duration = _engine.Calculate(new List<TaskItem>(), new List<TaskRelation>(), ProjectStart);
        Assert.Equal(0, duration);
    }
}
