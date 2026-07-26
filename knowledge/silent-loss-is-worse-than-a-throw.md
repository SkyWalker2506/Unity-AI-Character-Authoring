---
topicId: silent-loss-is-worse-than-a-throw
title: Sessiz kayıp, istisnadan tehlikelidir — ve kapsamı ölçülür
appliesTo: [AuthoringFieldValue, CharacterSpec, WP-20 converter, 6 durumlu değer modeli, merge motoru]
confidence: high
sourceRefs: [bm-measured]
---

# Kayıp var, ama otorite kararını etkilemiyor — ikisi birden doğru

Aynı ölçüm turu ki `NpcDefinition`'ın kayıpsız olduğunu gösterdi, bir kayıp da buldu:

| Ölçüm | Sonuç |
|---|---|
| `AuthoringFieldValue.Known(CanonicalValue.String(...))` → JSON → geri | `State: Known → Unspecified`, `Value: → null` |
| `CharacterSpec.Parameters["height"]` (aynı kanalın gerçek kullanımı) | `State: → Unspecified` |
| `NpcDefinition` / `NpcAuthoringPlan` kaynağında `AuthoringFieldValue` geçişi | **0** |

Üç satır birlikte okunmalı. Ayrı ayrı okunursa iki farklı yanlış çıkar:

- Yalnız ilk iki satır: *"bu paketin serileştirmesi bozuk"* → yanlış, otorite hattı temiz.
- Yalnız üçüncü satır: *"her şey kayıpsız"* → yanlış, kayıp gerçek ve yaşıyor.

Doğru cümle şu: **kayıp gerçektir, ölçülmüştür, ve otorite hattının dışındadır.** Bu yüzden
otorite kararı (`NpcDefinition`) kayıptan etkilenmedi — ama kayıp yok sayılmadı, borç olarak
kaydedildi (WP-20).

## Neden bu kayıp özellikle kötü bir kayıp

`AuthoringFieldValue` bir `readonly struct`; private constructor, get-only property, converter
yok. Newtonsoft onu yeniden inşa edemiyor. Kritik nokta bu değil, **ne yaptığı**:

> Newtonsoft **istisna atmıyor.** `Unspecified` üretiyor ve devam ediyor.

`Unspecified` bu modelde geçerli, anlamlı bir durumdur — "bilinçli olarak belirtilmedi" demektir.
Yani kayıp, sistemin normal kelime dağarcığındaki bir değere dönüşüyor. Aşağı akıştaki hiçbir kod
"bu değer kayboldu" ile "bu değer kasten boş bırakıldı" arasındaki farkı göremez.

6 durumlu değer modelinin (`Unspecified · Absent · Null · Unknown · Computed · Known`) **bütün
varlık sebebi** tam olarak bu ayrımı korumaktı. Serileştirme onu düşürüyor. Yani kayıp yalnız bir
alanı değil, modelin kendi tezini vuruyor.

## Bu bilgi tasarımda neyi değiştiriyor

**1. Otorite seçimi bu ölçümle *desteklendi*, kayıpla *engellenmedi*.** `CharacterSpec` otorite
olamaz çünkü tek parametre kanalı kalıcılıktan sağ çıkmıyor. `NpcDefinition` olabilir çünkü bu
tipe hiç dokunmuyor. Karar kayıp ölçüldüğü için verilebildi.

**2. Kayıp bir teste çevrildi, bir TODO'ya değil.** İki test kaybı **kasten iddia ediyor**
(`Measured_AuthoringFieldValueDoesNotSurvivePlainNewtonsoftRoundTrip`,
`Measured_CharacterSpecParametersChannelIsLostOnRoundTrip`). WP-20 converter'ı eklediğinde bu
testler **silinmez, ters çevrilir** — assert mesajları bunu okuyana söylüyor. Böylece "ne zaman
kırıktı, ne zaman düzeldi" tarihi kodda kalır.

**3. Onarım, otorite ayrımından ayrı bir iş paketi.** İkisini aynı commit'e koymak, iki ayrı
kararın birbirini gizlemesine yol açardı: serileştirme onarımı ile otorite seçimi bağımsız
incelenebilir kalmalı.

**4. Merge ve snapshot tarafı uyarılmış olur.** `AuthoringFieldValue`, merge motorunun ve field
schema'sının da para birimidir. Bir gün `NormalizedSnapshot` diske yazılırsa (WP-10/WP-21) bu kayıp
oraya taşınır. Converter WP-20'de gelmezse, disk formatı ilk günden yalan söyler.

## Sınırlar

- **Kanal JSON'dur.** Ölçüm varsayılan Newtonsoft ayarlarıyla yapıldı. Özel bir converter,
  `[JsonConstructor]` ya da bir `ContractResolver` eklendiği an bu madde geçersizdir — zaten
  WP-20'nin işi tam olarak budur.
- **Kayıp yönü tek taraflı ölçüldü.** Ölçülen `Known → Unspecified` geçişidir. Diğer beş durumun
  (`Absent`, `Null`, `Unknown`, `Computed`) round-trip davranışı **ayrı ayrı ölçülmedi**.
  Muhtemelen hepsi `Unspecified`'a düşüyor ama bu **varsayımdır**, ölçüm değildir.
- **"Otorite hattı temiz" iddiası bugünün grafiğine bağlıdır.** M6 ölçümü, `NpcDefinition` ve
  `NpcAuthoringPlan` kaynak dosyalarında `AuthoringFieldValue`'nun geçmediğini gösteriyor.
  Bu, kaynak metin düzeyinde bir ölçümdür; **derleme zamanı bir kısıt değildir**. Biri yarın
  `NpcDefinition`'a bir `AuthoringFieldValue` alanı eklerse round-trip testi kırmızıya döner
  (asıl koruma budur), ama M6'nın kendisi bunu engellemez.
- **`CharacterSpec` `[Obsolete]`, silinmiş değil.** Tüketici (MDP) bu paketi commit ile pinliyor.
  Yani kayıp bugün **canlı kodda duruyor** ve bu madde bir tarihçe değil, güncel bir uyarıdır.
- **WP-20 sonrası bu madde yeniden yazılmalıdır**, arşivlenmemeli: kaybın *ne zaman* ve *neden*
  var olduğu, converter'ın hangi davranışı üretmesi gerektiğinin gerekçesidir.
