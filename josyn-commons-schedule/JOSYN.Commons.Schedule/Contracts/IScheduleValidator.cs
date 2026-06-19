#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Contract for semantic validation of a parsed schedule definition.
/// </summary>
public interface IScheduleValidator
{
    /// <summary>
    /// Validates the semantics of a successfully parsed <see cref="ScheduleDefinition"/>.
    /// </summary>
    /// <param name="definition">The definition to validate.</param>
    /// <returns>
    /// All issues found — errors and warnings. An empty list means the definition is clean.
    /// This method never fails structurally; it always returns a list.
    /// </returns>
    static abstract IReadOnlyList<ValidationIssue> Validate(ScheduleDefinition definition);
}
