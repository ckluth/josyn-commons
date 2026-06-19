#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Unit of a repetition interval used by <c>interval</c> rules.
/// </summary>
public enum DurationUnit
{
    /// <summary>Minutes — suffix <c>m</c> in the schedule file.</summary>
    Minutes,

    /// <summary>Hours — suffix <c>h</c> in the schedule file.</summary>
    Hours,
}
