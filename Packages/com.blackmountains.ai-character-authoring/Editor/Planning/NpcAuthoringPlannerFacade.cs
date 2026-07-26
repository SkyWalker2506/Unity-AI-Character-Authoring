using BlackMountains.AICharacterAuthoring;

namespace BlackMountains.AICharacterAuthoring.Editor
{
    /// <summary>
    /// The Editor-side entry point to the authoritative <see cref="NpcDefinition"/> planning stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this type exists.</b> Before WP-07 the rich planner
    /// (<see cref="NpcRecipePlanner"/> → <see cref="NpcAuthoringPlan"/>) was referenced by exactly
    /// zero Editor files — it was reachable only from runtime tests. A planning stack that no
    /// Editor assembly can name is not a pipeline, it is dead weight. This facade is the single
    /// declared seam where the Editor half of the package touches the authoritative model, so
    /// WP-08's <c>NpcExecutionPlanCompiler</c> has something to attach to.
    /// </para>
    /// <para>
    /// <b>What this type deliberately is not.</b> It is a visibility seam, not a bridge. Every
    /// method here forwards to the runtime planner unchanged — no defaulting, no fixing up, no
    /// policy. If a behaviour belongs to planning it belongs in <see cref="NpcRecipePlanner"/>; if
    /// it belongs to execution it belongs in WP-08's compiler. Adding logic here would recreate
    /// the exact split-authority defect WP-07 exists to remove.
    /// </para>
    /// <para>
    /// <b>The plan produced here cannot be applied yet.</b> <see cref="NpcAuthoringPlan"/> is a
    /// <i>domain</i> plan; the kernel apply machine consumes a <c>GenerationPlan</c>
    /// (an <i>execution</i> plan). The compiler between the two does not exist — see
    /// <see cref="ExecutionPlanCompilerAvailable"/>.
    /// </para>
    /// </remarks>
    public static class NpcAuthoringPlannerFacade
    {
        /// <summary>
        /// Work package that introduces the domain-plan → execution-plan compiler.
        /// </summary>
        public const string ExecutionPlanCompilerWorkPackage = "WP-08";

        /// <summary>
        /// Whether a compiled <see cref="NpcAuthoringPlan"/> can currently be turned into a kernel
        /// <c>GenerationPlan</c> and applied. Always <c>false</c> until
        /// <see cref="ExecutionPlanCompilerWorkPackage"/> lands.
        /// </summary>
        /// <remarks>
        /// This is a tripwire, not a feature flag: nothing branches on it today. It exists so that
        /// the absence of an execution path is an asserted fact rather than tribal knowledge, and
        /// so WP-08 cannot claim completion without flipping it and the test that pins it.
        /// </remarks>
        public static bool ExecutionPlanCompilerAvailable => false;

        /// <summary>
        /// Validates a definition against the authoritative rules. Pure forward to
        /// <see cref="NpcDefinitionValidator.Validate"/>.
        /// </summary>
        public static NpcDefinitionValidationResult ValidateDefinition(
            NpcDefinition definition,
            NpcBehaviorRecipe behaviorRecipe,
            NpcValidationContext context)
        {
            return NpcDefinitionValidator.Validate(definition, behaviorRecipe, context);
        }

        /// <summary>
        /// Compiles a definition into the domain plan. Pure forward to
        /// <see cref="NpcRecipePlanner.Compile"/>; validation runs inside the planner, so a caller
        /// must not pre-validate and must read <c>Diagnostics</c> on failure.
        /// </summary>
        public static NpcPlanCompilationResult CompileDomainPlan(
            NpcDefinition definition,
            NpcBehaviorRecipe behaviorRecipe,
            NpcValidationContext context)
        {
            return NpcRecipePlanner.Compile(definition, behaviorRecipe, context);
        }
    }
}
