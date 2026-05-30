# Advice for Maintainers of josyn-commons

Read the platform-wide maintainer's guide first:
[josyn-platform/MAINTAINERS.md](../josyn-platform/MAINTAINERS.md)

Everything there applies here. This file covers only what is specific to this repo.

---

## The one rule that defines this repo

**`josyn-foundation` never references `josyn-commons`.**

Not even once. Not even "just this helper, just this time."

Foundation is the bedrock. Commons grows. These two facts are only compatible
as long as the dependency never flows upward. The moment it does, foundation
is no longer independent — and everything that stands on foundation inherits
the instability of a growing layer.

The violation is silent. It compiles. It ships. It may never cause visible pain.
But it changes the shape of the architecture in a way that future decisions
will be made against — without knowing the shape is already wrong.

**Do not do it.**

---

## When a helper seems to belong in foundation instead

Stop. Ask:

- Is this helper truly foundational — stable, protocol-relevant, depended upon forever?
  → It belongs in `josyn-foundation`. Move it there deliberately, with an ADR.

- Does it carry JOSYN domain knowledge?
  → It does not belong in `josyn-commons`. Keep it in the consuming repo.

- Is it genuinely generic, but only used in one repo so far?
  → Keep it in the consuming repo. Promote it here when it proves cross-repo value.

None of these paths leads to foundation referencing commons.
If you find yourself there, the design needs to change — not the rule.

