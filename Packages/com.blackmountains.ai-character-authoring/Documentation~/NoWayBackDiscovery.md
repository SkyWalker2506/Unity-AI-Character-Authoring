# No Way Back Discovery

Source-level discovery for the initial package foundation.

- Existing hostile humanoid reference is `Assets/Game/Prefabs/Enemy/Zombie_RagdollTest.prefab`.
- Existing BD wiring is hard-coded in `Assets/Game/Editor/ZombieBDGraphBuilder.cs` and validates through `ZombieBDGraphValidator.cs`.
- The zombie stack is `ZombieManager`/`ZombieProfile`, `NavMeshAgent`, `ZombieTargeting`, `ZombieAttack`, Behavior Designer tasks, Animator, and RA2.
- RA2 setup code uses `FIMSpace.FProceduralAnimation.RagdollAnimator2` in `Assets/Game/Editor/RA2SetupTool.cs`.
- RA2 compiles through `Assembly-CSharp`, so package asmdefs cannot reference RA2 types.
- No project-side asmdef covers `Assets/Game/Scritps`; project adapters stay in predefined assemblies for now.
- Behavior Designer is installed as embedded package `com.opsive.behaviordesigner`, but package core does not reference it.
- No existing gameplay, vendor, prefab, scene, render, or asmdef file was modified for this foundation.

Deferred discovery-to-integration work:

- normalized Behavior Designer graph representation;
- real No Way Back operation handlers;
- RA2 DeathOnly serialized field schema and validation oracle;
- disposable validation scene and PlayMode probes.
