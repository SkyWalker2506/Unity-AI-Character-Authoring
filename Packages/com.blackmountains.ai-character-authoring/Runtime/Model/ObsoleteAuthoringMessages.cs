namespace BlackMountains.AICharacterAuthoring
{
    /// <summary>
    /// Deprecation messages for the retired <c>CharacterSpec</c> planning stack (WP-07).
    /// </summary>
    /// <remarks>
    /// Centralised so the wording is identical everywhere and can be asserted by a test. Each
    /// message must name the replacement *and* say where the missing piece lands, so a caller who
    /// hits the warning knows whether to migrate now or wait.
    /// </remarks>
    public static class ObsoleteAuthoringMessages
    {
        /// <summary>
        /// Warning text for <c>CharacterSpec</c>.
        /// </summary>
        public const string CharacterSpec =
            "CharacterSpec is retired (WP-07). Use NpcDefinition — it is the single authoritative " +
            "character specification. NpcDefinition already compiles to a domain plan via " +
            "NpcAuthoringPlannerFacade/NpcRecipePlanner; the domain-plan-to-execution-plan compiler " +
            "(NpcExecutionPlanCompiler) lands in WP-08. Until WP-08 there is no supported path from " +
            "either model to a real mutation, so do not migrate to CharacterSpec to get one.";

        /// <summary>
        /// Warning text for <c>GenerationPlanCompiler</c>.
        /// </summary>
        public const string GenerationPlanCompiler =
            "GenerationPlanCompiler is retired (WP-07). It emits one synthetic 'ensureCapability' " +
            "operation per capability and never populates TargetAssetGuid/TargetObjectId, so the " +
            "plans it produces cannot be applied. Use NpcDefinition with NpcAuthoringPlannerFacade " +
            "for the authoritative domain plan; the replacement execution-plan compiler " +
            "(NpcExecutionPlanCompiler) lands in WP-08.";
    }
}
