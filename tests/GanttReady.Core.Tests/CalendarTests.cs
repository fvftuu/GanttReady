using GanttReady.Server.Services;
using Xunit;

namespace GanttReady.Core.Tests;

public class CalendarTests
{
    private readonly CalendarService _calendar = new(null!); // 鍙祴鍚屾鏂规硶锛屼笉娴婦B
    private readonly HashSet<DateTime> _noHolidays = new();

    // 宸ヤ綔鏃ヤ綅鎺╃爜锛氬懆涓€~鍛ㄤ簲
    private const int MonFri = 0b00111110; // 62, bits 1-5 = Mon-Fri
    private const int AllWeek = 0b01111111; // bit0-6 = Sun-Sat
    private const int OnlyMon = 0b00000010; // bit1 = Mon

    [Fact]
    public void IsWorkingDay_鍛ㄤ竴鑷冲懆浜擾杩斿洖true()
    {
        var bits = MonFri;
        var mon = new DateTime(2026, 6, 1); // Monday
        var fri = new DateTime(2026, 6, 5); // Friday
        Assert.True(_calendar.IsWorkingDay(bits, mon, _noHolidays));
        Assert.True(_calendar.IsWorkingDay(bits, fri, _noHolidays));
    }

    [Fact]
    public void IsWorkingDay_鍛ㄥ叚鏃杩斿洖false()
    {
        var bits = MonFri;
        var sat = new DateTime(2026, 6, 6); // Saturday
        var sun = new DateTime(2026, 6, 7); // Sunday
        Assert.False(_calendar.IsWorkingDay(bits, sat, _noHolidays));
        Assert.False(_calendar.IsWorkingDay(bits, sun, _noHolidays));
    }

    [Fact]
    public void IsWorkingDay_鑺傚亣鏃杩斿洖false()
    {
        var bits = MonFri;
        var holidays = new HashSet<DateTime> { new(2026, 6, 1) }; // Monday is holiday
        Assert.False(_calendar.IsWorkingDay(bits, new(2026, 6, 1), holidays));
    }

    [Fact]
    public void IsWorkingDay_姣忓ぉ涓婄彮_鍏ㄩ儴杩斿洖true()
    {
        var bits = AllWeek;
        Assert.True(_calendar.IsWorkingDay(bits, new(2026, 6, 6), _noHolidays)); // Sat
        Assert.True(_calendar.IsWorkingDay(bits, new(2026, 6, 7), _noHolidays)); // Sun
    }

    [Fact]
    public void AddWorkingDays_鏃犱紤鎭棩_绛変簬鑷劧澶?)
    {
        var start = new DateTime(2026, 6, 1); // Monday
        var end = _calendar.AddWorkingDays(AllWeek, start, 10, _noHolidays);
        Assert.Equal(start.AddDays(10), end); // 涓嶅惈璧峰鏃?
    }

    [Fact]
    public void AddWorkingDays_鍛ㄤ竴鑷冲懆浜擾璺宠繃鍛ㄦ湯()
    {
        var start = new DateTime(2026, 6, 1); // Monday
        // 10 working days = 2 weeks = Mon 6/1 鈫?Fri 6/12 (skip 2 weekends)
        var end = _calendar.AddWorkingDays(MonFri, start, 10, _noHolidays);
        Assert.Equal(new DateTime(2026, 6, 15), end);
    }

    [Fact]
    public void AddWorkingDays_璧峰浜庡懆浜擾璺宠繃鍛ㄦ湯()
    {
        var start = new DateTime(2026, 6, 5); // Friday
        // 3 working days: Fri(1), Mon(2), Tue(3) 鈫?Tue June 9
        var end = _calendar.AddWorkingDays(MonFri, start, 3, _noHolidays);
        Assert.Equal(new DateTime(2026, 6, 10), end);
    }

    [Fact]
    public void AddWorkingDays_涓棿鏈夎妭鍋囨棩()
    {
        var start = new DateTime(2026, 5, 29); // Friday
        var holidays = new HashSet<DateTime> { new(2026, 6, 1) }; // Monday is holiday
        // 3 working days: Fri(1), Tue(2, Mon skipped as holiday), Wed(3) 鈫?Wed June 3
        var end = _calendar.AddWorkingDays(MonFri, start, 3, holidays);
        Assert.Equal(new DateTime(2026, 6, 4), end);
    }

    [Fact]
    public void AddWorkingDays_宸ユ湡涓?_杩斿洖璧峰鏃?)
    {
        var start = new DateTime(2026, 6, 1);
        var end = _calendar.AddWorkingDays(MonFri, start, 0, _noHolidays);
        Assert.Equal(start, end);
    }

    [Fact]
    public void AddWorkingDays_浠呬竴澶╁伐浣滄棩()
    {
        var start = new DateTime(2026, 6, 1); // Monday
        // Only Monday is working, 3 working days = 3 consecutive Mondays
        // Mon 6/1(1) 鈫?Mon 6/8(2) 鈫?Mon 6/15(3) 
        var end = _calendar.AddWorkingDays(OnlyMon, start, 3, _noHolidays);
        Assert.Equal(new DateTime(2026, 6, 22), end);
    }

    [Fact]
    public void AddWorkingDays_鍏ㄩ潪宸ヤ綔鏃鎸夎嚜鐒跺ぉ()
    {
        var start = new DateTime(2026, 6, 1);
        var end = _calendar.AddWorkingDays(0, start, 10, _noHolidays); // 0 = no working days
        Assert.Equal(start.AddDays(10), end);
    }
}
