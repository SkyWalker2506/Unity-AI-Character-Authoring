# Black Mountains AI Character Authoring

Portable foundation for deterministic, reviewable AI-assisted character authoring in Unity.

This package does not let AI mutate prefabs directly. AI or other callers may produce declarative data; Unity asset mutation must go through the package Editor service, approval gate, operation handlers, lock, and journal.

## Implemented In This Slice

- Runtime identifiers, diagnostics, explicit value states, canonical values, invariant serialization, and stable SHA-256 hashing.
- Versioned `CharacterSpec`, `GenerationPlan`, provider/capability descriptors, and AI policy contracts.
- Engine-neutral Narrator contracts for NPC topics, semantic dialogue beats, localization handoff,
  accepted localized entries, provider provenance, and Game Pack-owned data sinks.
- Editor field identity and schemas, normalized snapshots, shared owner sets with provenance, and normative `base/current/new` merge behavior.
- Pure plan compiler with deterministic operation ordering, capability dependency/conflict checks, capability-level AI-required fail-closed policy, and zero mutation on compile failure.
- Manifest DTO/store abstraction, validation lifecycle states, mutation-scope enforcement, approval token model, operation-handler boundary, external path guardrails, mandatory apply lock, journal primitives, and recovery status inspection.
- Unity batch entry point `BlackMountains.AICharacterAuthoring.Editor.Transport.AuthoringCli.Run` with real read-only `doctor`, `plan`, `preview`, `recover-status`, and `export-state` commands.
- Synaptic-compatible file-backed facade that delegates to the same command path and contains no direct mutation code.
- NUnit coverage for value states, canonicalization/hashing, merge rows, field identity, deterministic ordering, compile-failure zero mutation, owner/provenance deletion, AI policy, path guardrails, and mutation-scope enforcement.

Unity 6000.4.3f1 batchmode verification passes all 23 package EditMode tests. The public `doctor`,
`plan`, and `preview` CLI paths were also smoke-tested with external request/result files.

## Deferred

- Real prefab/asset mutation handlers.
- Behavior Designer graph normalization and mutation.
- RA2 DeathOnly setup and validation.
- Disposable validation scene and PlayMode probes.
- Editor UI.
- Import/adopt/forget workflows and schema migrations beyond DTO boundaries.
- Full No Way Back hostile humanoid generation.
- No Way Back dialogue/subtitle/localization database adapter; the package currently defines only
  the data boundary and never renders subtitles itself.

Mutating CLI commands currently report not implemented instead of pretending success.
