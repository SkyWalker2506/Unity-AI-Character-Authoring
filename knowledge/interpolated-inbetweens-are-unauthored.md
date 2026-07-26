---
topicId: interpolated-inbetweens-are-unauthored
title: Aradaki değerler yazılmadı, doğrulanmadı ve hata orada
appliesTo: [blend/interpolasyon yapan her sistem, test stratejisi, NpcDefinition alan aralıkları]
confidence: high
sourceRefs: [unity-enemies-2022-p3]
---

# Uçlar doğru, arası uydurma

Kaynağın en keskin teknik tespiti bu. Standart yüz animasyonu bir **facial rig** kullanır:
uç ifadeler yazılır, aradaki her şey matematiksel enterpolasyonla üretilir.

Sorun şu: **aradaki hiçbir adım simüle edilmiş ya da oyuncunun gerçek anatomisini temsil eden
bir şey değildir.** Yüz hareket ederken oluşan karmaşıklık ve incelik enterpolasyona sığmaz.
Ortaya sentetik bir geçiş çıkar, ve insan o geçişi **fark eder**.

Kritik nokta: sistem **uçlarda başarısız olmuyor.** Uçlar yazıldı, doğru. Başarısızlık tam
olarak yazılmamış olan yerde — ortada.

## Bu bir yüz animasyonu maddesi değil, bir test ilkesi

Genellemesi şu ve bizi her yerde ilgilendiriyor:

> Uçları yazan ve arasını üreten her sistemde, **yazılan uçları test etmek arası hakkında
> hiçbir şey kanıtlamaz.**

Bu desen bizim hattımızda her yerde var: blend edilen değerler, aralık taşıyan alanlar,
kademeler arası geçişler, LOD eşikleri, kalibrasyon eğrileri. Hepsinde uçlar tanımlı, ara
üretilmiş.

Ve test yazarken doğal refleks tam olarak yanlış olanı yapıyor: sınır değerleri test ederiz
(0 ve 1, min ve max), çünkü klasik hata sınırlarda olur. Burada hata **ortada**.

## Karar

Bir alan ya da işlem aralık taşıyor ve arası üretiliyorsa, testi **yalnız uçları değil ara
noktaları da** yoklamalı. Ve "ara nokta doğru" ne demek — bunun tanımı yazılmalı; tanımı
olmayan bir ara, doğrulanamaz demektir.

Tanım yazılamıyorsa bu bilinmeye değer bir şeydir: **o aralık test edilemez** ve bu, madde
olarak kaydedilmeli, sessizce geçilmemeli.

Somut karşılığı: `NpcDefinition` üzerinde aralık taşıyan bir alan eklenirken, o alanın ara
değerlerinin ne anlama geldiği belgelenmeden eklenmemeli. "0 ile 1 arası" bir tip bildirimidir,
bir anlam bildirimi değildir.

## Çelişki kaydı yok, ama gerilim var

Bu madde `talk` sınıfı. Paketin diğer maddeleri `measured`. Çelişmiyorlar — bu madde bir
ölçümü çürütmüyor, ölçülmemiş bir alana işaret ediyor. Ölçülebilir hale geldiğinde
(bir ara-değer testi yazıldığında) sınıfı yükselir.

## Sınırlar

Enterpolasyonun **kabul edilebilir** olduğu yerler var ve madde onları yasaklamıyor:
ara değerin anlamı gerçekten doğrusalsa (bir renk rampası, bir ses seviyesi), enterpolasyon
doğru araçtır ve arası uydurma değildir.

Madde, ara değerin **fiziksel ya da anatomik bir gerçeği taklit ettiği** durumlar için:
orada doğrusal geçiş bir modelleme iddiasıdır ve genelde yanlış bir iddiadır.
