# ACA (Unity-AI-Character-Authoring) — Durum

> **Bu dosya her iş paketi tesliminde güncellenir.** Güncel değilse teslim eksiktir.

**Son güncelleme:** 2026-07-26 (WP-06 kernel asmdef ayrımı)
**Faz:** Gate 1 öncesi — kernel ACA'nın içinde, kendi asmdef'iyle. Governed mutation hattı **hâlâ kapalı**.

## Tamamlananlar

| İş | Ne getirdi | Kanıt |
|---|---|---|
| (önceki) | Paket çekirdeği: değer modeli, kanonik serileştirme, merge motoru, plan compiler, approval/journal/lock primitifleri, read-only CLI | `b0b66f7` |
| **WP-06** | **İki kernel asmdef'i** + kimlik nötrleştirme + kernel sınırının test ile zorlanması | Aşağıdaki koşum kanıtı |

### WP-06 ne yaptı

1. **12 domain-bağımsız dosya** `Runtime/Kernel/` ve `Editor/Kernel/` altına taşındı; iki yeni assembly:
   - `BlackMountains.AuthoringKernel` — tüm platformlar, `noEngineReferences: true`, sıfır referans.
   - `BlackMountains.AuthoringKernel.Editor` — Editor platformu, `noEngineReferences: true`,
     yalnız kernel runtime + Newtonsoft.
2. **13. ve 14. dosya:** `GenerationOperationComparer` + `GenerationPlanHasher`,
   `Editor/Compilation/GenerationPlanCompiler.cs`'den `Runtime/Kernel/Model/GenerationPlanHashing.cs`'e
   çıkarıldı. Zorunluydu: `PlanApplicationService` (kernel) bunlara bağlı; ACA Editor'da kalsalardı
   kernel → domain referansı doğardı. İkisi de saf C#, domain bilmiyor.
3. **Kimlik nötrleştirme:** `CharacterSpecId → SubjectSpecId` (plan + manifest),
   `CharacterSpecHash → SubjectSpecHash`, `CharacterId → SubjectId`, namespace'ler
   `BlackMountains.AuthoringKernel[.Editor]`. Eski adlar `*/Kernel/Compat/ObsoleteNameBridges.cs`
   içinde `[Obsolete]` köprü olarak duruyor.
4. **Kural derleme + test ile zorlanıyor** (aşağıda).

### WP-06 koşum kanıtı (2026-07-26, gerçekten koşuldu)

Komut:

```
/Applications/Unity/Hub/Editor/6000.4.3f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -runTests -testPlatform EditMode \
  -projectPath ~/Projects/bm-fixture -testResults /tmp/wp06.xml -logFile -
```

| Koşum | Sonuç |
|---|---|
| Baseline (WP-06 öncesi, aynı makine, aynı gün) | exit 0 · **130 test / 130 geçti / 0 kaldı** |
| WP-06 sonrası | exit 0 · **143 test / 143 geçti / 0 kaldı / 0 atlandı** · `error CS` sayısı 0 |

| Assembly | Baseline | WP-06 sonrası |
|---|---:|---:|
| `BlackMountains.AICharacterAuthoring.Editor.Tests` | 20 | 20 |
| `BlackMountains.AICharacterAuthoring.Runtime.Tests` | 26 | 26 |
| `BlackMountains.AnimationLibrary.Editor.Tests` | 21 | 21 |
| `BlackMountains.AnimationLibrary.Runtime.Tests` | 62 | 62 |
| `BlackMountains.AuthoringKernel.Editor.Tests` | — | **13 (yeni)** |
| `BlackMountains.Fixture.EditMode.Tests` | 1 | 1 |

**Regresyon yok:** mevcut 130 testin hiçbiri değişmedi; artış tamamen yeni kernel sınır testlerinden.

### Kernel sınırı gerçekten zorlanıyor mu — negatif kontrol koşuldu

`Editor/Kernel/*.asmdef`'e `BlackMountains.AICharacterAuthoring.Runtime` **kasten** eklendi ve suite
yeniden koşuldu:

| Koşum | Sonuç |
|---|---|
| İhlal enjekte edilmiş | exit **2** · 143 test / 142 geçti / **1 kaldı** |
| Kalan test | `KernelAssemblyDefinitionDeclaresNoCharacterAuthoringReference("BlackMountains.AuthoringKernel.Editor")` |
| Mesaj | `...asmdef must not list a character-authoring assembly. Found: BlackMountains.AICharacterAuthoring.Runtime` |

Enjeksiyon geri alındı; asmdef dosyasının SHA-256'sı 143/143 üreten hâlle **birebir aynı** doğrulandı.

Dikkat: bu senaryoda **reflection testi geçti**, yalnız asmdef testi kaldı — çünkü bildirilen ama
kullanılmayan bir referans metadata'ya yazılmaz. İki katmanlı kontrolün nedeni budur:

| Test | Neyi yakalar |
|---|---|
| `KernelAssemblyDoesNotReferenceCharacterAuthoring` | kernel kodu domain tipine *dokunursa* (reflection) |
| `KernelAssemblyDefinitionDeclaresNoCharacterAuthoringReference` | asmdef'e referans *eklenirse* (CompilationPipeline) |
| `KernelAssemblyIsEngineFree` | kernel'e `UnityEngine`/`UnityEditor` sızarsa |
| `KernelExposesNoSubjectDomainTypes` | kernel `Npc*`/`Character*` tip *yayımlarsa* (`[Obsolete]` köprüler hariç) |

Ayrıca `Tests/Kernel/Editor/` assembly'si **kasıtlı olarak ACA'ya referans vermez** — kernel'in tek
başına tüketilebilir olduğunun kanıtı, o assembly'nin derleniyor olmasıdır.

### Plan digest'i değişmedi

`GenerationPlanHasher` anahtar grameri korundu (`"specId"` anahtarı aynı kaldı), yani WP-06 öncesi
üretilmiş plan hash'leri ve onlara bağlı approval token'ları geçerli. Golden vektör bağımsız bir
**Python** uygulamasıyla üretildi ve C# uygulamasıyla ilk denemede birebir eşleşti
(`d566ed35a599a310ee2f61063416d8920c006759880bd03a22ba46ae597f498b`) — C1'in "aynı vektör, iki dil,
sıfır fark" ölçümünün küçük ölçekli ilk kanıtı.

## Bilinen sınırlar / bilinçli kararlar

- **Kernel `com.unity.nuget.newtonsoft-json`'a bağımlıdır.** Kaynak: `Editor/Kernel/Recovery/JournalAndLock.cs`
  (`JsonConvert.SerializeObject/DeserializeObject` ile journal yazımı/okuması). Yani
  `BlackMountains.AuthoringKernel.Editor` asmdef'i `Unity.Nuget.Newtonsoft-Json` referansını taşır ve
  WP-52'de `bm-authoring-kernel` paketi bu UPM bağımlılığını `package.json`'una **yazmak zorundadır**.
  `BlackMountains.AuthoringKernel` (runtime) tarafının böyle bir bağımlılığı **yok** — saf BCL.
- **Tanı kodları hâlâ `ACA-*`.** WP-06'da bilerek dokunulmadı; kodlar testlerle doğrulanan bir
  sözleşmedir ve kod ad alanı tahsisi (harita K3) bir `bm-contracts` işidir. Kernel'in nötr kod ön eki
  o iş paketinde verilir.
- **Namespace değişimi kırıcıdır.** MDP pinli olduğu için bugün kimseyi kırmıyor. Pinsiz bir tüketici
  için göç maliyeti dosya başına bir `using` satırıdır — bkz. `CONTRACTS.md` §2.
- **`[Obsolete]` köprüler serileştirmeye kapalı değil.** Nötr property ile aynı depoyu paylaşıyorlar;
  WP-10'da plan/manifest JSON'a yazılırken açıkça hariç tutulmalıdır, yoksa aynı değer iki anahtarla
  yazılır. Bugün bu tipler hiç serileştirilmiyor (yalnız `ExecutionJournal` serileştiriliyor).
- **Varsayılan external root ve journal fallback adı değişti** (`.../BlackMountains/AuthoringKernel`,
  `unknown-subject`). Diskte bu tarihte hiç journal üretilmediği için taşınacak veri yok.
- **PlayMode koşulmadı.** WP-06 hiçbir PlayMode davranışına dokunmuyor; ACA'nın `[UnityTest]` sayısı
  hâlâ 0 ve her iki ACA test asmdef'i `includePlatforms: ["Editor"]`.

## Kasıtlı eksikler — TAMAMLAMAYIN

Aşağıdakiler unutulmuş değil, **bilinçli olarak ertelenmiştir**:

- **Gerçek mutation handler'ları** (`PrefabBuildOperationHandler` vb.) — WP-09. Bugün her `apply`
  `ACA-OPERATION-NOT-IMPLEMENTED` ile duruyor ve bu **doğru davranıştır**: journal bile açılmıyor.
- **Plan store · approval artifact · dosya tabanlı manifest store** — WP-10. Bugün `ApprovalToken`
  yalnız bellek içi, `InMemoryManifestStore` tek store.
- **`recover.abandon` / rollback yürütücüsü** — WP-11.
- **`NormalizedSnapshot` üreticisi** — merge motoru tam ve testli ama besleyicisi yok; prefabdan
  snapshot üretimi WP-21.
- **Preview render rig** — kernel'e **girmez** (Ç2 kararı). Unity tarafı UAL'de, Blender tarafı MF'de.
- **`bm-authoring-kernel` paket çıkarımı** — WP-52 / Gate 2. İkinci ve üçüncü tüketici (Model Forge,
  Scene) gelmeden yapılmaz. WP-06 o günü klasör taşımaya indirdi.
- **Kernel'in `bm-contracts` şemalarına bağlanması** — WP-23/24. Bugün kernel kendi tiplerini taşıyor.

## Sonraki iş paketi

**WP-07** — `NpcDefinition` tek otorite, `CharacterSpec` `[Obsolete]`.
Ardından WP-20 (round-trip düzeltmesi) → WP-08 (`NpcExecutionPlanCompiler`).
Sıra gerekçesi: harita §3.1 madde 4 — girdi kanalı kırıkken handler yazmak, doğrulanmamış girdiyle
diske yazmaktır.
