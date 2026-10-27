namespace Sufficit.Telephony.Panel.Operations;

/// <summary>
/// Work shift of a Queue controller queue (configured in Blazor, stored in the queue options, sufficit-ari 0.20.15+).
/// Local HH:mm, end before start crosses midnight (the shift belongs to the day it started), weekdays 0 = Sunday.
/// </summary>
public sealed record QueueConfigShift(string Name, string Start, string End, int[] Weekdays, int OffsetMinutes = -180)
{
    private static int? Minutes(string? value) => value is { Length: 5 } && TimeOnly.TryParseExact(value, "HH:mm", out var t) ? t.Hour * 60 + t.Minute : null;
    public bool IsValid() => Name is { Length: > 0 and <= 60 } && !Name.Any(char.IsControl) && Minutes(Start) is { } s && Minutes(End) is { } e && s != e &&
        OffsetMinutes is >= -720 and <= 840 && Weekdays is { Length: > 0 and <= 7 } && Weekdays.All(d => d is >= 0 and <= 6);

    /// <summary>Whether an instant falls inside the shift and which local day the shift started on.</summary>
    public bool Contains(DateTimeOffset at, out DateOnly day)
    {
        var local = at.ToOffset(TimeSpan.FromMinutes(OffsetMinutes));
        var minute = local.Hour * 60 + local.Minute; int s = Minutes(Start) ?? 0, e = Minutes(End) ?? 0;
        var date = DateOnly.FromDateTime(local.DateTime);
        bool inside;
        if (s < e) inside = minute >= s && minute < e;
        else if (minute >= s) inside = true;
        else { inside = minute < e; date = date.AddDays(-1); }
        day = date;
        return inside && Weekdays.Contains((int)date.DayOfWeek);
    }

    /// <summary>Local day whose shift is current or most recent at <paramref name="now"/> (today's when it already started).</summary>
    public DateOnly Today(DateTimeOffset now) => Contains(now, out var day) ? day : DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromMinutes(OffsetMinutes)).DateTime);

    /// <summary>Query for sufficit-ari /report/shift over the last seven days.</summary>
    public string ReportQuery() => $"report/shift?start={Start}&end={End}&days={string.Join(',', Weekdays)}&offset={OffsetMinutes}";
}

/// <summary>Per-shift view for one queue: today's totals, the last seven days and who answered most.</summary>
public sealed record QueueShiftView(QueueConfigShift Shift, bool Current, QueueShiftDay? Today, QueueShiftDay[] Days, QueueShiftExtension[] TopExtensions,
    DateOnly? ShownDate = null, bool IsToday = true)
{
    public long Entered => Today?.Entered ?? 0;
    public double? AnswerRate => Today is { Entered: > 0 } t ? (double)t.Answered / t.Entered : null;
    public double? SlaRate => Today is { Answered: > 0 } t ? (double)t.AnsweredWithinSla / t.Answered : null;
    /// <summary>Average calls received per day the shift ran in the period (days without calls count as zero only when they ran).</summary>
    public double DailyAverage => Days.Length == 0 ? 0 : Days.Average(d => d.Entered);

    public static QueueShiftView Build(QueueConfigShift shift, QueueShiftReport? report, Guid queue, DateTimeOffset now)
    {
        var days = report?.Days.Where(d => d.QueueId == queue).OrderBy(d => d.Date).ToArray() ?? [];
        var today = shift.Today(now);
        // A shift that does not run today shows its last run (with the date), never a misleading zero.
        var runsToday = shift.Weekdays.Contains((int)today.DayOfWeek);
        var shown = runsToday ? days.FirstOrDefault(d => d.Date == today) : days.LastOrDefault(d => d.Date < today);
        return new(shift, shift.Contains(now, out _), shown, days,
            report?.Extensions.Where(e => e.QueueId == queue).OrderByDescending(e => e.Answered).Take(3).ToArray() ?? [],
            runsToday ? today : shown?.Date, runsToday);
    }
}

/// <summary>Calls of the selected period that entered when no shift was running (uncovered hours on the wall screen).</summary>
public sealed record OutOfShiftCalls(long Calls, long Answered, long Abandoned)
{
    public static OutOfShiftCalls Count(IEnumerable<DistributionCall> calls, IReadOnlyList<QueueConfigShift> shifts)
    {
        if (shifts.Count == 0) return new(0, 0, 0);
        var outside = calls.Where(c => !shifts.Any(s => s.Contains(c.EnteredAt, out _))).ToArray();
        return new(outside.Length, outside.LongCount(c => c.Outcome == "answered"), outside.LongCount(c => c.Outcome == "abandoned"));
    }
}
