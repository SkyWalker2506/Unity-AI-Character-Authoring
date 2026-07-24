# CLI

Batch entry point:

```text
-batchmode -nographics -quit
-projectPath <project>
-executeMethod BlackMountains.AICharacterAuthoring.Editor.Transport.AuthoringCli.Run
--aca-command <command>
--aca-request <absolute-json-path>
--aca-result <absolute-json-path>
```

Request and result paths must be outside the Unity project root.

Implemented commands:

- `doctor`
- `plan`
- `preview`
- `recover-status`
- `export-state`

Gated commands:

- `index`
- `apply`
- `validate`
- `recover-resume`
- `recover-rollback`

Gated commands return a structured not-implemented diagnostic and perform no managed asset mutation.
