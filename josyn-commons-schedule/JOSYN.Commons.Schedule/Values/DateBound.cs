#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// An activation boundary for the <c>active_from</c> / <c>active_until</c> modifiers.
/// Two forms exist: a one-time full ISO date, or a recurring annual month-day pair.
/// </summary>
public abstract record DateBound
{
    private DateBound() { }

    /// <summary>
    /// A one-time boundary expressed as a full ISO date (<c>YYYY-MM-DD</c>).
    /// The rule is active only relative to this specific calendar date.
    /// </summary>
    /// <param name="Date">The boundary date.</param>
    public sealed record FullDate(DateOnly Date) : DateBound;

    /// <summary>
    /// A recurring annual boundary expressed as a month-day pair (<c>MM-DD</c>).
    /// The same boundary applies every calendar year.
    /// </summary>
    /// <param name="Month">Month number (1–12).</param>
    /// <param name="Day">Day number (1–31).</param>
    public sealed record Annual(int Month, int Day) : DateBound;
}
