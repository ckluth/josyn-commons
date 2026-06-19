namespace JOSYN.Commons.Schedule;

/// <summary>
/// A parsed schedule file — an ordered list of rule blocks.
/// </summary>
/// <remarks>
/// Multiple rules are OR'd: the job is triggered if any rule is satisfied at the current
/// evaluation moment. Exclusions are applied after all rules are evaluated and always win.
/// Evaluation order does not matter — two rules firing at the same moment do not produce
/// two launches.
/// </remarks>
/// <param name="Rules">All rule blocks in document order.</param>
public sealed record ScheduleDefinition(IReadOnlyList<ScheduleRule> Rules);
