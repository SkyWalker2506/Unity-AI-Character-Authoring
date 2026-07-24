# State And Merge

Field identity is:

```text
providerId + stableFieldKey
```

Provider schema version is migration context and is not part of field identity.

Value states are distinct:

- `Unspecified`
- `Absent`
- `Null`
- `Known`
- `Unknown`
- `Computed`

Default ownership policy is `Enforced`.

`PreserveOnDrift` follows the normative table:

```text
C=B, N=B       -> No-op
C=B, N!=B      -> Apply desired
C!=B, N=B      -> Preserve current and record drift
C!=B, N!=B,C=N -> No-op, converged
C!=B, N!=B,C!=N -> Conflict
```

`Enforced` drift is visible in preview diagnostics before an overwrite can be applied.

`Unspecified` desired state has no opinion and preserves current without claiming drift. An `Unknown`
base/current/desired observation fails closed as a conflict instead of being silently overwritten.

Shared resources use owner sets, not refcounts. Deletion requires an empty owner set and deletable provenance (`CreatedByFramework` or `AdoptedByFramework`).
