# Provider Authoring

Providers declare capabilities and field ownership. They do not mutate assets.

Use package contracts:

- `ICharacterAuthoringProvider` declares provider metadata and contributes capabilities.
- `ICapabilityProvider` expands a capability into deterministic operations.
- Editor field schemas declare stable `providerId + stableFieldKey` ownership.
- Operation handlers are the only extension point that can perform project mutation, and only while called by `PlanApplicationService` inside `MutationScope`.

No Way Back skeleton providers register:

- hostile humanoid;
- locomotion;
- perception;
- melee;
- death;
- Behavior Designer boundary;
- RA2 DeathOnly boundary.

The current skeleton intentionally does not edit prefabs, behavior graphs, or RA2 data.
