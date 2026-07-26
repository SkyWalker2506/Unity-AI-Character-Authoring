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

    public sealed class SpecGenerationResult
    {
        public bool Success { get; set; }
        public CharacterSpec Spec { get; set; }
        public List<AuthoringDiagnostic> Diagnostics { get; set; } = new List<AuthoringDiagnostic>();
    }
}
