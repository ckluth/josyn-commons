# JOSYN.Commons.Schedule

Schedule definition language for JOSYN — parser, serializer, and validator.

Implements the JSONC-based schedule format defined in **ADR-026**.

## Overview

A schedule file is a JSON array of rule objects. Each object has a `type` property
that identifies the rule kind, plus type-specific properties. Comments (`//`) are
supported via JSONC processing.

Supported rule types: `interval`, `fixed`, `nth_weekday`, `monthly_date`,
`week_interval`, `once`, `exclude`.

## Usage

```csharp
// Parse a schedule file (JSONC supported — // line comments are stripped)
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

// Serialize back to JSON
Result<string> serialized = ScheduleSerializer.Serialize(result.Value);
```
