---
topicId: a-comparer-that-walks-nothing-finds-nothing
title: Ölçüm aleti de ölçülür — negatif kontrolü olmayan "0 kayıp" bir sayı değildir
appliesTo: [round-trip ölçümleri, reflection tabanlı karşılaştırıcılar, koruma testleri, iş paketi teslim kanıtı]
confidence: high
sourceRefs: [bm-measured]
---

# "0 kayıp" ile "hiçbir şey gezilmedi" aynı çıktıyı verir

Bu paketin en değerli ölçümü — `NpcDefinition`'ın kayıpsız round-trip'i — reflection tabanlı bir
karşılaştırıcıyla yapıldı. Böyle bir alet **sessizce bozulur**: yanlış bir `BindingFlags`, erken
bir `return`, atlanan bir koleksiyon tipi. Bozulduğunda hata vermez; **yeşil kalır ve hiçbir şey
ölçmez.**

Failure modu şu yüzden sinsi: doğru alet ile bozuk aletin çıktısı **birebir aynıdır**. Her ikisi
de "0 kayıp" der. Aradaki farkı gösteren tek şey, aletin *başarısız olabildiğini* göstermektir.

Ölçüm (M13): `DeepCompare`'e yedi ayrı yapısal derinlikte kasıtlı kayıp enjekte edildi —
kök skaler, iç içe record decimal'i, liste elemanı, sözlük değeri, düşürülmüş sözlük anahtarı,
null'lanmış alt nesne, liste uzunluğu. **7/7 tespit edildi ve doğru yolda raporlandı.**

İki ayrıntı bu ölçümü ciddi kılıyor:

1. **Sıfır kontrolü önce koşuyor.** İki özdeş fixture karşılaştırılıyor ve **boş** sonuç
   bekleniyor. Bu olmasaydı, sonraki yedi vaka round-trip kaybını değil *inşa gürültüsünü*
   ölçüyor olabilirdi.
2. **Yalnız "kayıp bulundu" değil, "doğru yerde bulundu" iddia ediliyor.** Her vaka beklenen yol
   parçasını (`ctl.Ecosystem.Needs.InitialHunger` gibi) kontrol ediyor. Her şeye "farklı" diyen
   bir alet de 7/7 yakalardı — ve işe yaramazdı.

## Bu bilgi tasarımda neyi değiştiriyor

**1. Ölçüm testi tek başına teslim edilemez.** Bir iş paketi bir ölçümü kanıt olarak sunuyorsa,
ölçüm aletinin negatif kontrolü **aynı teslimin parçasıdır**. WP-07'de bu yapıldı; sonraki
ölçümlerde de yapılmalı.

**2. Aynı kural koruma testlerine de uygulanır.** Bir "ihlal yok" testi, taradığı kümenin boş
olmadığını da kanıtlamalıdır. WP-06 bunu asmdef sınırında yaptı: ihlal *kasten* enjekte edildi,
suite kırmızıya döndü, sonra geri alındı. `unity-binds-by-guid-not-assembly-name`'in kilidi de
aynı kalıbı taşıyor — `ResolvedAssembly_IsTheOneItClaimsToBe`, taramanın gerçekten tip gördüğünü
doğrular.

**3. Negatif kontrol tek seferlik bir el işi değil, kalıcı bir test olmalı.** Elle bir kez ihlal
enjekte edip geri almak, o günkü aleti doğrular. Aleti *yarın* koruyan şey, negatif kontrolün
suite'te yaşamasıdır. Bu, WP-06'nın elle yaptığı negatif kontrol ile WP-07'nin
`DeepCompare_DetectsInducedLossAtEveryStructuralDepth`'i arasındaki fark — ve ikincisi doğru olan.

**4. Bu, WP-07'nin manşet sonucunu kanıt sınıfına yükseltiyor.** `authority-lives-where-serialization-is-lossless`
maddesi tek başına duramaz; dayanağının yarısı bu maddedir.

## Sınırlar

- **Negatif kontrol *tespit edilebilirliği* kanıtlar, *kapsamı* değil.** Yedi derinlik gezildiğini
  gösterir; modelin **her** dalının gezildiğini göstermez. Ölçüm ancak fixture'ın doldurduğu dallar
  kadar geniştir — modele yeni bir tip eklendiğinde maksimal fixture da büyütülmelidir, yoksa yeni
  dal sessizce ölçüm dışında kalır.
- **Yalnız hataya *açık* aletler için.** Reflection, yansıma tabanlı gezinme, dinamik tarama,
  "hiçbir şey bulamazsa geç" mantığı taşıyan her ölçüm. Somut ve sabit bir assert
  (`Assert.That(hash, Is.EqualTo("d566ed…"))`) bu ilaveyi gerektirmez: yanlış hesaplarsa zaten
  kırmızı olur.
- **Maliyet gerçek.** Negatif kontrol, ölçüm testinden genelde daha uzun ve daha sıkıcıdır. Her
  assert'e uygulanacak bir kural değil; **bir teslimin kanıtı olarak sunulan** ölçümlere
  uygulanacak bir kuraldır.
- **Bu madde bir metodoloji maddesidir, bir zanaat ölçümü değil.** Bir kod yolu hakkında bilgi
  vermez; ölçümlerin nasıl sunulacağı hakkında bilgi verir. Kodlaşması beklenmez.
