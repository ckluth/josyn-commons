using System.Text.Json;

using JOSYN.Foundation.ResultPattern;

#pragma warning disable IDE0130
namespace JOSYN.Commons.Schedule;

/// <summary>
/// Parses a schedule definition file from its JSONC text representation.
/// </summary>
/// <inheritdoc cref="IScheduleParser"/>
public static partial class ScheduleParser
{
    /// <inheritdoc cref="IScheduleParser.Parse"/>
    public static Result<ScheduleDefinition> Parse(string text)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(text, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException ex)
        {
            return Result.Error($"Schedule parse failed: invalid JSON — {ex.Message}");
        }

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return Result.Error("Schedule file must be a JSON array of rule objects.");

        var (rules, errors) = ParseAllElements(doc.RootElement);
        return errors.Count > 0 ? Result.Error(FormatErrors(errors)) : new ScheduleDefinition(rules);

        //
        // nested helpers
        //

        static (List<ScheduleRule> Rules, List<string> Errors) ParseAllElements(JsonElement array)
        {
            var rules  = new List<ScheduleRule>();
            var errors = new List<string>();
            var index  = 0;

            foreach (var element in array.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"Rule {index + 1}: expected a JSON object, got {element.ValueKind}.");
                    index++;
                    continue;
                }

                var result = ParseRule(element, index);
                if (result.Succeeded)
                    rules.Add(result.Value);
                else
                    errors.Add(result.ErrorMessage ?? $"Rule {index + 1}: unknown error.");

                index++;
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
