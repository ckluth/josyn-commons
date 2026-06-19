using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// A single rule block extracted from the raw INI text — a key/value map with
/// the block's position index for diagnostic messages.
/// </summary>
internal sealed class IniBlock
{
    private readonly Dictionary<string, string> entries;

    internal int BlockIndex { get; }

    internal IniBlock(int blockIndex, Dictionary<string, string> iniEntries)
    {
        BlockIndex = blockIndex;
        entries   = iniEntries;
    }

    /// <summary>
    /// Returns the value for a required key, or an error that names the block and key.
    /// </summary>
    internal Result<string> Require(string key) => entries.TryGetValue(key, out var value) ? Result<string>.Success(value) : Result.Error($"Block {BlockIndex + 1}: required key '{key}' is missing.");

    /// <summary>Returns the raw value for an optional key, or <see langword="null"/> if absent.</summary>
    internal string? Optional(string key) => entries.GetValueOrDefault(key);
}
