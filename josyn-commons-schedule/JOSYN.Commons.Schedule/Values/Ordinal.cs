#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// The ordinal position of a weekday within a calendar period — the <c>nth</c> field
/// of an <c>nth_weekday</c> rule.
/// </summary>
public abstract record Ordinal
{
    private Ordinal() { }

    /// <summary>
    /// The Nth occurrence (1 through 5) of the weekday within the period.
    /// </summary>
    /// <param name="N">Occurrence index. Valid range: 1–5.</param>
    public sealed record Numeric(int N) : Ordinal;

    /// <summary>The last occurrence of the weekday within the period.</summary>
    public sealed record Last : Ordinal;

    /// <summary>
    /// The (last − <paramref name="Offset"/>)th occurrence of the weekday within the period.
    /// <c>Offset = 1</c> means second-to-last; <c>Offset = 2</c> means third-to-last.
    /// </summary>
    /// <param name="Offset">How many positions back from last. Must be ≥ 1.</param>
    public sealed record LastMinus(int Offset) : Ordinal;
}
