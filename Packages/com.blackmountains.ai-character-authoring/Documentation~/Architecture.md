# Architecture

The package is split into runtime contracts and Editor-only implementation.

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
