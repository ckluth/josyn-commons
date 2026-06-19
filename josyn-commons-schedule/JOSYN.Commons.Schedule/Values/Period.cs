#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// The calendar period used by <c>nth_weekday</c> rules to scope the ordinal lookup.
/// </summary>
public enum Period
{
    /// <summary>Calendar month.</summary>
    Month,

    /// <summary>
    /// Calendar quarter — Q1 = January, Q2 = April, Q3 = July, Q4 = October.
    /// "Nth weekday of a quarter" means the Nth occurrence in the first month of that quarter.
    /// </summary>
    Quarter,

    /// <summary>Calendar year.</summary>
    Year,
}
