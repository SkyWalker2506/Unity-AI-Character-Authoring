# ACA — Public yüzeyler ve kararlılık garantileri

Bu repoyu **dışarıdan tüketenler** için. Kararlılık sınıfları: **Kararlı** (kırma), **Geçiş**
(`[Obsolete]`, silinme tarihi belli), **Akışkan** (haber vermeden değişir).

## 1. Assembly adları — Kararlı

| Assembly | Sınıf | Not |
|---|---|---|
| `BlackMountains.AuthoringKernel` | Kararlı | WP-52'de `bm-authoring-kernel` paketine taşınır; **ad değişmez** |
| `BlackMountains.AuthoringKernel.Editor` | Kararlı | aynı |
| `BlackMountains.AICharacterAuthoring.Runtime` | Kararlı | domain |
| `BlackMountains.AICharacterAuthoring.Editor` | Kararlı | domain + CLI |

Kernel assembly adları WP-06'da bilerek nötr seçildi: paket çıkarımı gününde tüketici asmdef'leri
değişmesin diye.

## 2. Namespace'ler — WP-06 kırıcı değişikliği

WP-06'da 12 dosya ACA'dan kernel'e taşındı ve namespace'leri nötrleşti:

| Eski | Yeni |
|---|---|
| `BlackMountains.AICharacterAuthoring` (kernel tipleri) | `BlackMountains.AuthoringKernel` |
| `BlackMountains.AICharacterAuthoring.Editor` (kernel tipleri) | `BlackMountains.AuthoringKernel.Editor` |

Taşınan tipler: `CanonicalValue` · `AuthoringFieldValue` · `AuthoringValueState` · `CanonicalValueKind`
· `CanonicalSerialization` · `AuthoringDiagnostic` · `DiagnosticList` · `DiagnosticSeverity`
· `AuthoringIdentifier` · `SubjectId`/`ProviderId`/`CapabilityId`/`FieldKey`/`OperationId`/`AssetId`
· `GenerationPlan` · `PlanSource` · `GenerationOperation` · `OperationSecurityLevel`
· `OperationReversibility` · `OperationLifecyclePhase` · `GenerationOperationComparer`
· `GenerationPlanHasher` · `ApprovalToken` · `PlanPreview` · `ApplyResult` · `IOperationHandler`
· `OperationHandlerRegistry` · `MutationScope` · `PlanApplicationService` · `AuthoringProjectIdentity`
· `AuthoringExternalPathPolicy` · `ExecutionJournal` · `AuthoringJournalStore` · `AuthoringApplyLock`
· `RecoveryService`/`RecoveryStatus` · `FieldIdentity` · `FieldSchema` · `NormalizedSnapshot`
· `NormalizedSnapshotHasher` · `OwnershipPolicy` · `ResourceProvenance` · `SharedResourceOwnership`
· `GenerationManifest` · `IManifestStore` · `InMemoryManifestStore` · `ThreeWayMergeEngine` + merge tipleri.

**Tüketici için göç:** ilgili dosyalara `using BlackMountains.AuthoringKernel;` (ve Editor tipleri için
`using BlackMountains.AuthoringKernel.Editor;`) ekle. Başka değişiklik gerekmez.

`GenerationPlanCompiler`, `AuthoringProviderRegistry`, `IntelligencePolicyResolver`, `CharacterSpec`,
tüm `Npc*` ve `Narrative*` tipleri ve `Editor.Transport.*` **yerinde kaldı** —
`BlackMountains.AICharacterAuthoring[.Editor]` namespace'lerindeler.

## 3. Üye adları — Geçiş (`[Obsolete]`, WP-52'de silinir)

| Eski ad | Yeni ad | Köprü |
|---|---|---|
| `GenerationPlan.CharacterSpecId` | `GenerationPlan.SubjectSpecId` | `[Obsolete]` property, aynı depoyu yazar |
| `GenerationManifest.CharacterSpecId` | `GenerationManifest.SubjectSpecId` | aynı |
| `GenerationManifest.CharacterSpecHash` | `GenerationManifest.SubjectSpecHash` | aynı |
| `CharacterId` (struct) | `SubjectId` | `[Obsolete]` struct, iki yönlü implicit dönüşüm |

Köprüler `*/Kernel/Compat/ObsoleteNameBridges.cs` içindedir.

> **Uyarı — serileştirme.** Köprü property'ler nötr property ile *aynı depoyu* paylaşır. Plan/manifest
> için bir JSON persistence katmanı yazıldığında (WP-10) köprüler **açıkça hariç tutulmalıdır**;
> aksi halde aynı değer iki anahtarla yazılır. Bugün bu tiplerin hiçbiri serileştirilmiyor.

## 4. Plan digest'i — Kararlı (wire contract)

`GenerationPlanHasher.Hash` anahtar grameri sözleşmedir. `CharacterSpecId → SubjectSpecId` yeniden
adlandırması digest anahtarını **değiştirmedi** (`"specId"` aynı kaldı); WP-06 öncesi üretilmiş her
plan hash'i ve ona bağlı approval token'ı geçerliliğini korur.

Golden vektör (bağımsız Python uygulamasıyla üretildi, C# ile birebir aynı):

```
plan: PlanId="plan.digest", SubjectSpecId="subject.digest",
      op: id="op.digest", kind="kernel.noop", target/key="target" (kalan alanlar varsayılan)
sha256 = d566ed35a599a310ee2f61063416d8920c006759880bd03a22ba46ae597f498b
```

Test: `Tests/Kernel/Editor/KernelBoundaryTests.cs::PlanDigestGoldenVectorSurvivedTheRename`.

## 5. Tanı kodları — Kararlı (bugünkü hâliyle)

Kernel'in ürettiği kodlar hâlâ `ACA-*` ön ekini taşır (`ACA-APPROVAL-DENIED`,
`ACA-OPERATION-NOT-IMPLEMENTED`, `ACA-PATH-PROJECT-ROOT`, `ACA-APPLY-*`, `ACA-RECOVERY-BLOCKED`,
`ACA-PREVIEW-*`, `AuthoringDiagnostic` varsayılanı `ACA-UNKNOWN`). Bu kodlar test tarafından
doğrulanıyor ve WP-06'da **kasıtlı olarak değiştirilmedi** — kod ad alanı tahsisi (K3) bir
`bm-contracts` işidir, asmdef ayrımının yan etkisi olarak yapılmaz.

## 6. Davranış değişiklikleri (WP-06) — Akışkan

| Ne | Eski | Yeni |
|---|---|---|
| Varsayılan external root | `<AppData>/BlackMountains/AICharacterAuthoring` | `<AppData>/BlackMountains/AuthoringKernel` |
| Journal manifest fallback adı | `unknown-character` | `unknown-subject` |

İkisi de yalnızca **varsayılan** yoldur; `AuthoringExternalPathPolicy` ctor'una açık `externalRoot`
verildiğinde etkisizdir. Diskte bu tarihte hiç journal üretilmediği için taşınacak veri yoktur.

## 7. CLI yüzeyi — değişmedi

`BlackMountains.AICharacterAuthoring.Editor.Transport.AuthoringCli.Run` ve komut adları
(`doctor`, `plan`, `preview`, `recover-status`, `export-state`) WP-06'da değişmedi.
