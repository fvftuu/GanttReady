using NetPlan.Server.Services;
using Xunit;

namespace NetPlan.Core.Tests;

public class CalendarTests
{
    private readonly CalendarService _calendar = new(null!); // 只测同步方法，不测DB
    private readonly HashSet<DateTime> _noHolidays = new();

    // 工作日位掩码：周一~周五
    private const int MonFri = 0b00111110; // 62, bits 1-5 = Mon-Fri
    private const int AllWeek = 0b01111111; // bit0-6 = Sun-Sat
    private const int OnlyMon = 0b00000010; // bit1 = Mon

    [Fact]
    public void IsWorkingDay_周一至周五_返回true()
    {
        var bits = MonFri;
        var mon = new DateTime(2026, 6, 1); // Monday
        var fri = new DateTime(2026, 6, 5); // Friday
        Assert.True(_calendar.IsWorkingDay(bits, mon, _noHolidays));
        Assert.True(_calendar.IsWorkingDay(bits, fri, _noHolidays));
    }

    [Fact]
    public void IsWorkingDay_周六日_返回false()
    {
        var bits = MonFri;
        var sat = new DateTime(2026, 6, 6); // Saturday
        var sun = new DateTime(2026, 6, 7); // Sunday
        Assert.False(_calendar.IsWorkingDay(bits, sat, _noHolidays));
        Assert.False(_calendar.IsWorkingDay(bits, sun, _noHolidays));
    }

    [Fact]
    public void IsWorkingDay_节假日_返回false()
    {
        var bits = MonFri;
        var holidays = new HashSet<DateTime> { new(2026, 6, 1) }; // Monday is holiday
        Assert.False(_calendar.IsWorkingDay(bits, new(2026, 6, 1), holidays));
    }

    [Fact]
    public void IsWorkingDay_每天上班_全部返回true()
    {
        var bits = AllWeek;
        Assert.True(_calendar.IsWorkingDay(bits, new(2026, 6, 6), _noHolidays)); // Sat
        Assert.True(_calendar.IsWorkingDay(bits, new(2026, 6, 7), _noHolidays)); // Sun
    }

    [Fact]
    public void AddWorkingDays_无休息日_等于自然天()
    {
        var start = new DateTime(2026, 6, 1); // Monday
        var end = _calendar.AddWorkingDays(AllWeek, start, 10, _noHolidays);
        Assert.Equal(start.AddDays(10), end); // 不含起始日
    }

    [Fact]
    public void AddWorkingDays_周一至周五_跳过周末()
    {
        var start = new DateTime(2026, 6, 1); // Monday
        // 10 working days = 2 weeks = Mon 6/1 → Fri 6/12 (skip 2 weekends)
        var end = _calendar.AddWorkingDays(MonFri, start, 10, _noHolidays);
        Assert.Equal(new DateTime(2026, 6, 15), end);
    }

    [Fact]
    public void AddWorkingDays_起始于周五_跳过周末()
    {
        var start = new DateTime(2026, 6, 5); // Friday
        // 3 working days: Fri(1), Mon(2), Tue(3) → Tue June 9
        var end = _calendar.AddWorkingDays(MonFri, start, 3, _noHolidays);
        Assert.Equal(new DateTime(2026, 6, 10), end);
    }

    [Fact]
    public void AddWorkingDays_中间有节假日()
    {
        var start = new DateTime(2026, 5, 29); // Friday
        var holidays = new HashSet<DateTime> { new(2026, 6, 1) }; // Monday is holiday
        // 3 working days: Fri(1), Tue(2, Mon skipped as holiday), Wed(3) → Wed June 3
        var end = _calendar.AddWorkingDays(MonFri, start, 3, holidays);
        Assert.Equal(new DateTime(2026, 6, 4), end);
    }

    [Fact]
    public void AddWorkingDays_工期为0_返回起始日()
    {
        var start = new DateTime(2026, 6, 1);
        var end = _calendar.AddWorkingDays(MonFri, start, 0, _noHolidays);
        Assert.Equal(start, end);
    }

    [Fact]
    public void AddWorkingDays_仅一天工作日()
    {
        var start = new DateTime(2026, 6, 1); // Monday
        // Only Monday is working, 3 working days = 3 consecutive Mondays
        // Mon 6/1(1) → Mon 6/8(2) → Mon 6/15(3) 
        var end = _calendar.AddWorkingDays(OnlyMon, start, 3, _noHolidays);
        Assert.Equal(new DateTime(2026, 6, 22), end);
    }

    [Fact]
    public void AddWorkingDays_全非工作日_按自然天()
    {
        var start = new DateTime(2026, 6, 1);
        var end = _calendar.AddWorkingDays(0, start, 10, _noHolidays); // 0 = no working days
        Assert.Equal(start.AddDays(10), end);
    }
}
