# ACA (Unity-AI-Character-Authoring) — Durum

> **Bu dosya her iş paketi tesliminde güncellenir.** Güncel değilse teslim eksiktir.

**Son güncelleme:** 2026-07-27 (WP-KNOWLEDGE knowledge katmanı)
**Faz:** Gate 1 öncesi — kernel ACA'nın içinde, kendi asmdef'iyle. Governed mutation hattı **hâlâ kapalı**.

## Zorunlu okuma sırası — işe başlamadan önce

ADR-002 §4 (`bm-contracts/docs/AI/ADR-002-knowledge-layer.md`) bu repoda da geçerlidir. Her ajan,
kod değiştirmeden önce şu sırayı izler:

| # | Dosya | Cevapladığı soru |
|---|---|---|
| 1 | **bu dosya** (`docs/AI/STATE.md`) | Neyin bitmiş, neyin **kasıtlı eksik** olduğu |
| 2 | `knowledge/_index.md` | Bu göreve hangi zanaat maddeleri uygun |
| 3 | seçilen `knowledge/*.md` | Ölçülmüş gerçeğin kendisi |
| 4 | — | işi yap |
| 5 | teslim raporu | `knowledgePackId` + `knowledgePackDigest` + `citedTopics[]` yaz |

Sıra önemli. `STATE.md` önce gelir çünkü "Kasıtlı eksikler" listesini okumayan ajan, bilinçli
olarak boş bırakılmış bir yeri "tamamlar" ve bir mimari kararı sessizce bozar.

**Tümünü okuma.** `knowledge/_index.md` bir görev→madde eşlemesi tutar; yalnız kendi görevine ait
olanları oku. Bu bir performans önlemi değil **doğruluk önlemi**: ilgisiz bir madde kararı
iyileştirmez, bulandırır.

`citedTopics: []` geçerlidir — her karar bir maddeye dayanmak zorunda değil. `knowledgePackId: null`
da yasak değil (acil düzeltme, mekanik görev), ama raporda **"bilgisiz karar"** olarak durur.

**Knowledge normatif değildir.** Hard gate'ler koddadır (asmdef sınırları, koruma testleri,
kanonik digest) ve **kod kazanır**. Bir madde ile kod çelişirse kod doğrudur ve madde güncellenir.

## Tamamlananlar

| İş | Ne getirdi | Kanıt |
|---|---|---|
| (önceki) | Paket çekirdeği: değer modeli, kanonik serileştirme, merge motoru, plan compiler, approval/journal/lock primitifleri, read-only CLI | `b0b66f7` |
| **WP-06** | **İki kernel asmdef'i** + kimlik nötrleştirme + kernel sınırının test ile zorlanması | Aşağıdaki koşum kanıtı |
| **WP-07** | **Tek otorite `NpcDefinition`** + eski yığın `[Obsolete]` + R2 riskinin **ölçülmesi** | Aşağıdaki koşum kanıtı |
| **WP-KNOWLEDGE** | `knowledge/` paketi (5 madde, tamamı `bm-measured`) + `SerializeReference` sıfır yüzeyinin teste bağlanması | Aşağıdaki koşum kanıtı |

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

### WP-07 ne yaptı

1. **`NpcDefinition` otorite ilan edildi** — tipin XML doc'u artık yetkili hattı yazıyor:
   `NpcDefinition → NpcDefinitionValidator → NpcRecipePlanner → NpcAuthoringPlan → (WP-08)
   NpcExecutionPlanCompiler → GenerationPlan`. Son ok **yok**; bugün hiçbir `NpcDefinition`
   kernel'in apply makinesine ulaşmıyor.
2. **`CharacterSpec` ve `GenerationPlanCompiler` `[Obsolete]`** (`IsError = false` — MDP pinli,
   derleme kırılmıyor). Mesajlar `Runtime/Model/ObsoleteAuthoringMessages.cs`'de merkezî; her ikisi
   de hem `NpcDefinition`'ı hem WP-08'i adıyla anıyor ve bu bir testle zorlanıyor.
   **Silinmediler** — MDP bu paketi commit ile pinliyor.
3. **`NpcAuthoringPlan` Editor'dan erişilebilir hale geldi.** WP-07 öncesi **sıfır** Editor dosyası
   bu tipi adlandırıyordu; zengin planlayıcı yazılmış ama çağrılamıyordu.
   `Editor/Planning/NpcAuthoringPlannerFacade.cs` tek bildirilmiş dikiş yeri —
   **köprü değil, görünürlük**: her metot runtime planlayıcıya olduğu gibi iletir, bir test bunu
   plan digest'i üzerinden zorlar. `ExecutionPlanCompilerAvailable => false` bir tripwire'dır
   (bugün hiçbir kod ona dallanmıyor); WP-08 onu ve onu sabitleyen testi aynı commit'te çevirmek
   zorunda.

### WP-07'nin asıl bulgusu — R2 riski ÖLÇÜLDÜ, kısmen çürütüldü

`character.md` R2 "veri modeli ne Unity'de ne JSON'da yaşayabiliyor" diyordu ama **hiç ölçülmemişti**.
Ölçüm artık `Tests/Editor/NpcDefinitionAuthorityTests.cs`'de:

| Ölçülen | Sonuç |
|---|---|
| `NpcDefinition` → JSON → `NpcDefinition` (maksimal fixture, 14 633 karakter JSON) | **0 alan kaybı** |
| Katalog örnekleri (Vendor · Bandit · Companion) | **0 alan kaybı** |
| `NpcAuthoringPlan` round-trip + `DeterministicHash` korunumu | **0 alan kaybı**, hash aynı |
| `AuthoringFieldValue` (`CharacterSpec`'in parametre kanalı) | **KAYIP** — `State=Known` → `Unspecified`, `Value=null` |
| `CharacterSpec.Parameters["height"]` | **KAYIP** — `State=Unspecified` |

**Yani R2 yanlış genellenmişti:** kırık olan `NpcDefinition` değil, `AuthoringFieldValue`'dur
(private ctor + get-only property + converter yok). `NpcDefinition` bu tipi hiç kullanmıyor —
otorite seçimi bu ölçümle desteklenmiş oldu, varsayımla değil.

Kayıp **sessizdir**: Newtonsoft istisna atmaz, `Unspecified` üretir. "Bilinmiyor" ile
"bilinçli olarak belirtilmemiş" arasındaki fark yok olur — 6 durumlu değer modelinin bütün amacı budur.
Bu yüzden iki ölçüm testi **kaybı kasten iddia ediyor**; WP-20 converter'ı eklediğinde bu testler
**silinmez, ters çevrilir** (assert mesajları bunu söylüyor).

**Ölçüm aleti de kontrol edildi.** `DeepCompare` reflection tabanlı; sessizce hiçbir şeyi
gezmeyen bir karşılaştırıcı "0 kayıp" sonucunu değersiz kılardı.
`DeepCompare_DetectsInducedLossAtEveryStructuralDepth` yedi ayrı derinlikte (kök skaler, iç içe
record decimal'i, liste elemanı, sözlük değeri, düşürülmüş sözlük anahtarı, null'lanmış alt nesne,
liste uzunluğu) kasıtlı kayıp enjekte edip **tespit edildiğini ve doğru yolda raporlandığını**
doğruluyor.

### WP-07 koşum kanıtı (2026-07-26, gerçekten koşuldu)

```
/Applications/Unity/Hub/Editor/6000.4.3f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -runTests -testPlatform EditMode \
  -projectPath ~/Projects/bm-fixture -testResults /tmp/wp07.xml -logFile -
```

| Koşum | Sonuç |
|---|---|
| Baseline (WP-07 değişiklikleri geri alınmış çalışma ağacı, aynı makine/gün) | exit 0 · **172 test / 172 geçti / 0 kaldı** |
| WP-07 sonrası | exit 0 · **186 test / 186 geçti / 0 kaldı / 0 atlandı** · `error CS` = 0 · sızan `warning CS0618` = 0 |

| Assembly | Baseline | WP-07 sonrası |
|---|---:|---:|
| `BlackMountains.AICharacterAuthoring.Editor.Tests` | 20 | **34 (+14)** |
| `BlackMountains.AICharacterAuthoring.Runtime.Tests` | 26 | 26 |
| `BlackMountains.AuthoringKernel.Editor.Tests` | 13 | 13 |
| `BlackMountains.AnimationLibrary.*` + fixture | 113 | 113 |

**Regresyon yok:** mevcut 172 testin hiçbiri değişmedi; artışın tamamı yeni WP-07 testleridir.

> Not: baseline WP-06'nın kaydettiği 143 değil **172**. Fark ACA'dan gelmiyor — ACA'nın sahibi
> olduğu üç assembly WP-06'daki sayılarla birebir aynı (20 + 26 + 13 = 59). Artış tamamen ayrı bir
> repodaki `Unity-Animation-Library` paketinden (`21→41`, `62→71`). Bu yüzden baseline her WP'de
> **yeniden ölçülmelidir**; fixture ACA'nın kontrol etmediği bir paketi de derliyor.

### `[Obsolete]` uyarıları derlemeyi kırmıyor — nasıl sağlandı

`IsError = false` tek başına yeterli değildi; uyarının *doğru yerde* susturulması gerekti.
`#pragma warning disable 618` yalnız dört yerde, her biri gerekçeli: eski yığının kendi
implementasyonu (`GenerationPlanCompiler.cs`), henüz göç etmemiş CLI verb'leri (`AuthoringCli.cs`),
eski tipi taşımak *işi olan* payload'lar (`SpecGenerationResult`, `CapabilityPlanningContext`) ve
eski yığını kasten koşan testler. Susturmalar **tüketici çağrı yerlerini kapsamaz** — MDP göç
etmeye başladığında uyarıyı görecek.

### WP-KNOWLEDGE ne yaptı

1. **`knowledge/` paketi açıldı** (`bm.ai-character-authoring/knowledge` 0.1.0) — 5 madde, hepsi
   `bm-measured` kaynaklı. Dış zanaat kaynağı yok: her madde bu repo veya `bm-fixture` üzerinde
   koşulmuş bir ölçüme dayanıyor. Ölçüm tablosu ve ortam bilgisi `knowledge/_sources.md`'de
   (M1–M17), görev→madde eşlemesi `knowledge/_index.md`'de.
2. **Okuma protokolü bu dosyaya girdi** (yukarıdaki "Zorunlu okuma sırası"), ADR-002 §4 uyarınca.
3. **En değerli madde koda bağlandı.** `unity-binds-by-guid-not-assembly-name`, "asmdef taşırsak
   serileştirilmiş veri kırılır" korkusunun bu repo için **yanlış** olduğunu ölçüyor — ve sınırını
   da veriyor: bağışıklık `SerializeReference`'ın sıfır olmasından geliyor, Unity'nin bir
   lütfundan değil. Sınır artık `Tests/Editor/SerializedSurfaceTests.cs` ile mekanik olarak
   tutuluyor.

Kilitlenen ölçümler: `SerializeReference` **0**, `SerializeField`/`MonoBehaviour`/`ScriptableObject`
**0/0/0**, dört üretim assembly'sinin üçü `noEngineReferences: true`. Bağlanma mekanizması da
ölçüldü: `m_Script` **`.cs.meta` GUID**'ine bağlanıyor (assembly adı zincirde yok),
`m_EditorClassIdentifier` **opsiyonel** (fixture PackageCache'inde 1335 boş / 170 dolu; *aynı*
GUID hem boş hem dolu hâlde yükleniyor), `SerializeReference` ise `RefIds[].type.asm` ile
**literal assembly adına** bağlanıyor.

### WP-KNOWLEDGE koşum kanıtı (2026-07-27, gerçekten koşuldu)

```
/Applications/Unity/Hub/Editor/6000.4.3f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -runTests -testPlatform EditMode \
  -projectPath ~/Projects/bm-fixture -testResults /tmp/wpk.xml -logFile -
```

| Koşum | Sonuç |
|---|---|
| Baseline (değişiklik öncesi, aynı makine, aynı oturum) | exit 0 · **190 test / 190 geçti / 0 kaldı** |
| WP-KNOWLEDGE sonrası | exit 0 · **204 test / 204 geçti / 0 kaldı / 0 atlandı** · `error CS` = 0 · sızan `warning CS0618` = 0 |

| Assembly | Baseline | Sonra |
|---|---:|---:|
| `BlackMountains.AICharacterAuthoring.Editor.Tests` | 34 | **46 (+12)** |
| `BlackMountains.AICharacterAuthoring.Runtime.Tests` | 26 | 26 |
| `BlackMountains.AuthoringKernel.Editor.Tests` | 13 | 13 |
| **ACA payı** | **73** | **85 (+12)** |
| `AnimationLibrary.Editor.Tests` | 41 | **43 (+2, ACA'ya ait değil)** |
| `AnimationLibrary.Runtime.Tests` + fixture | 76 | 76 |

**Regresyon yok:** mevcut 190 testin hiçbiri değişmedi. ACA artışı **tam olarak** 12 — yeni
`SerializedSurfaceTests`'in üç testi × dört üretim assembly'si.

> **+2 bana ait değil.** `AnimationLibrary.Editor.Tests` 41 → 43, iki koşum arasında komşu paketin
> deposuna **paralel bir oturumun** commit'i düştüğü için arttı
> (`HumanoidClip_ScaleWithoutARigIdentity_RefusesToProduceMillimetres`,
> `HumanoidSpace_IsDetectedEvenThoughHasMotionCurvesIsFalse`). Kırılım yazılmasaydı bu iş paketi
> kendi +12'sini +14 diye raporlardı. Gerekçe:
> `knowledge/baseline-is-measured-not-remembered`.

### Yeni koruma testi gerçekten koruyor mu — negatif kontrol koşuldu

`Editor/Planning/` altına geçici bir sonda kondu: `ScriptableObject` türeten, `[SerializeReference]`
alan taşıyan bir tip.

| Koşum | Sonuç |
|---|---|
| İhlal enjekte edilmiş | exit **2** · 204 test / 202 geçti / **2 kaldı** |
| Kalan testler | `ProductionAssembly_DeclaresNoSerializeReferenceField("BlackMountains.AICharacterAuthoring.Editor")` · `ProductionAssembly_DefinesNoUnityObjectDerivedType("BlackMountains.AICharacterAuthoring.Editor")` |
| Diğer üç assembly | **yeşil kaldı** — `noEngineReferences: true` oldukları için ihlal oralarda yazılamıyor bile |
| Sonda silindikten sonra | exit 0 · **204 / 204** |

Dikkat: yalnız `…AICharacterAuthoring.Editor` kırmızıya döndü. Bu bir eksiklik değil, ölçümün
kendisi — tek gerçek risk yüzeyi o assembly, çünkü engine'i görebilen tek üretim assembly'si o.

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
- **`AuthoringFieldValue` Newtonsoft converter'ı** — WP-20. WP-07 kaybı **ölçtü ve testle sabitledi**,
  düzeltmedi. Düzeltme bilinçli olarak WP-07'nin dışında: otorite ayrımı ile serileştirme onarımı
  ayrı incelenebilir kalmalı.
- **Eski yığının silinmesi** — MDP pinini bıraktığında. `[Obsolete]` bir silme takvimi değil,
  bir yön tabelasıdır.
- **CLI `plan`/`compile` verb'lerinin `NpcDefinition`'a bağlanması** — WP-08. `AuthoringCli.cs`
  hâlâ eski yığını çağırıyor; oradaki `#pragma warning disable 618` **bitmemiş bir göçün
  işaretidir**, onay değil.

## Sonraki iş paketi

**WP-20** — `AuthoringFieldValue` round-trip düzeltmesi (converter). WP-07'nin ölçümü bunun tek
gerçek serileştirme kırığı olduğunu gösterdi; iki ölçüm testi düzeltme geldiğinde
**ters çevrilmeli, silinmemeli**.
Ardından **WP-08** (`NpcExecutionPlanCompiler`) — `NpcAuthoringPlan` → `GenerationPlan`.
WP-08 aynı commit'te `NpcAuthoringPlannerFacade.ExecutionPlanCompilerAvailable`'ı ve
`ExecutionPlanCompiler_IsStillAbsent` testini çevirmek zorundadır.
Sıra gerekçesi: harita §3.1 madde 4 — girdi kanalı kırıkken handler yazmak, doğrulanmamış girdiyle
diske yazmaktır.
