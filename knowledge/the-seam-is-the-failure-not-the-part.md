---
topicId: the-seam-is-the-failure-not-the-part
title: Hata parçalarda değil, parçaların birleştiği yerde
appliesTo: [pipeline entegrasyonu, WP-09 prefab build, WP-12 smoke, çapraz-pipeline kapılar]
confidence: high
sourceRefs: [unity-enemies-2022-p3]
---

# Her parça kusursuz, bütün yine de yanlış

Kaynaktaki karakter beş ayrı süreçten geliyor: yüz performansı (4D volumetrik), gövde
(full-body mocap), eller ve parmaklar (ayrı ve daha yüksek hassasiyetli özel çekim), kıyafet
(kumaş simülasyonu), saç (ayrı bir simülasyon sistemi). Farklı süreçler, farklı stüdyolar,
farklı zamanlar.

Ve anlatılan başarısızlık **hiçbirinin içinde değil, aralarında**: yüz çekimi sırasında oyuncu
çok sabit durmak zorunda kalırsa, yüz performansı ile gövde performansı arasında bir
**süreksizlik** oluşuyor. Sonuç kaynağın kendi ifadesiyle *gövdesiyle hizasız yüzen bir kafa*.

Yüz mükemmel. Gövde mükemmel. Bütün uncanny.

## Dikiş yerini kimse denetlemiyor

Bizim yapımızda bu doğrudan bir boşluk. Her pipeline kendi kapısına sahip: Character
Authoring karakteri doğruluyor, Animation Library seçimi doğruluyor, Scene yerleşimi, Cinematic
kamerayı. Her biri **kendi** çıktısını ölçüyor.

Dikişi ölçen kimse yok. Ve yukarıdaki gözlem şunu söylüyor: **başarısızlığın asıl yeri orası.**

Bu, her pipeline'ın kapısını sıkılaştırarak çözülmez — beş yeşil kapı, kırmızı bir bütün
üretebilir. Ayrı bir denetim gerekiyor: bileşik çıktının kendisi.

## Karar

`WP-12` smoke testi bir "çalışıyor mu" kontrolü değil, **dikiş denetimi** olarak tasarlanmalı.
Ölçtüğü şey bileşenlerin sağlığı değil, aralarındaki hizalanma olmalı — kimin kime göre nerede
durduğu, hangi referans çerçevesinde, hangi zaman ekseninde.

Ve mimari sonuç: bileşik doğrulama **hiçbir pipeline'ın tek başına sahibi olamayacağı** bir
sorumluluktur. Bugün sahipsiz. Sahibi tanımlanmalı — yoksa herkes kendi yeşilini raporlar ve
kırmızı bütün kimsenin işi olmaz.

## İkinci ders: kaynağı bozarak aracın işini kolaylaştırma

Aynı bölümde ince bir karar var. Hizalamayı kolaylaştırmak için oyuncuyu sabit tutmak
**mümkündü** ve araç için daha temiz veri üretecekti. Bunu yapmamışlar: boyun ve kafa
hareketine izin vermişler — tıkanma ve veri hatası riskini bilerek almışlar — çünkü o
hareketler boyun dokusunun deformasyonunu belirliyor ve yasaklamak performansı **doğasından
koparıyor**.

Sonra kafayı hesaplama yoluyla nötre geri döndürmüşler.

Genellemesi: **kaynağı, aracın işini kolaylaştırmak için kısıtlama.** Kısıtlama gerçeği
bozuyorsa, zengin yakala ve sonradan düzelt. Aracın rahatlığı uğruna kaybedilen gerçek geri
gelmiyor; sonradan yapılacak düzeltme ise sadece iş.

Bu, Model Forge'daki dondurma mantığıyla aynı yöne bakıyor: kaynak hâli korunur, türetilmiş
hâl yeniden üretilebilir.

## Sınırlar

Madde, **ayrı süreçlerden gelen parçaların birleştiği** durumlar için. Tek bir süreçten çıkan
bütünsel bir çıktıda dikiş yoktur ve ek denetim israftır.

Ayrıca "dikiş denetimi" bugün bizde **tanımsız** — neyin ölçüleceği yazılmadı. Madde bir
boşluğa işaret ediyor, bir çözüm sunmuyor. Çözüm yazıldığında bu madde daralacak.
