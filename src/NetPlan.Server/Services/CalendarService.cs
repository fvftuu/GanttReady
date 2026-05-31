using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

/// <summary>工作日历计算服务</summary>
public class CalendarService
{
    private readonly NetPlanDbContext _db;

    public CalendarService(NetPlanDbContext db)
    {
        _db = db;
    }

    /// <summary>获取项目的工作日位掩码</summary>
    public async Task<int> GetWorkDayBitsAsync(int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        return project?.WorkDayBits ?? 0b00111110;
    }

    /// <summary>获取项目的节假日日期集合</summary>
    public async Task<HashSet<DateTime>> GetHolidaysAsync(int projectId)
    {
        var holidays = await _db.Holidays
            .Where(h => h.ProjectId == projectId)
            .Select(h => h.Date.Date)
            .ToListAsync();
        return new HashSet<DateTime>(holidays);
    }

    /// <summary>判断某天是否是工作日</summary>
    public bool IsWorkingDay(int workDayBits, DateTime date, HashSet<DateTime> holidays)
    {
        if (holidays.Contains(date.Date)) return false;
        var dow = (int)date.DayOfWeek; // 0=Sun..6=Sat
        return (workDayBits & (1 << dow)) != 0;
    }

    /// <summary>获取项目某天的 IsWorkingDay 快捷方法</summary>
    public async Task<bool> IsWorkingDayAsync(int projectId, DateTime date)
    {
        var bits = await GetWorkDayBitsAsync(projectId);
        var holidays = await GetHolidaysAsync(projectId);
        return IsWorkingDay(bits, date, holidays);
    }

    /// <summary>从 startDate 开始，加上 duration 个工作日，返回实际结束日期</summary>
    public async Task<DateTime> AddWorkingDaysAsync(int projectId, DateTime startDate, int duration)
    {
        var bits = await GetWorkDayBitsAsync(projectId);
        var holidays = await GetHolidaysAsync(projectId);
        return AddWorkingDays(bits, startDate, duration, holidays);
    }

    /// <summary>从 startDate 的次日开始，加上 duration 个工作日，返回结束日期（不含起始日）</summary>
    public DateTime AddWorkingDays(int workDayBits, DateTime startDate, int duration, HashSet<DateTime> holidays)
    {
        if (duration <= 0) return startDate;
        if ((workDayBits & 0x7F) == 0) return startDate.AddDays(duration);
        var current = startDate;
        var added = 0;
        var maxLoops = duration * 366 + 366;
        while (added < duration && maxLoops-- > 0)
        {
            current = current.AddDays(1);
            if (IsWorkingDay(workDayBits, current, holidays))
                added++;
        }
        return current;
    }

    /// <summary>计算两个日期之间的工作日天数（含起止日）</summary>
    public async Task<int> GetWorkingDaysCountAsync(int projectId, DateTime start, DateTime end)
    {
        var bits = await GetWorkDayBitsAsync(projectId);
        var holidays = await GetHolidaysAsync(projectId);
        var count = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (IsWorkingDay(bits, d, holidays))
                count++;
        }
        return Math.Max(1, count);
    }
}
