#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// A repetition interval for <c>interval</c> rules — an integer amount paired with a unit.
/// </summary>
/// <remarks>
/// Compound forms such as <c>1h30m</c> are not accepted by the language; use the smallest
/// unit instead (<c>90m</c>). This type therefore has a single amount/unit pair.
/// </remarks>
/// <param name="Amount">Number of units. Must be ≥ 1.</param>
/// <param name="Unit">Minutes or hours.</param>
public sealed record Duration(int Amount, DurationUnit Unit)
{
    /// <summary>Total duration expressed as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan ToTimeSpan() =>
        Unit == DurationUnit.Hours
            ? TimeSpan.FromHours(Amount)
            : TimeSpan.FromMinutes(Amount);

    /// <inheritdoc/>
    public override string ToString() =>
        Unit == DurationUnit.Hours ? $"{Amount}h" : $"{Amount}m";
}
