# Changelog

## 0.1.0 - 2026-07-23

- Added the embedded package foundation.
- Added pure value, merge, planning, manifest, execution boundary, recovery, CLI, and facade contracts.
- Added No Way Back project-side provider skeleton under `Assets/Game/AICharacterAuthoring`.
- Added focused NUnit tests for the implemented foundation; 23/23 pass in Unity 6000.4.3f1 EditMode batchmode.
- Hardened capability-level AI-required enforcement, semantic plan hashing, non-reversible approval,
  atomic external journals/results, lock/recovery ordering, and indeterminate merge handling.
- Added engine-neutral Narrator and localization exchange contracts. Creative NPC topic/dialogue
  generation defaults to `AIRequired`; subtitle rendering and game database mutation remain Game Pack concerns.
