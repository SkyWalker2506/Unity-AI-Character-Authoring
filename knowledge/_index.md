# ACA — Knowledge Pack

**packId:** `bm.ai-character-authoring/knowledge` · **version:** 0.2.0 · **kaynaklar:** `bm-measured` + `unity-enemies-2022-p3`

Repo nasıl çalışır → `docs/AI/`. Burası: *bu paket üzerinde çalışırken hangi ölçülmüş gerçekler
kararı değiştirir.*

## Statü — normatif değil

Hard gate'ler koddadır (asmdef sınırları, koruma testleri, kanonik digest) ve **kod kazanır**.
Knowledge, kodun ölçemediği ya da henüz ölçmediği yerde yön verir. Bir madde mekanik olarak
kilitlenebilecek kadar olgunlaşırsa knowledge'dan çıkar, teste girer — bu geçiş madde içinde
`→ kodlaştı:` ile işaretlenir.

Paket iki eksende konuşuyor ve ikisi ayrı sınıftan:

**Doğruluk** (`bm-measured`, ölçüm) — serileştirme kayıpsız mı, Unity neye bağlanıyor, baseline
gerçekten kaç. Bu repo ve `bm-fixture` üzerinde koşulmuş. Ölçüm tablosu → `_sources.md`.

**İnandırıcılık** (`unity-enemies-2022-p3`, stüdyo anlatımı) — bir karakter neden yanlış
hissettirir. Ölçümden düşük güvenilirlikte ve bilinçli olarak yalnız **teşhis** kısmı alındı;
kaynağın çözümü (volumetrik yakalama hattı) kapsamımız dışında.

İkisi çakışırsa **ölçüm kazanır**. Bugün çakışma yok: farklı sorulara cevap veriyorlar.

## Hangi görevde ne okunur

| Görev | Oku |
|---|---|
| Veri modeline yeni tip/alan eklemek | `authority-lives-where-serialization-is-lossless` · `silent-loss-is-worse-than-a-throw` |
| WP-08 execution-plan compiler'ı yazmak | `authority-lives-where-serialization-is-lossless` — **önce bunu oku** |
| WP-20 `AuthoringFieldValue` converter'ı | `silent-loss-is-worse-than-a-throw` — testler silinmez, ters çevrilir |
| Plan/manifest'i diske yazmak (WP-10) | `authority-lives-where-serialization-is-lossless` · `silent-loss-is-worse-than-a-throw` |
| asmdef taşımak, namespace değiştirmek, WP-52 kernel çıkarımı | `unity-binds-by-guid-not-assembly-name` — **önce bunu oku** |
| Bir tipi Unity'de serileştirmeyi düşünmek | `unity-binds-by-guid-not-assembly-name` |
| Ölçüm ya da koruma testi yazmak | `a-comparer-that-walks-nothing-finds-nothing` · `interpolated-inbetweens-are-unauthored` |
| Bir iş paketini teslim etmek, `STATE.md` koşum tablosunu yazmak | `baseline-is-measured-not-remembered` |
| CI kurmak | `baseline-is-measured-not-remembered` · `a-comparer-that-walks-nothing-finds-nothing` |
| Karakter kalite kapısı ya da rubric yazmak | `motion-not-pose-is-where-believability-breaks` · `some-features-only-exist-in-composition` |
| Aralık taşıyan bir alan eklemek, blend/LOD eşiği tasarlamak | `interpolated-inbetweens-are-unauthored` — **önce bunu oku** |
| WP-09 prefab build, WP-12 smoke, çapraz-pipeline entegrasyon | `the-seam-is-the-failure-not-the-part` — **önce bunu oku** |
| Önizleme rig'i ya da `PreviewRigSpec` tasarlamak | `some-features-only-exist-in-composition` |

## Maddeler

| topicId | Başlık | appliesTo | güven | kodlaştı |
|---|---|---|---|---|
| `authority-lives-where-serialization-is-lossless` | Otorite, serileştirmeyi kayıpsız geçen temsilde durur | `NpcDefinition`, `NpcAuthoringPlan`, WP-08, plan store | yüksek | — |
| `silent-loss-is-worse-than-a-throw` | Sessiz kayıp istisnadan tehlikelidir, kapsamı ölçülür | `AuthoringFieldValue`, `CharacterSpec`, WP-20, merge | yüksek | — |
| `unity-binds-by-guid-not-assembly-name` | GUID'e bağlanır, assembly adına değil — istisnası `SerializeReference` | asmdef taşıma, WP-52, `CONTRACTS.md` | yüksek | **evet** |
| `a-comparer-that-walks-nothing-finds-nothing` | Ölçüm aleti de ölçülür | round-trip ölçümleri, koruma testleri | yüksek | — |
| `baseline-is-measured-not-remembered` | Baseline hatırlanmaz, yeniden ölçülür | teslim kanıtı, `STATE.md`, CI | yüksek | — |
| `interpolated-inbetweens-are-unauthored` | Uçları test etmek arası hakkında hiçbir şey kanıtlamaz | blend/aralık taşıyan her sistem, test stratejisi | yüksek | — |
| `the-seam-is-the-failure-not-the-part` | Hata parçalarda değil, birleştikleri yerde | WP-09, WP-12, çapraz-pipeline kapılar | yüksek | — |
| `motion-not-pose-is-where-believability-breaks` | Tek kare, karakter için kısmi sonuçtur | `VisualEvaluation`, kalite kapıları | orta | — |
| `some-features-only-exist-in-composition` | İzole denetim bir sınıf özelliği hiç görmez | `PreviewRigSpec`, kabul kriterleri | orta | — |

## İki maddeyi birlikte okuma zorunluluğu

`authority-lives-where-serialization-is-lossless` ve `silent-loss-is-worse-than-a-throw` **aynı
ölçüm turundan** çıktı ve ayrı okunursa ikisi de yanlış anlaşılır:

- Yalnız ilki → *"bu paketin serileştirmesi kayıpsız"* — **yanlış**, ölçülmüş bir kayıp var.
- Yalnız ikincisi → *"bu paketin serileştirmesi bozuk"* — **yanlış**, otorite hattı temiz.

Doğru okuma: kayıp gerçektir, ölçülmüştür ve otorite hattının **dışındadır**. `_index.md` bu ikisini
hiçbir görevde tek başına önermez.

## Karakter kalitesinin iki ekseni

`motion-not-pose-is-where-believability-breaks` ve `some-features-only-exist-in-composition`
aynı boşluğun iki eksenidir ve birlikte okunmalıdır. Biri **zaman**, diğeri **bağlam** diyor:

|  | izole | bileşik |
|---|---|---|
| **tek kare** | bugün ölçtüğümüz yer | ölçmüyoruz |
| **dizi** | ölçmüyoruz | asıl soru burada |

Bugünkü kapılarımız sol üst hücrede duruyor ve diğer üçü hakkında sessiz. Tehlikeli olan
sessizliğin **"geçti" gibi okunması**. Bu yüzden her iki madde de aynı şeyi istiyor: sonuç
kısmi ise kısmi olduğunu söylesin.

## Ajan protokolü

`docs/AI/STATE.md` → bu dosya → ilgili maddeler → iş → teslim raporuna
`knowledgePackId` + `knowledgePackDigest` + `citedTopics[]`.

`citedTopics: []` geçerlidir — her karar bir maddeye dayanmak zorunda değil. Ama boş olması
**görünür** olur; bir kalıp haline gelirse ya knowledge o alanda eksiktir ya ajan okumamıştır.
İkisi de bilinmeye değer.
