using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Contract for the schedule definition language parser.
/// </summary>
public interface IScheduleParser
{
    /// <summary>
    /// Parses a schedule file's text content into a <see cref="ScheduleDefinition"/>.
    /// </summary>
    /// <param name="text">The full text of the schedule file.</param>
    /// <returns>
    /// The parsed definition on success, or a failure describing all parse errors found
    /// across all blocks (not just the first one).
    /// </returns>
    static abstract Result<ScheduleDefinition> Parse(string text);
}
