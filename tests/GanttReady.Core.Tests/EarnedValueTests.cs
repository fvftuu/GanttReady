using GanttReady.Server.Models;
using Xunit;

namespace GanttReady.Core.Tests;

public class EarnedValueTests
{
    private readonly DateTime _today = new(2024, 6, 15);

    /// <summary>
    /// 澶嶅埗 AnalysisService.CalcPlannedPct 鐨勯€昏緫鐢ㄤ簬娴嬭瘯楠岃瘉
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
    /// 妯℃嫙 GetEarnedValueAsync 鐨勬牳蹇冭绠楅€昏緫锛堜笉渚濊禆鏁版嵁搴擄級
    /// </summary>
    private static (double pv, double ev, double ac, double spi, double cpi, double sv, double cv, double csi, double bac)
        CalculateEarnedValue(List<TaskItem> allTasks, DateTime statusDate)
    {
        // 杩囨护鐖朵换鍔?        var parentIds = allTasks.Where(t => t.ParentTaskId.HasValue).Select(t => t.ParentTaskId.Value).ToHashSet();
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

    // ==================== CalcPlannedPct 鍩虹娴嬭瘯 ====================

    [Fact]
    public void 璁″垝杩涘害_鏈埌寮€濮嬫棩鏈焈杩斿洖0()
    {
        var t = MakeTask(1, new DateTime(2024, 7, 1), new DateTime(2024, 7, 10), 0, 1000);
        var pct = CalcPlannedPct(t, new DateTime(2024, 6, 15));
        Assert.Equal(0, pct);
    }

    [Fact]
    public void 璁″垝杩涘害_宸茶繃缁撴潫鏃ユ湡_杩斿洖1()
    {
        var t = MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 1000);
        var pct = CalcPlannedPct(t, new DateTime(2024, 6, 15));
        Assert.Equal(1, pct);
    }

    [Fact]
    public void 璁″垝杩涘害_杩涜涓璤鎸夋瘮渚?)
    {
        var t = MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 0, 1000);
        var pct = CalcPlannedPct(t, new DateTime(2024, 6, 15));
        // 杩囦簡 14 澶?/ 鎬?30 澶?= 0.4667
        Assert.Equal(14.0 / 29, pct, 4); // (15-1)=14澶?/ (30-1)=29澶╄法搴?    }

    // ==================== EVM 鍩虹鍦烘櫙 ====================

    [Fact]
    public void 鍗曚换鍔″凡瀹屾垚_PV绛変簬EV绛変簬BAC()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000),
        };
        var (pv, ev, ac, spi, cpi, sv, cv, _, bac) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(50000, bac);
        Assert.Equal(50000, pv);  // 璁″垝宸插畬鎴?        Assert.Equal(50000, ev);  // 瀹為檯宸插畬鎴?        Assert.Equal(1.0, spi);
        Assert.Equal(1.0, cpi);
        Assert.Equal(0, sv);
        Assert.Equal(0, cv);
    }

    [Fact]
    public void 鍗曚换鍔¤繘灞?0_PV杩囧崐_EV鍗?)
    {
        // 浠诲姟 6鏈?鏃モ啋6鏈?0鏃ワ紝鐘舵€佹棩鏈?鏈?5鏃ワ紙杩囧崐锛夛紝瀹屾垚鐜?0%
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 50, 10000),
        };
        var (pv, ev, _, spi, _, sv, _, _, _) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 15));

        // PV: (15-1)/(30-1) = 14/29 鈮?0.4828 鈫?4828
        var expectedPv = 10000 * (14.0 / 29);
        Assert.Equal(expectedPv, pv, 0);
        Assert.Equal(5000, ev);  // 10000 * 50%
        Assert.True(spi > 1.0);  // EV(5000) > PV(4828) 鈫?杩涘害瓒呭墠
    }

    [Fact]
    public void 鍗曚换鍔¤繘灞曟參_SPI灏忎簬1()
    {
        // 浠诲姟 6鏈?鏃モ啋6鏈?0鏃ワ紝6鏈?5鏃ユ鏌ワ紝瀹屾垚鐜?0%
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 20, 10000),
        };
        var (pv, ev, _, spi, _, sv, _, _, _) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 15));

        Assert.True(spi < 1.0); // EV(2000) < PV(4828) 鈫?杩涘害婊炲悗
        Assert.True(sv < 0);
    }

    // ==================== 澶氫换鍔?EVM ====================

    [Fact]
    public void 澶氫换鍔绾挎€у彔鍔?)
    {
        // A: 宸插畬宸?100%, 30000), B: 杩涜涓?60%, 50000)
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 30000),
            MakeTask(2, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), 60, 50000),
        };
        var (pv, ev, _, spi, _, _, _, _, bac) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 15));

        Assert.Equal(80000, bac);
        // A: PV=30000, EV=30000
        // B: PV=50000*14/29鈮?8276, EV=50000*60%=30000
        var expectedBpv = 50000 * (14.0 / 29);
        Assert.Equal(30000 + expectedBpv, pv, 0);
        Assert.Equal(30000 + 30000, ev);
    }

    // ==================== 瀹為檯鎴愭湰娴嬭瘯 ====================

    [Fact]
    public void 鏈夊疄闄呮垚鏈椂_AC鍙栧疄闄呭€?)
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000, actualCost: 55000),
        };
        var (_, _, ac, _, cpi, _, cv, _, _) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(55000, ac);     // 瀹為檯鑺变簡 55000
        Assert.True(cpi < 1.0);      // CPI < 1 鈫?瓒呮敮
        Assert.True(cv < 0);         // CV < 0 鈫?瓒呮敮
    }

    [Fact]
    public void 鏃犲疄闄呮垚鏈椂_AC閫€鍖栫瓑浜嶦V()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000),
        };
        var (_, ev, ac, _, cpi, _, _, _, _) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(ev, ac);  // 鏃犲疄闄呮垚鏈暟鎹?鈫?AC=EV
        Assert.Equal(1.0, cpi);
    }

    // ==================== 杈圭晫鏉′欢 ====================

    [Fact]
    public void PV涓洪浂_SPI榛樿涓?()
    {
        // 浠诲姟鏈紑濮嬶紙璁″垝寮€濮嬫棩鏈熷湪灏嗘潵锛?        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 8, 1), new DateTime(2024, 8, 10), 0, 10000),
        };
        var (pv, _, _, spi, _, _, _, _, _) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(0, pv);
        Assert.Equal(1.0, spi);  // PV=0 鈫?SPI=1
    }

    [Fact]
    public void 闆堕绠椾换鍔¤璺宠繃()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 0),    // 闆堕绠?            MakeTask(2, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 100, 50000),
        };
        var (pv, ev, _, _, _, _, _, _, bac) = CalculateEarnedValue(tasks, _today);

        Assert.Equal(50000, bac);   // 鍙湁 T2 鐨勯绠?        Assert.Equal(50000, pv);    // T1 琚烦杩?        Assert.Equal(50000, ev);
    }

    [Fact]
    public void 鐖朵换鍔¤杩囨护_涓嶅弬涓嶦VM()
    {
        // P(鐖讹紝棰勭畻10涓? 鈫?C1(瀛愶紝棰勭畻5涓? 瀹屾垚100%), C2(瀛愶紝棰勭畻10涓? 瀹屾垚50%)
        var p = MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 7, 30), 0, 100000);
        var c1 = MakeTask(2, new DateTime(2024, 6, 1), new DateTime(2024, 6, 15), 100, 50000, parentId: 1);
        var c2 = MakeTask(3, new DateTime(2024, 6, 10), new DateTime(2024, 7, 30), 50, 100000, parentId: 1);

        var allTasks = new List<TaskItem> { p, c1, c2 };
        var (pv, ev, _, _, _, _, _, _, bac) = CalculateEarnedValue(allTasks, new DateTime(2024, 6, 15));

        // BAC = 鍙湁鍙跺瓙浠诲姟 = 5涓?+ 10涓?= 15涓囷紙鐖朵换鍔¤杩囨护锛?        Assert.Equal(150000, bac);
        // C1 宸插畬鎴? EV=50000
        // C2 杩涘睍50%: EV=50000
        Assert.Equal(100000, ev);
    }

    [Fact]
    public void CSI鍏紡姝ｇ‘()
    {
        // SPI=0.9, CPI=1.1 鈫?CSI = 0.99
        var tasks = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 6, 10), 50, 10000, actualCost: 5000),
        };
        var (_, _, _, _, _, _, _, csi, _) = CalculateEarnedValue(tasks, new DateTime(2024, 6, 20));
        // 宸插畬鎴?0%锛孍V=5000銆侾V=10000銆係PI=0.5銆侫C=5000銆侰PI=1.0銆侰SI=0.5
        Assert.Equal(0.5, csi);
    }

    // ==================== 杩涘害鍋忓樊鏂瑰悜 ====================

    [Fact]
    public void 杩涘害鍋忓樊_鎻愬墠涓烘_婊炲悗涓鸿礋()
    {
        // 鎻愬墠锛氬畬鎴愮巼50% > 璁″垝杩涘害25%
        var ahead = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 7, 30), 50, 10000),
        };
        var (pv, ev, _, _, _, sv, _, _, _) = CalculateEarnedValue(ahead, new DateTime(2024, 6, 15));
        Assert.True(sv > 0, "瀹屾垚鐜?> 璁″垝杩涘害 鈫?SV涓烘");

        // 婊炲悗锛氬畬鎴愮巼10% < 璁″垝杩涘害25%
        var behind = new List<TaskItem>
        {
            MakeTask(1, new DateTime(2024, 6, 1), new DateTime(2024, 7, 30), 10, 10000),
        };
        var (_, _, _, _, _, sv2, _, _, _) = CalculateEarnedValue(behind, new DateTime(2024, 6, 15));
        Assert.True(sv2 < 0, "瀹屾垚鐜?< 璁″垝杩涘害 鈫?SV涓鸿礋");
    }
}
