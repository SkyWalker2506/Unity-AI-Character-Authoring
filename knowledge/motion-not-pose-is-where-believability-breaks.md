---
topicId: motion-not-pose-is-where-believability-breaks
title: İnandırıcılık duruşta değil harekette kırılır
appliesTo: [VisualEvaluation, kalite kapıları, önizleme, WP-12 smoke]
confidence: medium
sourceRefs: [unity-enemies-2022-p3]
---

# Statik insan çözülmüş bir problem, hareketli değil

Kaynak açık bir ayrım yapıyor: **statik** dijital insanlar bugün neredeyse mükemmele
itilebiliyor ve hareketsiz bakıldığında gerçeğinden ayırt edilemiyor. **Hareket eklendiği
anda denklem değişiyor** ve uncanny valley geri geliyor.

Sebebi algısal: insan benzerliğine yaklaşıldıkça beynimiz neredeyse kusursuzluk bekliyor.
Ve hata fark edildiğinde *neden* yanlış hissettirdiğini bilmiyoruz — sadece yanlış geliyor.

## Bunun bizim kapılarımıza etkisi — kör nokta

`VisualEvaluation@1` bugün **render edilmiş bir kare** üzerinden konuşuyor. Bir kare, tanımı
gereği bir duruştur.

Yani karakter kalitesini tek kareden değerlendiren her kapı, problemin **kolay yarısını**
ölçüyor ve zor yarısına **yapısal olarak kör**. Üstelik körlüğünü bildirmiyor: statik
değerlendirme yüksek puan verip geçirir, hata ancak oynatıldığında ortaya çıkar.

Bu bir eşik ayarı sorunu değil, **kapsam sorunu**. Eşiği yükseltmek yardım etmez.

## Karar

Bir karakter değerlendirmesi, girdisinin tek kare mi yoksa dizi mi olduğunu **taşımak
zorunda**:

```
VisualEvaluation.subjectKind:  static-frame | motion-sequence
```

`static-frame` bir karakter için **kısmi sonuçtur** ve öyle raporlanmalı. "Geçti" değil,
"duruş kapısını geçti, hareket ölçülmedi" demeli. Aksi halde bir sonraki aşama ölçülmemiş
bir şeyi ölçülmüş sanar.

`subjectKind` alanı `bm-contracts`'ta zaten var; eksik olan onun **karakter öznelerinde
zorunlu** olması ve `static-frame` sonucunun kısmi olarak işaretlenmesi.

## Ne YAPMIYORUZ

Kaynak bu problemi 4D volumetrik video ile çözüyor. **Biz onu yapmıyoruz ve yakın planda
yapmayacağız.** Bu maddenin taşıdığı şey çözüm değil **teşhis**: hareket ayrı bir başarısızlık
sınıfıdır ve statik ölçüm onu görmez.

Teşhisi almak, reçeteyi almadan da değerlidir — çünkü kapılarımızın neyi ölçmediğini bilmek,
neyi ölçtüğünü bilmek kadar önemli.

## Sınırlar

Madde **insan benzeri** özneler için. Stilize karakterlerde, hayvanlarda, mekanik varlıklarda
uncanny valley aynı keskinlikte çalışmıyor — beklenti eşiği insan yüzüne özgü. Prop ve
ortam asset'leri için tamamen geçersiz; orada tek kare meşru bir ölçüm.

Ayrıca `confidence: medium`: bu bir stüdyo gözlemi, kontrollü bir algı çalışması değil.
Yönü doğru, büyüklüğü ölçülmemiş.
