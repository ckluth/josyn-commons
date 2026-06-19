# JOSYN.Commons.Schedule

Schedule definition language for JOSYN — parser, serializer, and validator.

Implements the INI-based schedule format defined in **ADR-026**.

## Overview

A schedule file is a sequence of rule blocks separated by blank lines.
Each block has a `type` key that identifies the rule kind, plus type-specific keys.

Supported rule types: `interval`, `fixed`, `nth_weekday`, `monthly_date`,
`week_interval`, `once`, `exclude`.

## Usage

```csharp
// Parse a schedule file
Result<ScheduleDefinition> result = ScheduleParser.Parse(fileContent);
if (!result.Succeeded)
{
    Console.Error.WriteLine(result.ErrorMessage);
    return;
}

// Validate semantics
IReadOnlyList<ValidationIssue> issues = ScheduleValidator.Validate(result.Value);
foreach (var issue in issues)
    Console.WriteLine($"[{issue.Severity}] {issue.Message}");

// Serialize back to INI
Result<string> serialized = ScheduleSerializer.Serialize(result.Value);
```
