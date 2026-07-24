using System.Collections.Generic;

namespace BlackMountains.AICharacterAuthoring
{
    /// <summary>
    /// Declarative authoring request for deciding what an NPC should talk about and why.
    /// The provider produces data only; it never renders subtitles or mutates game dialogue state.
    /// </summary>
    public sealed class NarrativeContentRequest
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string RequestId { get; set; }
        public string CharacterSpecId { get; set; }
        public string SourceLocale { get; set; } = "en";
        public AuthoringQualityPolicy QualityPolicy { get; set; } = AuthoringQualityPolicy.AIRequired;
        public List<string> AllowedTopics { get; set; } = new List<string>();
        public List<string> ForbiddenTopics { get; set; } = new List<string>();
        public List<string> TargetLocales { get; set; } = new List<string>();
        public Dictionary<string, string> WorldContext { get; set; } =
            new Dictionary<string, string>(System.StringComparer.Ordinal);
        public Dictionary<string, string> CharacterContext { get; set; } =
            new Dictionary<string, string>(System.StringComparer.Ordinal);
        public List<string> Constraints { get; set; } = new List<string>();
    }

    /// <summary>
    /// A semantic conversation unit. Game-specific dialogue systems decide when and how to play it.
    /// </summary>
    public sealed class NarrativeBeat
    {
        public string BeatId { get; set; }
        public string TopicId { get; set; }
        public string Purpose { get; set; }
        public string CooldownGroup { get; set; }
        public List<string> Preconditions { get; set; } = new List<string>();
        public List<NarrativeLineData> Lines { get; set; } = new List<NarrativeLineData>();
    }

    /// <summary>
    /// Engine-neutral line data. LocalizationKey is the stable handoff identity.
    /// </summary>
    public sealed class NarrativeLineData
    {
        public string LineId { get; set; }
        public string SpeakerId { get; set; }
        public string LocalizationKey { get; set; }
        public string SourceLocale { get; set; } = "en";
        public string SourceText { get; set; }
        public string Intent { get; set; }
        public string Emotion { get; set; }
        public decimal SuggestedMinimumDisplaySeconds { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, string> Variables { get; set; } =
            new Dictionary<string, string>(System.StringComparer.Ordinal);
    }

    /// <summary>
    /// Translation handoff generated from accepted source lines.
    /// </summary>
    public sealed class NarrativeLocalizationRequest
    {
        public string RequestId { get; set; }
        public string LocalizationKey { get; set; }
        public string SourceLocale { get; set; } = "en";
        public string SourceText { get; set; }
        public string SpeakerId { get; set; }
        public string NarrativeIntent { get; set; }
        public List<string> TargetLocales { get; set; } = new List<string>();
        public Dictionary<string, string> Context { get; set; } =
            new Dictionary<string, string>(System.StringComparer.Ordinal);
        public List<string> Constraints { get; set; } = new List<string>();
    }

    public sealed class LocalizedNarrativeEntry
    {
        public string LocalizationKey { get; set; }
        public string Locale { get; set; }
        public string Text { get; set; }
        public Dictionary<string, string> Metadata { get; set; } =
            new Dictionary<string, string>(System.StringComparer.Ordinal);
    }

    public sealed class NarrativeContentPackage
    {
        public const string CurrentSchemaVersion = "1";

        public string SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string PackageId { get; set; }
        public string CharacterSpecId { get; set; }
        public List<NarrativeBeat> Beats { get; set; } = new List<NarrativeBeat>();
        public List<NarrativeLocalizationRequest> LocalizationRequests { get; set; } =
            new List<NarrativeLocalizationRequest>();
        public List<LocalizedNarrativeEntry> LocalizedEntries { get; set; } =
            new List<LocalizedNarrativeEntry>();
        public IntelligenceProviderDescriptor Provenance { get; set; }
        public List<AuthoringDiagnostic> Diagnostics { get; set; } =
            new List<AuthoringDiagnostic>();
    }

    public sealed class NarrativeContentResult
    {
        public bool Success { get; set; }
        public NarrativeContentPackage Content { get; set; }
        public List<AuthoringDiagnostic> Diagnostics { get; set; } =
            new List<AuthoringDiagnostic>();
    }

    /// <summary>
    /// Creative narrative generation boundary. AIRequired requests must fail closed.
    /// </summary>
    public interface INarrativeContentProvider
    {
        IntelligenceProviderDescriptor Descriptor { get; }
        NarrativeContentResult Generate(NarrativeContentRequest request);
    }

    /// <summary>
    /// Optional translation/localization provider. It exchanges structured data only.
    /// </summary>
    public interface INarrativeLocalizationProvider
    {
        IntelligenceProviderDescriptor Descriptor { get; }
        NarrativeContentResult Localize(NarrativeContentPackage source);
    }

    /// <summary>
    /// Game Pack-owned sink that imports accepted narrative/localization data into the game's
    /// dialogue, subtitle, voice, or localization database. Rendering remains outside the package.
    /// </summary>
    public interface INarrativeDataSink
    {
        DiagnosticList Validate(NarrativeContentPackage content);
        void Store(NarrativeContentPackage content);
    }
}
