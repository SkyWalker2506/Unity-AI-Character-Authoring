# ACA — Knowledge Pack

**packId:** `bm.ai-character-authoring/knowledge` · **version:** 0.1.0 · **kind:** tamamı `measured`

Repo nasıl çalışır → `docs/AI/`. Burası: *bu paket üzerinde çalışırken hangi ölçülmüş gerçekler
kararı değiştirir.*

## Statü — normatif değil

Hard gate'ler koddadır (asmdef sınırları, koruma testleri, kanonik digest) ve **kod kazanır**.
Knowledge, kodun ölçemediği ya da henüz ölçmediği yerde yön verir. Bir madde mekanik olarak
kilitlenebilecek kadar olgunlaşırsa knowledge'dan çıkar, teste girer — bu geçiş madde içinde
`→ kodlaştı:` ile işaretlenir.

Bu paketin tek kaynağı `bm-measured`. Dış zanaat kaynağı yok: buradaki her madde bu repo ve
`bm-fixture` test host'u üzerinde koşulmuş bir ölçüme dayanıyor. Ölçüm ortamı ve tam ölçüm
tablosu → `_sources.md`.

## Hangi görevde ne okunur

| Görev | Oku |
|---|---|
| Veri modeline yeni tip/alan eklemek | `authority-lives-where-serialization-is-lossless` · `silent-loss-is-worse-than-a-throw` |
| WP-08 execution-plan compiler'ı yazmak | `authority-lives-where-serialization-is-lossless` — **önce bunu oku** |
| WP-20 `AuthoringFieldValue` converter'ı | `silent-loss-is-worse-than-a-throw` — testler silinmez, ters çevrilir |
| Plan/manifest'i diske yazmak (WP-10) | `authority-lives-where-serialization-is-lossless` · `silent-loss-is-worse-than-a-throw` |
| asmdef taşımak, namespace değiştirmek, WP-52 kernel çıkarımı | `unity-binds-by-guid-not-assembly-name` — **önce bunu oku** |
| Bir tipi Unity'de serileştirmeyi düşünmek | `unity-binds-by-guid-not-assembly-name` |
| Ölçüm ya da koruma testi yazmak | `a-comparer-that-walks-nothing-finds-nothing` |
| Bir iş paketini teslim etmek, `STATE.md` koşum tablosunu yazmak | `baseline-is-measured-not-remembered` |
| CI kurmak | `baseline-is-measured-not-remembered` · `a-comparer-that-walks-nothing-finds-nothing` |

## Maddeler

| topicId | Başlık | appliesTo | güven | kodlaştı |
|---|---|---|---|---|
| `authority-lives-where-serialization-is-lossless` | Otorite, serileştirmeyi kayıpsız geçen temsilde durur | `NpcDefinition`, `NpcAuthoringPlan`, WP-08, plan store | yüksek | — |
| `silent-loss-is-worse-than-a-throw` | Sessiz kayıp istisnadan tehlikelidir, kapsamı ölçülür | `AuthoringFieldValue`, `CharacterSpec`, WP-20, merge | yüksek | — |
| `unity-binds-by-guid-not-assembly-name` | GUID'e bağlanır, assembly adına değil — istisnası `SerializeReference` | asmdef taşıma, WP-52, `CONTRACTS.md` | yüksek | **evet** |
| `a-comparer-that-walks-nothing-finds-nothing` | Ölçüm aleti de ölçülür | round-trip ölçümleri, koruma testleri | yüksek | — |
| `baseline-is-measured-not-remembered` | Baseline hatırlanmaz, yeniden ölçülür | teslim kanıtı, `STATE.md`, CI | yüksek | — |

## İki maddeyi birlikte okuma zorunluluğu

`authority-lives-where-serialization-is-lossless` ve `silent-loss-is-worse-than-a-throw` **aynı
ölçüm turundan** çıktı ve ayrı okunursa ikisi de yanlış anlaşılır:

- Yalnız ilki → *"bu paketin serileştirmesi kayıpsız"* — **yanlış**, ölçülmüş bir kayıp var.
- Yalnız ikincisi → *"bu paketin serileştirmesi bozuk"* — **yanlış**, otorite hattı temiz.

Doğru okuma: kayıp gerçektir, ölçülmüştür ve otorite hattının **dışındadır**. `_index.md` bu ikisini
hiçbir görevde tek başına önermez.

## Ajan protokolü

`docs/AI/STATE.md` → bu dosya → ilgili maddeler → iş → teslim raporuna
`knowledgePackId` + `knowledgePackDigest` + `citedTopics[]`.

`citedTopics: []` geçerlidir — her karar bir maddeye dayanmak zorunda değil. Ama boş olması
**görünür** olur; bir kalıp haline gelirse ya knowledge o alanda eksiktir ya ajan okumamıştır.
İkisi de bilinmeye değer.
