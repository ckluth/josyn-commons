using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Contract for the schedule definition language serializer.
/// </summary>
public interface IScheduleSerializer
{
    /// <summary>
    /// Serializes a <see cref="ScheduleDefinition"/> back to its INI text representation.
    /// </summary>
    /// <param name="definition">The schedule to serialize.</param>
    /// <returns>
    /// The canonical INI text on success. Blocks are separated by blank lines.
    /// Keys within a block are padded so the <c>=</c> signs align.
    /// </returns>
    static abstract Result<string> Serialize(ScheduleDefinition definition);
}
