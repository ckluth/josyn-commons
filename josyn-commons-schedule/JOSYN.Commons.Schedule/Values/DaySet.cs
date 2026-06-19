#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// An immutable set of days-of-week, used by <c>days</c> fields.
/// </summary>
/// <remarks>
/// Named factories cover the three keyword aliases defined by ADR-026:
/// <c>weekdays</c>, <c>weekend</c>, <c>daily</c>.
/// Arbitrary sets are constructed via the primary constructor.
/// </remarks>
public sealed record DaySet(IReadOnlySet<DayOfWeek> Days)
{
    /// <summary>Monday through Friday.</summary>
    public static DaySet Weekdays { get; } = new(new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });

    /// <summary>Saturday and Sunday.</summary>
    public static DaySet Weekend { get; } = new(new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday });

    /// <summary>All seven days.</summary>
    public static DaySet Daily { get; } = new(new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday });

    /// <summary>Returns true if <paramref name="day"/> is a member of this set.</summary>
    public bool Contains(DayOfWeek day) => Days.Contains(day);

    // Record equality on IReadOnlySet uses reference equality by default, which is wrong.
    // Declaring Equals(DaySet?) without any modifier causes the compiler to use this
    // implementation instead of generating its own.
    /// <inheritdoc/>
    public bool Equals(DaySet? other) => other is not null && Days.SetEquals(other.Days);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Order-independent hash: XOR the individual day hash codes.
        return Days.Aggregate(0, (current, d) => current ^ d.GetHashCode());
    }
}
