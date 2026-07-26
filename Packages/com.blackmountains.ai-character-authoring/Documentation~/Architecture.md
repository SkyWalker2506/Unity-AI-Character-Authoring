# Architecture

The package is split into runtime contracts and Editor-only implementation, and — since WP-06 —
each of those halves is split again into a domain-neutral **authoring kernel** and the character domain:

| Assembly | Folder | Contains |
| --- | --- | --- |
| `BlackMountains.AuthoringKernel` | `Runtime/Kernel/` | value states, canonical serialization, diagnostics, identifiers, plan/operation types, plan digest |
| `BlackMountains.AuthoringKernel.Editor` | `Editor/Kernel/` | execution boundary, approval, lock/journal/recovery, snapshots, ownership, three-way merge, manifest DTOs |
| `BlackMountains.AICharacterAuthoring.Runtime` | `Runtime/` | `CharacterSpec`, `Npc*`, narrative/provider/capability contracts |
| `BlackMountains.AICharacterAuthoring.Editor` | `Editor/` | provider registry, plan compiler, CLI, Synaptic facade |

The kernel assemblies reference no character type and no Unity engine assembly; both rules are enforced
by `Tests/Kernel/Editor/KernelBoundaryTests.cs`. They move to a `bm-authoring-kernel` package at WP-52.
See `docs/AI/ARCHITECTURE.md` and `docs/AI/CONTRACTS.md` in the repository root.

Runtime contains portable data:

- stable identifiers;
- explicit value states;
- canonical values and hashes;
- diagnostics;
- `CharacterSpec`;
- `GenerationPlan`;
- provider, capability, and intelligence contracts.

Editor contains deterministic authoring infrastructure:

- field schemas and normalized snapshots;
- three-way merge;
- provider registry and pure compiler;
- manifest DTOs;
- operation-handler boundary;
- mutation scope;
- approval token;
- external path policy;
- lock, journal, and recovery status;
- CLI and Synaptic-compatible facade.

Dependencies point inward. Package core does not reference No Way Back, RA2, Polymind, Sensor Toolkit, Synaptic, or Behavior Designer.

Current project adapters live under `Assets/Game/AICharacterAuthoring` and remain in predefined Unity assemblies so they can see `Assembly-CSharp` systems such as RA2 when real handlers are later implemented.
