---
topicId: some-features-only-exist-in-composition
title: Bazı özellikler ancak bileşik karede vardır
appliesTo: [önizleme, PreviewRigSpec, asset kabul kriterleri, ScenePlacementHint]
confidence: medium
sourceRefs: [unity-enemies-2022-p3]
---

# İzole bakıldığında görünmeyen, hatta anlamsız görünen özellikler

Kaynakta üç örnek var ve üçü de aynı deseni gösteriyor: karakterin **üzerinde** yazılan ama
değerini yalnız **kompozisyonda** gösteren özellikler.

**Peach fuzz** (vücudu kaplayan ince tüyler). Normalde fark etmediğimiz ama hissettiğimiz
şey. Yüzün kenarlarında çevredeki ışığı yakalıyor, karakterin kenar ışığını **genişletip
yumuşatıyor**. Kaynağın ifadesiyle bu, karakterin arka plana **oturma biçimini tamamen
değiştiriyor**.

Yani peach fuzz bir karakter özelliği gibi görünüyor ama çözdüğü problem bir **kompozisyon**
problemi — Bölüm 2'deki katman ve derinlik meselesinin ta kendisi.

**Göz–göz kapağı geçişi.** Gerçekte bu geçiş keskin değil; arada her zaman ince bir sıvı
tabakası var ve göz yüzün geri kalanıyla sürekli akıyormuş gibi duruyor. Bunu taklit etmek
için gözün etrafına normalleri harmanlayan küçük bir mesh koymuşlar. Olmadığında yapay ve
sert bir ayrım kalıyor.

**Göz ve diş çevresinde doğru gölgeleme.** Kapaklar kapanırken doğru tıkanma olmazsa gözler
kafanın parçası gibi durmuyor. Gerçek zamanlıda küçük detaylarda gölgeleme her zaman kusursuz
olamadığı için bu ayrıca ele alınmış.

## Ortak nokta ve bizim için sonucu

Üçü de **izole bir asset denetiminden geçer.** Mesh geçerli, doku geçerli, sayaçlar normal.
Üçünün de yokluğu izole denetimde **hiçbir bayrak üretmez** — ama bileşik karede hemen
görünür.

Bu, önizleme mimarimiz için doğrudan bir sonuç: bir karakter asset'ini kendi başına, nötr bir
ortamda render edip değerlendirmek, bu sınıf özelliği **görmez**. Değerlendirmenin ışıklı ve
arka planlı bir bağlamda yapılması gerekiyor.

## Karar

`PreviewRigSpec@1` bir karakter öznesi için **arka plan ve kenar ışığı** taşımalı; nötr gri
bir küre önizlemesi malzeme değerlendirmesi için doğru, karakter değerlendirmesi için yanlış.

Ve asset kabul kriterlerine bir ayrım girmeli:

```
evaluationContext:  isolated | composed
```

`isolated` geçen bir karakter, `composed` denetimden geçmiş sayılmaz. Bu,
`motion-not-pose-is-where-believability-breaks` maddesindeki kısmi-sonuç mantığının ikinci
ekseni: bir karakter için tam değerlendirme **hem hareket hem bileşim** ister.

## Sınırlar

Madde **gerçekçi insan** karakterler için ölçülmüş. Stilize karakterlerde peach fuzz ve yumuşak
kenar geçişi sanat yönüne aykırı olabilir — orada sert ayrım kasıtlıdır ve madde uygulanmamalı.

Prop ve ortam asset'leri için de geçersiz: onlarda izole denetim yeterlidir.

`confidence: medium` — etkinin varlığı anlatılıyor, büyüklüğü ölçülmemiş. Kendi hattımızda
"peach fuzz olmadan ne kadar kötü" diye bir ölçümümüz yok.
