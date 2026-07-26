# Kaynaklar

Her knowledge maddesi buradaki bir `sourceId`'ye dayanır. Kaynaksız madde kabul edilmez.

Bu pakette **iki kaynak** var, farklı sınıflardan ve farklı işler için.

`bm-measured` — bu paketin kendi kodu üzerinde koşulmuş ölçümler. Hiyerarşide (`measured` >
`docs`/`paper` > `talk`/`practice`) en yüksek sınıf, ama aynı zamanda **en dar**: bir ölçüm
yalnız ölçüldüğü konfigürasyon için konuşur. Her maddenin "Sınırlar" bölümü bu darlığı yazmakla
yükümlüdür.

`unity-enemies-2022-p3` — dijital insan üretimi üzerine bir stüdyo anlatımı. `talk` sınıfı,
yani ölçümden düşük. Bu kaynaktan alınan maddeler bize bir **çözüm** değil bir **teşhis**
veriyor; reçetesi (4D volumetrik yakalama, özel tarama stüdyoları) kapsamımızın dışında ve
alınmadı.

**Çelişki kaydı:** bugün çelişki yok. İki kaynak farklı alanlarda konuşuyor — `bm-measured`
serileştirme ve bağlanma hakkında, `unity-enemies-2022-p3` algı ve kompozisyon hakkında.
Çakışırlarsa **ölçüm kazanır** ve madde dosyası ikisini de yazar.

---

## `bm-measured`

| | |
|---|---|
| **sourceId** | `bm-measured` |
| **kind** | `measured` |
| **title** | ACA'nın kendi ölçümleri (bu paket + `bm-fixture` test host'u) |
| **retrievedAt** | 2026-07-27 |
| **reliability** | en yüksek |

### Ölçüm ortamı — bir iddia bunsuz taşınamaz

| | |
|---|---|
| Host | macOS 25.5.0, arm64 |
| Unity | 6000.4.3f1 |
| Test host | `~/Projects/bm-fixture` (ACA'nın kendi Unity projesi yok) |
| Newtonsoft | `com.unity.nuget.newtonsoft-json` 3.2.2, **varsayılan ayarlar** |
| Koşum | `Unity -batchmode -runTests -testPlatform EditMode` |

Bu satırlar süs değil. Aşağıdaki serileştirme ölçümlerinin tamamı **varsayılan Newtonsoft
ayarlarıyla** yapıldı; özel bir `JsonSerializerSettings` ile sonuçlar farklı olurdu ve o hâlde
bu tablo geçersizdir.

### Ölçülenler

| # | Ölçüm | Sonuç | Nasıl ölçüldü | Madde |
|---|---|---|---|---|
| M1 | `NpcDefinition` → JSON → `NpcDefinition` (maksimal fixture) | **0 alan kaybı**, JSON 14 633 karakter | `NpcDefinition_SurvivesPlainNewtonsoftRoundTrip`, reflection ile alan-alan `DeepCompare` | `authority-lives-where-serialization-is-lossless` |
| M2 | Katalog örnekleri (Vendor · Bandit · Companion) | **0 alan kaybı** (3/3) | `NpcDefinition_CatalogSamples_SurvivePlainNewtonsoftRoundTrip` | aynı |
| M3 | `NpcAuthoringPlan` round-trip + `DeterministicHash` | **0 alan kaybı**, hash birebir aynı | `NpcAuthoringPlan_SurvivesPlainNewtonsoftRoundTrip` | aynı |
| M4 | `AuthoringFieldValue.Known(...)` round-trip | `State=Known → Unspecified`, `Value=null`, **istisna yok** | `Measured_AuthoringFieldValueDoesNotSurvivePlainNewtonsoftRoundTrip`; koşum çıktısı: `deserialized to State=Unspecified, Value=<null>` | `silent-loss-is-worse-than-a-throw` |
| M5 | `CharacterSpec.Parameters["height"]` round-trip | `State=Unspecified` (değer yok oldu) | `Measured_CharacterSpecParametersChannelIsLostOnRoundTrip` | aynı |
| M6 | `NpcDefinition`/`NpcAuthoringPlan` kaynak dosyalarında `AuthoringFieldValue` geçişi | **0** (`NpcAuthoringContracts.cs` 0, `NpcAuthoringPlanning.cs` 0) | `grep -c` iki dosyada | aynı |
| M7 | ACA üretim kodunda `SerializeReference` | **0** | `grep -rn "SerializeReference" --include="*.cs"` paket ağacında | `unity-binds-by-guid-not-assembly-name` |
| M8 | ACA'da `SerializeField` / `MonoBehaviour` / `ScriptableObject` | **0 / 0 / 0** | aynı yöntem | aynı |
| M9 | `noEngineReferences: true` olan üretim assembly'si | **4'ün 3'ü** (yalnız `…AICharacterAuthoring.Editor` engine görebiliyor) | dört asmdef okundu | aynı |
| M10 | `m_Script`'in bağlandığı anahtar | `.cs.meta` **GUID**'i — `guid: de640fe3d0db1804a85f9fc8f5cadab6` → `UniversalRendererData.cs.meta` | fixture YAML'ı + aynı GUID'i taşıyan `.meta` dosyası arandı | aynı |
| M11 | `m_EditorClassIdentifier` zorunlu mu | **hayır, opsiyonel** — fixture PackageCache'inde `.asset/.prefab/.unity` içinde **1335 boş / 170 dolu**; *aynı* GUID hem boş hem dolu hâlde görüldü | tüm YAML tarandı; `UniversalRendererData.asset` (boş) ile `BmFixture_UniversalRenderer.asset` (dolu) aynı `m_Script` GUID'ini taşıyor, ikisi de yükleniyor | aynı |
| M12 | `SerializeReference`'ın bağlandığı anahtar | **literal assembly adı** — `RefIds[].type.{class, ns, asm}` | fixture'daki `references:` bloğu okundu (`asm: Unity.RenderPipelines.Universal.Runtime`) | aynı |
| M13 | `DeepCompare` negatif kontrolü | **7/7** kasıtlı kayıp tespit edildi ve doğru yolda raporlandı | `DeepCompare_DetectsInducedLossAtEveryStructuralDepth` — kök skaler, iç içe record decimal'i, liste elemanı, sözlük değeri, düşürülmüş sözlük anahtarı, null'lanmış alt nesne, liste uzunluğu | `a-comparer-that-walks-nothing-finds-nothing` |
| M14 | Test host toplamı (2026-07-27) | **190 test / 190 geçti / 0 kaldı** | tam suite koşumu, NUnit XML'i sayıldı | `baseline-is-measured-not-remembered` |
| M15 | Toplamın ACA'ya ait payı | **73** (34 Editor + 26 Runtime + 13 Kernel) | aynı XML, assembly kırılımı | aynı |
| M16 | `STATE.md`'de yazılı WP-07 baseline'ı ile bugünkü fark | yazılı **186** → ölçülen **190**; farkın **tamamı** ACA dışından (`AnimationLibrary` + fixture 113 → 117), ACA payı **değişmedi** | iki kırılım tablosu karşılaştırıldı | aynı |
| M17 | Aynı oturum içinde, iki koşum arası drift | `AnimationLibrary.Editor.Tests` **41 → 43**; iki yeni test, komşu paketin deposuna paralel bir oturumun commit'inden | iki NUnit XML'inin test-adı kümeleri farklandı; komşu repo `git log` + dosya mtime ile doğrulandı | aynı |

### Ölçülmeyenler — bilerek boş

Bunlar "henüz yapılmadı" değil, **"bu paket bu iddiayı taşımıyor"** anlamına gelir:

| Ölçülmedi | Neden önemli |
|---|---|
| Windows / Linux host | Serileştirme ölçümleri tek host'ta yapıldı. Newtonsoft davranışı platformdan bağımsız beklenir ama **doğrulanmadı**. |
| PlayMode | ACA'nın `[UnityTest]` sayısı 0; her iki ACA test asmdef'i `includePlatforms: ["Editor"]`. |
| Özel `JsonSerializerSettings` altında round-trip | Tüm M1–M5 varsayılan ayarlarla. `TypeNameHandling` veya `NullValueHandling` değişirse tablo geçersizdir. |
| `NpcDefinition`'ın Unity YAML'ına yazılması | Böyle bir yol yok (M8). Bir gün olursa M7–M12 yeniden ölçülmelidir. |
| `m_EditorClassIdentifier` bozulunca ne olduğu | M11 alanın **opsiyonel** olduğunu gösteriyor; *kasten bozulmuş* bir değerin davranışı denenmedi. |

---

## `unity-enemies-2022-p3`

| | |
|---|---|
| **sourceId** | `unity-enemies-2022-p3` |
| **kind** | `talk` |
| **title** | Unity Demo Team — "Enemies" yapım serisi, Bölüm 3: gerçekçi insan karakterler |
| **retrievedAt** | 2026-07-27 (kullanıcı tarafından transkript olarak sağlandı) |
| **reliability** | orta |

### Neden orta, ve neden yine de alındı

Bu bir stüdyo anlatımı: tek bir yapım, tek bir tür (gerçekçi, oturan, konuşan tek karakter),
kontrollü bir çalışma değil. Anlatılan etkilerin **yönü** güvenilir, **büyüklüğü** ölçülmemiş.
Bu yüzden bu kaynaktan gelen dört maddenin ikisi `medium`, ikisi `high` — `high` olanlar bir
algı iddiası değil, bir **yapısal** iddia taşıdığı için (enterpolasyon ara değerleri yazılmaz;
ayrı süreçler dikiş üretir).

Alınma sebebi: bu paketin geri kalanı serileştirme ve bağlanma hakkında konuşuyor, yani
**doğruluk**. Bu kaynak **inandırıcılık** hakkında konuşuyor ve o eksen hiç temsil edilmiyordu.

### Ne alındı, ne alınmadı

| Alındı — teşhis | Alınmadı — reçete |
|---|---|
| Hareket ayrı bir başarısızlık sınıfıdır; statik ölçüm görmez | 4D volumetrik video yakalama |
| Enterpolasyon ara değerleri yazılmamış ve doğrulanmamıştır | Facial rig / FACS / wrinkle map üretimi |
| Ayrı süreçlerden gelen parçalar dikişte başarısız olur | Cilt, göz, saç shader'larının kendisi |
| Bazı özellikler yalnız bileşik karede var olur | Tarama, topoloji transferi, kumaş ve mocap tedarikçileri |

Reçete kısmı kapsamımız dışında: özel tarama stüdyoları, topoloji transfer araçları ve
volumetrik yakalama hattı bizim kurmadığımız ve kurmayacağımız şeyler. **Teşhisi almak,
reçeteyi almadan da değerlidir** — kapılarımızın neyi ölçmediğini bilmek için.

Satıcı ve araç adları bilinçli olarak madde metinlerine taşınmadı: bu repo public ve bir
tedarikçi listesi burada bir bağımlılık iddiası gibi okunurdu.

### Bu kaynağın bir ölçümü doğruladığı yer

Cilt albedo'sunun **nötr ışıkta** yakalandığı, yani ham renk elde etmek için aydınlatmanın
dışarıda bırakıldığı anlatılıyor. Bu, `bm-model-forge/knowledge/baked-in-is-lost-control.md`
maddesinin bağımsız bir teyidi: pişmiş ışık kontrolü geri alınamaz biçimde kaybettirir.
İki paket aynı sonuca farklı yollardan geldi.
