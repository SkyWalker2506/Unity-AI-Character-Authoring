---
topicId: authority-lives-where-serialization-is-lossless
title: Otorite, serileştirmeyi kayıpsız geçen temsilde durur
appliesTo: [NpcDefinition, NpcAuthoringPlan, WP-08 execution plan compiler, plan store, approval token]
confidence: high
sourceRefs: [bm-measured]
---

# "Hangi tip otorite olsun" bir zevk sorusu değil, bir ölçüm sorusu

Bir authoring hattında er ya da geç şu soru gelir: *tek doğru kaynak hangi temsil?* Cevap
genellikle mimari zevkle verilir — "en zengin tip", "en merkezi tip", "en eski tip". Bu paket
cevabı **ölçerek** verdi ve ölçüm cevabı değiştirebilecek durumdaydı.

Ölçülen şey tek bir şey: **temsil kendi kalıcılık formatından geçip geri döndüğünde ne kaybediyor?**

| Ölçüm | Sonuç |
|---|---|
| `NpcDefinition` → JSON → `NpcDefinition` (maksimal fixture, 14 633 karakter) | **0 alan kaybı** |
| Katalog örnekleri: Vendor · Bandit · Companion | **0 alan kaybı** (3/3) |
| `NpcAuthoringPlan` round-trip + `DeterministicHash` korunumu | **0 alan kaybı**, hash birebir aynı |

Varsayılan Newtonsoft ayarlarıyla, hiçbir özel converter olmadan. Bu kasıtlı: kendi kalıcılık
formatından geçmek için özel converter'a **ihtiyaç duyan** bir model, o converter'ı unutan ilk
kişide authoring niyetini sessizce düşürür.

## Bu bilgi tasarımda neyi değiştiriyor

**1. `NpcDefinition` otorite ilan edilebildi.** Tasarım incelemesinin R2 riski "veri modeli ne
Unity'de ne JSON'da yaşayabiliyor" diyordu ve **hiç ölçülmemişti**. Ölçüm bu riski `NpcDefinition`
için kapattı. Karar artık varsayıma değil sayıya dayanıyor — ve sayı kaybı gösterseydi otorite
başka bir tip olurdu.

**2. WP-08 güvenle üstüne inşa edebilir.** Bir execution-plan compiler'ı, girdisi kalıcılıktan
sağ çıkmayan bir modelin üstüne yazmak, doğrulanmamış girdiyle diske yazmaktır. Sıra gerekçesi
budur.

**3. Plan kimliği kalıcılığa dayanıklı → approval token'ları anlamlı.** `NpcAuthoringPlan`
round-trip'ten sonra **aynı** `DeterministicHash`'i üretiyor. Bu olmasaydı "onaylanan plan ile
uygulanan plan aynı" cümlesi kurulamazdı: plan diske yazılıp geri okunduğunda kimliği değişen bir
sistemde onay mekanizması dekordur. WP-10'un dosya tabanlı plan store'u bu ölçüme yaslanıyor.

**4. Otorite iddiası testle kilitli, yorumla değil.** Ölçümler `Tests/Editor/NpcDefinitionAuthorityTests.cs`
içinde yaşıyor. `NpcDefinition`'a kayıplı bir alan eklendiği gün kırmızıya döner. Yani bu madde,
kendisini geçersiz kılacak değişikliği yakalayan bir alarma bağlı.

## Neden "0 kayıp" tek başına yeterli bir kanıt değil

Reflection tabanlı bir karşılaştırıcı **sessizce hiçbir şey gezmeyebilir** ve o hâlde de "0 kayıp"
raporlar. Bu yüzden bu maddenin dayanağı iki parçalı: round-trip ölçümleri **ve** ölçüm aletinin
negatif kontrolü (bkz. `a-comparer-that-walks-nothing-finds-nothing`). İkincisi olmadan yukarıdaki
tablo kanıt değil, temenni olurdu.

## Sınırlar

Bu madde **yalnız** şu koşullarda geçerlidir:

- **Varsayılan Newtonsoft ayarları.** Ölçüm `JsonConvert.SerializeObject/DeserializeObject`'in
  varsayılan davranışıyla yapıldı. `TypeNameHandling`, `NullValueHandling`,
  `DefaultValueHandling` ya da özel bir `ContractResolver` devreye girerse **tablo geçersizdir**
  ve yeniden ölçülmelidir.
- **JSON, Unity YAML değil.** Ölçülen kanal Newtonsoft JSON'dur. `NpcDefinition` bugün hiçbir
  Unity serileştirmesine girmiyor (bkz. `unity-binds-by-guid-not-assembly-name`). Biri onu bir
  `ScriptableObject` alanına koyarsa bu madde **o yol için hiçbir şey söylemez** — Unity'nin
  serileştiricisi ayrı bir mekanizmadır ve ölçülmemiştir.
- **Ölçüldüğü ağaç kadar geniş.** İddia, ölçüm anındaki `NpcDefinition` ve onun taşıdığı tiplerle
  sınırlıdır. Modele **yeni bir tip** eklendiğinde iddia otomatik olarak genişlemez; maksimal
  fixture o dalı dolduracak şekilde büyütülmedikçe yeni dal ölçülmemiş sayılır.
- **Alan seviyesinde, bayt seviyesinde değil.** Ölçülen "hiçbir alan kaybolmadı"dır; "JSON metni
  deterministik" **değildir**. Anahtar sırası veya sayı biçimlendirmesi hakkında bu madde bir şey
  söylemez — plan digest'i için kullanılan kanonik serileştirme ayrı bir mekanizmadır.
- **Tek host.** macOS arm64, Unity 6000.4.3f1, Newtonsoft 3.2.2. Platformlar arası fark beklenmiyor
  ama doğrulanmadı.

## Kardeş madde

Bu maddenin ikizi `silent-loss-is-worse-than-a-throw`: aynı ölçüm turunda **kayıp bulunan** kanal.
Bu paketin serileştirmesi "her şey kayıpsız" değildir ve öyle okunmamalıdır.
