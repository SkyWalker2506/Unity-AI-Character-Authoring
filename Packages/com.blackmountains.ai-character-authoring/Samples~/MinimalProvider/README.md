# Minimal Provider Sample

This sample is documentation-only in the foundation slice.

A provider should:

1. Implement `ICharacterAuthoringProvider`.
2. Register capability descriptors through `ProviderPlanningContext`.
3. Declare field schemas in an Editor provider.
4. Add operation handlers only for deterministic, journalable mutations.
5. Never mutate assets from registration, planning, CLI, UI, or facade code.
