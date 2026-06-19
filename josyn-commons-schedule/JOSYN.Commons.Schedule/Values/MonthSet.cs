#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// An immutable set of calendar months (1–12), used by the <c>months</c> field of
/// <c>monthly_date</c> rules.
/// </summary>
/// <remarks>
/// When the <c>months</c> key is omitted from a rule the field is modelled as
/// <see langword="null"/> at the rule level, not as <see cref="All"/> — the two
/// are semantically equivalent but <see langword="null"/> preserves the author's intent
/// and round-trips cleanly through the serializer (no redundant key is emitted).
/// </remarks>
public sealed record MonthSet(IReadOnlySet<int> Months)
{
    /// <summary>All twelve months.</summary>
    public static MonthSet All { get; } = new(
        new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

    /// <summary>Returns true if <paramref name="month"/> (1–12) is a member of this set.</summary>
    public bool Contains(int month) => Months.Contains(month);

    // Override equality so two instances with the same month numbers compare as equal.
    // Declaring without any modifier causes the compiler to use this instead of generating its own.
    /// <inheritdoc/>
    public bool Equals(MonthSet? other) =>
        other is not null && Months.SetEquals(other.Months);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var m in Months)
            hash ^= m.GetHashCode();
        return hash;
    }
}
