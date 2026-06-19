using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Parses a schedule definition file from its INI text representation.
/// </summary>
/// <inheritdoc cref="IScheduleParser"/>
public static partial class ScheduleParser
{
    /// <inheritdoc cref="IScheduleParser.Parse"/>
    public static Result<ScheduleDefinition> Parse(string text)
    {
        var blocks = Preprocess(text);
        var (rules, errors) = ParseAllBlocks(blocks);
        return errors.Count > 0 ? Result.Error(FormatErrors(errors)) : new ScheduleDefinition(rules);

        //
        // nested helpers
        //

        static (List<ScheduleRule> Rules, List<string> Errors) ParseAllBlocks(IReadOnlyList<IniBlock> blocks)
        {
            var rules = new List<ScheduleRule>();
            var errors = new List<string>();

            foreach (var block in blocks)
            {
                var result = ParseRule(block);
                if (result.Succeeded)
                    rules.Add(result.Value);
                else
                    // Capture the error but continue so all blocks are validated in one pass.
                    errors.Add(result.ErrorMessage ?? $"Block {block.BlockIndex + 1}: unknown error.");
            }
            return (rules, errors);
        }

        static string FormatErrors(List<string> errors)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Schedule parsing failed with {errors.Count} error(s):");
            for (var i = 0; i < errors.Count; i++)
                sb.AppendLine($"  [{i + 1}] {errors[i]}");
            return sb.ToString().TrimEnd();
        }
    }
}
