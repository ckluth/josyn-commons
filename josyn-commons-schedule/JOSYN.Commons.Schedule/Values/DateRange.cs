#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// An inclusive date range used by <c>exclude</c> rule blocks.
/// When <see cref="Start"/> equals <see cref="End"/> the range covers a single date.
/// </summary>
/// <param name="Start">First date in the range (inclusive).</param>
/// <param name="End">Last date in the range (inclusive). Must be ≥ <paramref name="Start"/>.</param>
public sealed record DateRange(DateOnly Start, DateOnly End)
{
    /// <summary>Returns true if <paramref name="date"/> falls within this range.</summary>
    public bool Contains(DateOnly date) => date >= Start && date <= End;
}
