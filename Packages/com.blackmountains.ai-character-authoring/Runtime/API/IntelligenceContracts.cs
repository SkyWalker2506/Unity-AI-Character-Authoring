using System.Collections.Generic;
using BlackMountains.AuthoringKernel;

namespace BlackMountains.AICharacterAuthoring
{
    public enum AuthoringQualityPolicy
    {
        DeterministicOnly,
        AIOptional,
        AIRequired
    }

    public sealed class IntelligenceProviderDescriptor
    {
        public string ProviderId { get; set; }
        public string ModelId { get; set; }
        public string PromptTemplateVersion { get; set; }
        public string StructuredOutputSchemaVersion { get; set; }
    }

    public sealed class IntelligenceRequest
    {
        public string RequestId { get; set; }
        public AuthoringQualityPolicy Policy { get; set; }
        public string Prompt { get; set; }
        public Dictionary<string, string> Context { get; set; } = new Dictionary<string, string>(System.StringComparer.Ordinal);
    }

    public sealed class IntelligenceResult
    {
        public bool Success { get; set; }
        public string DeclarativeJson { get; set; }
        public IntelligenceProviderDescriptor Provider { get; set; }
        public List<AuthoringDiagnostic> Diagnostics { get; set; } = new List<AuthoringDiagnostic>();
    }

    public interface IAuthoringIntelligenceProvider
    {
        IntelligenceProviderDescriptor Descriptor { get; }
        IntelligenceResult Generate(IntelligenceRequest request);
    }

    /// <summary>
    /// Natural-language brief to <c>CharacterSpec</c> generator. Zero implementations exist.
    /// </summary>
    /// <remarks>
    /// WP-07: this interface targets the retired specification model. The authoritative brief
    /// parser must produce <see cref="NpcDefinition"/>; that capability
    /// (<c>bm.character-authoring/brief.parse</c>) is unwritten, so this is left in place rather
    /// than re-pointed speculatively.
    /// </remarks>
    public interface ICharacterSpecGenerator
    {
        SpecGenerationResult Generate(SpecGenerationRequest request);
    }

    public sealed class SpecGenerationRequest
    {
        public string RequestId { get; set; }
        public string NaturalLanguageIntent { get; set; }
        public AuthoringQualityPolicy Policy { get; set; } = AuthoringQualityPolicy.DeterministicOnly;
    }

    /// <summary>
    /// Result of the retired brief-to-<c>CharacterSpec</c> generator.
    /// </summary>
    /// <remarks>
    /// WP-07: 618 is suppressed here because carrying a deprecated payload is this type's whole
    /// job. The suppression is deliberately narrow — it must not hide the warning at any *new*
    /// call site.
    /// </remarks>
#pragma warning disable 618
    public sealed class SpecGenerationResult
    {
        public bool Success { get; set; }
        public CharacterSpec Spec { get; set; }
        public List<AuthoringDiagnostic> Diagnostics { get; set; } = new List<AuthoringDiagnostic>();
    }
#pragma warning restore 618
}
