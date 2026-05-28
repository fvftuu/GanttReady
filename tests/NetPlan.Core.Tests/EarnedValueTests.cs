using NetPlan.Server.Models;
using Xunit;

namespace NetPlan.Core.Tests;

public class EarnedValueTests
{
    private readonly DateTime _today = new(2024, 6, 15);

    /// <summary>
    /// 复制 AnalysisService.CalcPlannedPct 的逻辑用于测试验证
    /// </summary>
    private static double CalcPlannedPct(TaskItem task, DateTime asOfDate)
    {
        if (asOfDate < task.PlanStartDate) return 0;
        if (asOfDate >= task.PlanEndDate) return 1;
        var totalDays = (task.PlanEndDate - task.PlanStartDate).TotalDays;
        if (totalDays <= 0) return 1;
        return (asOfDate - task.PlanStartDate).TotalDays / totalDays;
    }

    /// <summary>
    /// 模拟 GetEarnedValueAsync 的核心计算逻辑（不依赖数据库）
    /// </summary>
    private static (double pv, double ev, double ac, double spi, double cpi, double sv, double cv, double csi, double bac)
        CalculateEarnedValue(List<TaskItem> allTasks, DateTime statusDate)
    {
        // 过滤父任务
        var parentIds = allTasks.Where(t => t.ParentTaskId.HasValue).Select(t => t.ParentTaskId.Value).ToHashSet();
        var tasks = allTasks.Where(t => !parentIds.Contains(t.Id)).ToList();

        double bac = tasks.Sum(t => (double)t.BudgetCost);
        double pv = 0, ev = 0;

        foreach (var task in tasks)
        {
            double taskBudget = (double)task.BudgetCost;
            if (taskBudget <= 0) continue;

            pv += taskBudget * CalcPlannedPct(task, statusDate);
            ev += taskBudget * (task.CompletionPercentage / 100.0);
        }

        var spi = pv > 0 ? ev / pv : 1.0;
        var sv = ev - pv;

        bool hasActualCostData = tasks.Any(t => t.ActualCost.HasValue && t.ActualCost.Value > 0);
        double ac;
        if (hasActualCostData)
            ac = tasks.Sum(t => (double)(t.ActualCost ?? t.BudgetCost));
        else
            ac = ev;

        var cpi = ac > 0 ? ev / ac : 1.0;
        var cv = ev - ac;
        var csi = spi * cpi;

        return (pv, ev, ac, spi, cpi, sv, cv, csi, bac);
    }

    private static TaskItem MakeTask(int id, DateTime start, DateTime end, int completion,
                                      decimal budget, decimal? actualCost = null, int? parentId = null)
    {
        return new TaskItem
        {
            Id = id,
            Code = $"T{id:D2}",
            Name = $"Task {id}",
            PlanStartDate = start,
            PlanEndDate = end,
            PlanDuration = (end - start).Days + 1,
            CompletionPercentage = completion,
            BudgetCost = budget,
            ActualCost = actualCost,
            ParentTaskId = parentId,
        };
    }

    // ==================== CalcPlannedPct 基础测试 ====================

    [Fact]
    public void 计划进度_未到开始日期_返回0()
    {
        var t = MakeTask(1, new DateTime(2024, 7, 1), new DateTime(2024, 7, 10), 0, 1000);
        var pct = CalcPlannedPct(t, new DateTime(2024, 6, 15));
        Assert.Equal(0, pct);
    }

    [Fact]
    public void 计划进度_已过结束日期_返回1()
    {
        var t = MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 1000);
        var pct = CalcPlannedPct(t, new DateTime(2024, 6, 15));
        Assert.Equal(1, pct);
    }

    [Fact]
    public void 计划进度_进行中_按比例()
    {
        var t = MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 0, 1000);
        var pct = CalcPlannedPct(t, new DateTime(2024, 6, 15));
        // 过了 14 天 / 总 30 天 = 0.4667
        Assert.Equal(14.0 / 29, pct, 4); // (15-1)=14天 / (30-1)=29天跨度
    }

    // ==================== EVM 基础场景 ====================

    [Fact]
    public void 单任务已完成_PV等于EV等于BAC()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000),
        };
        var (pv, ev, ac, spi, cpi, sv, cv, _, bac) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(50000, bac);
        Assert.Equal(50000, pv);  // 计划已完成
        Assert.Equal(50000, ev);  // 实际已完成
        Assert.Equal(1.0, spi);
        Assert.Equal(1.0, cpi);
        Assert.Equal(0, sv);
        Assert.Equal(0, cv);
    }

    [Fact]
    public void 单任务进展50_PV过半_EV半()
    {
        // 任务 6月1日→6月30日，状态日期6月15日（过半），完成率50%
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 50, 10000),
        };
        var (pv, ev, _, spi, _, sv, _, _, _) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 15));

        // PV: (15-1)/(30-1) = 14/29 ≈ 0.4828 → 4828
        var expectedPv = 10000 * (14.0 / 29);
        Assert.Equal(expectedPv, pv, 0);
        Assert.Equal(5000, ev);  // 10000 * 50%
        Assert.True(spi > 1.0);  // EV(5000) > PV(4828) → 进度超前
    }

    [Fact]
    public void 单任务进展慢_SPI小于1()
    {
        // 任务 6月1日→6月30日，6月15日检查，完成率20%
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 20, 10000),
        };
        var (pv, ev, _, spi, _, sv, _, _, _) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 15));

        Assert.True(spi < 1.0); // EV(2000) < PV(4828) → 进度滞后
        Assert.True(sv < 0);
    }

    // ==================== 多任务 EVM ====================

    [Fact]
    public void 多任务_线性叠加()
    {
        // A: 已完工(100%, 30000), B: 进行中(60%, 50000)
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 30000),
            MakeTask(2, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 60, 50000),
        };
        var (pv, ev, _, spi, _, _, _, _, bac) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 15));

        Assert.Equal(80000, bac);
        // A: PV=30000, EV=30000
        // B: PV=50000*14/29≈48276, EV=50000*60%=30000
        var expectedBpv = 50000 * (14.0 / 29);
        Assert.Equal(30000 + expectedBpv, pv, 0);
        Assert.Equal(30000 + 30000, ev);
    }

    // ==================== 实际成本测试 ====================

    [Fact]
    public void 有实际成本时_AC取实际值()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000, actualCost: 55000),
        };
        var (_, _, ac, _, cpi, _, cv, _, _) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(55000, ac);     // 实际花了 55000
        Assert.True(cpi < 1.0);      // CPI < 1 → 超支
        Assert.True(cv < 0);         // CV < 0 → 超支
    }

    [Fact]
    public void 无实际成本时_AC退化等于EV()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000),
        };
        var (_, ev, ac, _, cpi, _, _, _, _) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(ev, ac);  // 无实际成本数据 → AC=EV
        Assert.Equal(1.0, cpi);
    }

    // ==================== 边界条件 ====================

    [Fact]
    public void PV为零_SPI默认为1()
    {
        // 任务未开始（计划开始日期在将来）
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 8, 1), new DateTime(2024, 8, 10), 0, 10000),
        };
        var (pv, _, _, spi, _, _, _, _, _) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(0, pv);
        Assert.Equal(1.0, spi);  // PV=0 → SPI=1
    }

    [Fact]
    public void 零预算任务被跳过()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 0),    // 零预算
            MakeTask(2, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000),
        };
        var (pv, ev, _, _, _, _, _, _, bac) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(50000, bac);   // 只有 T2 的预算
        Assert.Equal(50000, pv);    // T1 被跳过
        Assert.Equal(50000, ev);
    }

    [Fact]
    public void 父任务被过滤_不参与EVM()
    {
        // P(父，预算10万) → C1(子，预算5万, 完成100%), C2(子，预算10万, 完成50%)
        var p = MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 7, 30), 0, 100000);
        var c1 = MakeTask(2, new DateTime(2024, 6, 1), new DateTime(2024, 6, 15), 100, 50000, parentId: 1);
        var c2 = MakeTask(3, new DateTime(2024, 6, 10), new DateTime(2024, 7, 30), 50, 100000, parentId: 1);

        var allTasks = new List<TaskItem> { p, c1, c2 };
        var (pv, ev, _, _, _, _, _, _, bac) = CalculateEarnedValue(allTasks, new DateTime(2024, 6, 15));

        // BAC = 只有叶子任务 = 5万 + 10万 = 15万（父任务被过滤）
        Assert.Equal(150000, bac);
        // C1 已完成: EV=50000
        // C2 进展50%: EV=50000
        Assert.Equal(100000, ev);
    }

    [Fact]
    public void CSI公式正确()
    {
        // SPI=0.9, CPI=1.1 → CSI = 0.99
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 50, 10000, actualCost: 5000),
        };
        var (_, _, _, _, _, _, _, csi, _) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 20));
        // 已完成50%，EV=5000。PV=10000。SPI=0.5。AC=5000。CPI=1.0。CSI=0.5
        Assert.Equal(0.5, csi);
    }

    // ==================== 进度偏差方向 ====================

    [Fact]
    public void 进度偏差_提前为正_滞后为负()
    {
        // 提前：完成率50% > 计划进度25%
        var ahead = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 7, 30), 50, 10000),
        };
        var (pv, ev, _, _, _, sv, _, _, _) = CalculateEarnedValue(ahead, new DateTime(2024, 6, 15));
        Assert.True(sv > 0, "完成率 > 计划进度 → SV为正");

        // 滞后：完成率10% < 计划进度25%
        var behind = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 7, 30), 10, 10000),
        };
        var (_, _, _, _, _, sv2, _, _, _) = CalculateEarnedValue(behind, new DateTime(2024, 6, 15));
        Assert.True(sv2 < 0, "完成率 < 计划进度 → SV为负");
    }
}
