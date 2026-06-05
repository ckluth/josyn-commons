# josyn-commons

> **Domain-agnostic platform utilities for the JOSYN platform.**
> Cross-repo helpers with no domain knowledge — never referenced by `josyn-foundation`.

Start here before adding anything to this repo.

---

## What is this?

`josyn-commons` is a shared toolbox for the JOSYN platform. It collects generic, reusable
C# helpers that emerge across multiple repos and have no natural home in any single one of them.

It is **not** a second foundation. The distinction matters:

| | `josyn-foundation` | `josyn-commons` |
|---|---|---|
| Role | Infrastructure primitives | Generic utility helpers |
| Stability | Stable forever — frozen contracts | Open for growth — evolves over time |
| Status | First-class platform citizen | Utility satellite |
| Versioning | Immovable baseline | Independent, backward-compatible |

---

## Architectural position

`josyn-commons` sits at the **bottom of the dependency graph** — alongside `josyn-foundation`,
but in its own world:

```
JOSYN.Commons.*  (no deps, or ResultPattern only)
      ▲  ▲  ▲
      │  │  │
   (any josyn repo may reference it — josyn-foundation never does)
```

The full platform dependency graph is documented in
[josyn-platform/architecture/overview.md](../josyn-platform/architecture/overview.md).

---

## The one hard rule

> **`josyn-foundation` never references `josyn-commons`.**

This is non-negotiable. Foundation is the bedrock. It must not depend on a growing,
evolving layer. Any change that would require foundation to reference commons is a
design signal that the helper belongs somewhere else — or that foundation needs to grow
in a controlled, versioned way instead.

---

## What belongs here

A helper belongs in `josyn-commons` if and only if **all three** of the following are true:

1. **Cross-repo reuse** — it is (or will be) used in ≥ 2 josyn repos.
2. **No domain knowledge** — it knows nothing about sessions, jobs, IPC, scheduling,
   or any other JOSYN concept.
3. **Universally portable** — it could live in any C# project, not just JOSYN.

If a helper fails any of these criteria, it stays in the consuming repo. When it later
proves its cross-repo value, it can be promoted here.

---

## Dependency constraint

Packages in this repo may only reference:

- **Nothing** *(preferred)* — pure utility with no external dependencies.
- **`JOSYN.Foundation.ResultPattern`** — only when the helper needs to express failure
  as a value (`Result` / `Result<T>`).

The following are **forbidden** as dependencies:

- `JOSYN.Foundation.PropertyBag`
- `JOSYN.Foundation.JIP`
- Any package from `josyn-jap`, `josyn-job-host`, or `josyn-backend`

Pulling in any of these would drag domain knowledge into a generic layer and destroy
the bottom-of-DAG guarantee.

---

## Coding conventions

All code in this repo follows the platform-wide coding standards:
[josyn-platform/architecture/coding-standards.md](../josyn-platform/architecture/coding-standards.md)

Key points that apply with particular force here:

- **Static wins** — helpers are almost always `static class` with `static` methods.
- **Immutability by default** — prefer `record` over `class` for any data types.
- **Errors as values** — return `Result` / `Result<T>`, never throw.
- **Minimal surface area** — expose only what is needed; internal types stay internal.
- **Contracts folder** — every public static type gets a companion `IXxx` interface
  with `static abstract` members in `Contracts/`.

---

## Naming conventions

Follows the platform-wide naming guide:
[josyn-platform/architecture/naming-conventions.md](../josyn-platform/architecture/naming-conventions.md)

| Thing | Pattern | Example |
|---|---|---|
| Repo | `josyn-commons` | — |
| Package directory | `josyn-commons-<topic>/` | `josyn-commons-strings/` |
| Assembly / NuGet ID | `JOSYN.Commons.<Topic>` | `JOSYN.Commons.Strings` |
| Namespace root | `JOSYN.Commons.<Topic>` | `JOSYN.Commons.Strings` |

---

## Package structure

Each helper package lives in its own sub-directory with its own solution, test project,
and local build scripts — identical layout to `josyn-foundation`:

```
josyn-commons-<topic>/
├── JOSYN.Commons.<Topic>/
│   ├── JOSYN.Commons.<Topic>.csproj
│   ├── Contracts/                     ← static abstract interfaces
│   └── ...
├── JOSYN.Commons.<Topic>.Test/
│   └── JOSYN.Commons.<Topic>.Test.csproj
├── JOSYN.Commons.<Topic>.slnx
├── Directory.Build.props
├── nuget.config
└── .local-build/
    ├── build.cmd
    ├── test.cmd
    └── pack.cmd                       ← outputs to ../../local-packages/
```

---

## Status

| Package | Sub-folder | Purpose |
|---------|-----------|---------|
| `JOSYN.Commons.Log` | `josyn-commons-log/` | Process-local file logger — migrated from `JOSYN.Jap.Shared.Log` per [ADR-008](../josyn-platform/decisions/ADR-008-locallog-relocation.md) |

License: MIT | Company: HAEVG AG | Target: net10.0
