---
topicId: unity-binds-by-guid-not-assembly-name
title: Unity serileştirmesi GUID'e bağlanır, assembly adına değil — tek istisnası SerializeReference
appliesTo: [asmdef taşıma, WP-52 kernel paket çıkarımı, namespace göçü, CONTRACTS.md kararlılık sınıfları]
confidence: high
sourceRefs: [bm-measured]
---

# "asmdef'i taşırsak serileştirilmiş veri kırılır" — bu repo için yanlış

Bu korku her Unity projesinde vardır ve bir refactor'ı yıllarca erteleyebilir. WP-06 tam olarak
bunu yaptı (12 dosya taşındı, namespace'ler değişti, iki yeni assembly doğdu) ve WP-52 aynısını
tekrar yapacak (kernel ayrı bir pakete çıkacak). O yüzden korku ölçüldü.

## Ölçülen mekanizma — üç alan, üç farklı bağlanma biçimi

Fixture'ın kendi YAML'ından okundu:

| Alan | Neye bağlanır | Assembly adı taşır mı |
|---|---|---|
| `m_Script: {fileID: 11500000, guid: …}` | **`.cs.meta` GUID'i** | **hayır** |
| `m_EditorClassIdentifier: Assembly::Namespace.Type` | hiçbir şeye — **opsiyonel** | evet, ama bağlayıcı değil |
| `RefIds[].type: {class, ns, asm: …}` (`SerializeReference`) | **literal assembly adı** | **evet, ve bağlayıcı** |

**M10 — GUID gerçekten bağlayıcı olan anahtar.** `m_Script`'teki
`guid: de640fe3d0db1804a85f9fc8f5cadab6` arandı ve tek bir yere düştü: ilgili `.cs` dosyasının
`.meta`'sı. Bağ, dosyanın kimliği üzerinden kuruluyor. Assembly'nin adı bu zincirde **hiç yok**.

**M11 — `m_EditorClassIdentifier` bağlayıcı değil, çünkü zorunlu bile değil.** Fixture'ın
PackageCache'indeki tüm `.asset/.prefab/.unity` dosyaları tarandı: **1335 blokta alan boş,
170 blokta dolu.** Kesin kanıt bu sayılar değil, şu çift:

| Dosya | `m_Script` GUID | `m_EditorClassIdentifier` |
|---|---|---|
| Paketin kendi gönderdiği `UniversalRendererData.asset` | `de640fe3…` | **boş** |
| Editor'ün yazdığı `BmFixture_UniversalRenderer.asset` | `de640fe3…` (aynı) | dolu |

**Aynı tip, aynı GUID, iki farklı hâl — ikisi de yükleniyor** (fixture bu asset'le URP açık hâlde
190 test koşuyor). Yani alanı dolduran şey Editor'ün yeniden serileştirmesidir; yükleme onu
gerektirmez. İçinde assembly adı geçiyor olması bu yüzden yanıltıcıdır.

**M12 — asıl tehlike burada.** `SerializeReference` kullanan bir alan, sakladığı her nesnenin
tipini asset'e **`asm: <assembly adı>`** olarak yazar. Fixture'da canlı bir örneği var. Assembly
yeniden adlandırılır ya da tip başka bir assembly'ye taşınırsa o referans çözülmez — **veri
sessizce gider ve hiçbir derleyici şikâyet etmez.**

## Bu repo için ölçülen sınır

| Ölçüm | Sonuç |
|---|---|
| ACA üretim kodunda `SerializeReference` | **0** |
| `SerializeField` / `MonoBehaviour` / `ScriptableObject` | **0 / 0 / 0** |
| `noEngineReferences: true` olan üretim assembly'si | **4'ün 3'ü** |

Üçüncü satır incedir: `BlackMountains.AuthoringKernel`, `…AuthoringKernel.Editor` ve
`…AICharacterAuthoring.Runtime` engine'i **hiç göremez**, dolayısıyla `[SerializeReference]`
oralarda yazılamaz bile. Yalnız `BlackMountains.AICharacterAuthoring.Editor` görebilir — tek
gerçek risk yüzeyi odur.

Ama asıl cümle ikinci satır: bu paket **hiçbir Unity serileştirilmiş örnek verisi üretmiyor.**
Kullanıcının projesinde ACA tiplerine bağlanan bir asset yok. Kırılacak bir şey olmadığı için
asmdef taşımak veri göçü değil, **klasör taşıma işidir.**

## Bu bilgi tasarımda neyi değiştiriyor

**1. WP-52 bir migration projesi olmaktan çıktı.** Kernel'i `bm-authoring-kernel` paketine taşımak
için asset upgrade yolu, GUID remap tablosu, sürüm köprüsü **gerekmiyor**. `ARCHITECTURE.md`'nin
"refactor değil, klasör taşıma işidir" cümlesinin ölçülmüş dayanağı budur.

**2. WP-06'nın kırıcı namespace değişimi doğru fiyatlandı.** `CONTRACTS.md` §2 göç maliyetini
"dosya başına bir `using` satırı" diye yazıyor. Bu ancak serileştirilmiş veri etkilenmiyorsa
doğrudur — ve etkilenmiyor. Serileştirilmiş veri olsaydı fiyat bir `using` değil, bir veri göçü
olurdu.

**3. Bir gelecek riski adıyla kaydedildi.** Bu bağışıklık bir Unity özelliği değil, **bu paketin
bir özelliğidir** ve tek bir commit'le kaybedilebilir. Birinin `…AICharacterAuthoring.Editor`'a bir
`ScriptableObject` + `[SerializeReference]` alan eklediği gün, WP-52 sessizce veri göçü gerektiren
bir işe döner — ve bunu fark ettiren hiçbir şey olmazdı.

O yüzden madde bir teste bağlandı:

→ kodlaştı: `Tests/Editor/SerializedSurfaceTests.cs` —
`ProductionAssembly_DeclaresNoSerializeReferenceField` ve
`ProductionAssembly_DefinesNoUnityObjectDerivedType`, dört üretim assembly'sinin **hepsi** için.
Üçüncü bir test (`ResolvedAssembly_IsTheOneItClaimsToBe`) taramanın gerçekten bir şey gezdiğini
doğrular — hiçbir tip bulamayan bir tarama da "0 ihlal" raporlar.

**4. Karar kuralı basit hâle geldi.** "Bu tip Unity'de serileştirilmeli mi?" sorusunun bedeli artık
belli: evet demek, WP-52'ye bir veri göçü yükü eklemektir. Bu bedel bilinerek ödenebilir; bilmeden
ödenmemeli. Test, kararın bilinçli olmasını zorunlu kılar.

## Sınırlar

- **İddia bu paketin *ürettiği* veri hakkındadır, tükettiği hakkında değil.** ACA bir gün bir
  `ScriptableObject` **okumaya** başlarsa (örneğin WP-21 prefabdan snapshot üretirken), okuduğu
  asset'in kendi `SerializeReference` grafiği olabilir. Bu madde o asset hakkında hiçbir şey
  söylemez — koruma yalnız ACA'nın kendi tiplerini kapsar.
- **`m_EditorClassIdentifier`'ın *opsiyonel* olduğu ölçüldü; *bozulduğunda ne olduğu* ölçülmedi.**
  Alan bağlayıcı değil çünkü boşken de yükleniyor. Kasten yanlış doldurulmuş bir değerin
  davranışı denenmedi ve bu madde onun hakkında konuşmuyor.
- **`SerializeReference` yasağı bir tercih değil, bir ödünleşme.** Polimorfik alan grafikleri
  gerçek bir Unity ihtiyacıdır. Madde "asla kullanmayın" demiyor; **"kullanırsanız assembly
  adları donmuş sayılır"** diyor. Bu paket assembly'lerini henüz dondurmadığı için bugünkü cevap
  hayır.
- **Ölçüm ortamı:** Unity 6000.4.3f1, macOS arm64, `bm-fixture` test host'u. YAML alan gramerinin
  başka bir Unity majör sürümünde aynı kaldığı **varsayılmıştır, ölçülmemiştir**.
- **`.meta` dosyalarının kendisi hâlâ kritiktir.** Bağ GUID üzerinden kuruluyorsa, bir `.cs.meta`
  silinip yeniden üretildiğinde GUID değişir ve bağ kopar. GUID'e bağlanmak asmdef taşımasına
  bağışıklık verir, **`.meta` kaybına değil.**
