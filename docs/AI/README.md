# docs/AI — makine-okur repo rehberi

Bu klasör **AI ajanları ve yeni gelen geliştiriciler** içindir. Amaç: bu repoyu hiç bilmeyen bir
okuyucunun, başka hiçbir kaynağa ihtiyaç duymadan *ne olduğunu, nasıl çalıştığını ve neye
dokunmaması gerektiğini* anlaması.

| Dosya | Ne zaman okunur | Kim günceller |
|---|---|---|
| `ARCHITECTURE.md` | Repoya ilk temasta. Assembly sınırları nerede, ne neye bağlı. | Mimari değişince |
| `STATE.md` | Her göreve başlamadan. Neyin bitti, neyin açık, neyin kasıtlı eksik olduğu. | **Her iş paketi tesliminde (zorunlu)** |
| `CONTRACTS.md` | Dışarıdan bu repoyu tüketirken. Public yüzeyler ve kararlılık garantileri. | Public API değişince |

Bu klasör **repo nasıl çalışır** sorusunu cevaplar. **İş nasıl iyi yapılır** ayrı bir yerdedir:

| Klasör | Ne zaman okunur | Kim günceller |
|---|---|---|
| `../../knowledge/` | `STATE.md`'den **hemen sonra**. Önce `_index.md`, sonra yalnız göreve uyan maddeler. | Yeni bir ölçüm madde adayı ürettiğinde |

Gerekçe: ADR-002 (`bm-contracts/docs/AI/ADR-002-knowledge-layer.md`). Okuma sırasının tamamı ve
teslim raporunun taşıması gerekenler `STATE.md` → "Zorunlu okuma sırası" bölümünde.

## Ajanlar için kurallar

1. Görev almadan önce `STATE.md`'yi oku. "Kasıtlı eksik" bölümündeki bir şeyi "eksik" diye tamamlama.
2. Ardından `knowledge/_index.md`'yi oku ve göreve uyan maddeleri aç. Hepsini okuma.
3. Kod değiştirdiysen `STATE.md`'yi **aynı commit'te** güncelle. Dokümansız teslim reddedilir.
4. Bir hükmü kanıtsız yazma. Dosya yolu + tip/üye ver.
5. Testi çalıştırmadan "çalışıyor" yazma.
6. Teslim raporuna `knowledgePackId` + `knowledgePackDigest` + `citedTopics[]` yaz.

## Bu reponun Unity projesi YOK

ACA bir UPM paketidir; kendi Unity projesi yoktur. Testler **`~/Projects/bm-fixture`** test
host'unda koşar (`Packages/manifest.json` → `testables`). Koşum komutu `STATE.md` içindedir.
