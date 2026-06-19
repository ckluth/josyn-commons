#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

public static partial class ScheduleParser
{
    //
    // Entry point for the preprocessing pipeline
    //
    private static IReadOnlyList<IniBlock> Preprocess(string text)
    {
        var lines  = text.Split('\n');
        var stripped = lines.Select(StripComment);
        var joined   = ApplyContinuation(stripped);
        return SplitIntoBlocks(joined);
    }

    //
    // Pipeline stages
    //

    // Remove everything from the first # or ; to end-of-line.
    // ADR-026 has no quoting, so a simple scan is correct.
    private static string StripComment(string line)
    {
        for (var i = 0; i < line.Length; i++)
            if (line[i] == '#' || line[i] == ';')
                return line[..i];
        return line;
    }

    // A line that starts with whitespace (after comment stripping) is an INI
    // continuation — its trimmed content is appended to the preceding value line.
    private static IEnumerable<string> ApplyContinuation(IEnumerable<string> lines)
    {
        string? pending = null;

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                // Blank line — flush pending line and emit blank as block separator.
                if (pending is not null) { yield return pending; pending = null; }
                yield return string.Empty;
                continue;
            }

            var isContinuation = (raw[0] == ' ' || raw[0] == '\t') && pending is not null;
            if (isContinuation)
                pending = pending + " " + raw.Trim();
            else
            {
                if (pending is not null) yield return pending;
                pending = raw.TrimEnd();
            }
        }

        if (pending is not null) yield return pending;
    }

#pragma warning disable CA1859
    private static IReadOnlyList<IniBlock> SplitIntoBlocks(IEnumerable<string> lines)
#pragma warning restore CA1859
    {
        var blocks  = new List<IniBlock>();
        var current = new List<string>();
        var index   = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current.Count <= 0) continue;
                blocks.Add(BuildBlock(index++, current));
                current = [];
            }
            else
            {
                current.Add(line.Trim());
            }
        }

        if (current.Count > 0)
            blocks.Add(BuildBlock(index, current));

        return blocks;
    }

    // Parse a list of "key = value" lines into an IniBlock.
    // Lines that do not contain '=' are silently skipped (e.g. orphaned comment remnants).
    private static IniBlock BuildBlock(int blockIndex, List<string> lines)
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            entries[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }
        return new IniBlock(blockIndex, entries);
    }
}
