using BlackMountains.AuthoringKernel;
using BlackMountains.AuthoringKernel.Editor;
namespace BlackMountains.AICharacterAuthoring.Editor
{
    public sealed class IntelligencePolicyResolution
    {
        public bool Success { get; set; }
        public bool UsedAI { get; set; }
        public bool UsedDeterministicFallback { get; set; }
        public AuthoringDiagnostic Diagnostic { get; set; }
    }

    public static class IntelligencePolicyResolver
    {
        public static IntelligencePolicyResolution Resolve(AuthoringQualityPolicy policy, bool aiProviderAvailable, bool deterministicFallbackAvailable)
        {
            switch (policy)
            {
                case AuthoringQualityPolicy.DeterministicOnly:
                    return new IntelligencePolicyResolution
                    {
                        Success = true,
                        UsedAI = false,
                        UsedDeterministicFallback = true
                    };
                case AuthoringQualityPolicy.AIOptional:
                    return new IntelligencePolicyResolution
                    {
                        Success = aiProviderAvailable || deterministicFallbackAvailable,
                        UsedAI = aiProviderAvailable,
                        UsedDeterministicFallback = !aiProviderAvailable && deterministicFallbackAvailable,
                        Diagnostic = aiProviderAvailable || deterministicFallbackAvailable
                            ? null
                            : AuthoringDiagnostic.Error("ACA-AI-OPTIONAL-NO-FALLBACK", "AIOptional requires either an AI provider or a declared deterministic fallback.")
                    };
                case AuthoringQualityPolicy.AIRequired:
                    return new IntelligencePolicyResolution
                    {
                        Success = aiProviderAvailable,
                        UsedAI = aiProviderAvailable,
                        UsedDeterministicFallback = false,
                        Diagnostic = aiProviderAvailable
                            ? null
                            : AuthoringDiagnostic.Error("ACA-AI-REQUIRED-UNAVAILABLE", "AIRequired failed closed because no AI provider is available.")
                    };
                default:
                    return new IntelligencePolicyResolution
                    {
                        Success = false,
                        Diagnostic = AuthoringDiagnostic.Error("ACA-AI-POLICY-UNKNOWN", "Unknown AI policy.")
                    };
            }
        }
    }
}
