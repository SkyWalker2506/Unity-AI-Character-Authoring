---
topicId: baseline-is-measured-not-remembered
title: Baseline hatırlanmaz, her seferinde yeniden ölçülür — çünkü test host'u senin değil
appliesTo: [iş paketi teslim kanıtı, STATE.md koşum tabloları, regresyon iddiası, CI stratejisi]
confidence: high
sourceRefs: [bm-measured]
---

# ACA'nın kendi Unity projesi yok — ve bu, test sayısını sahipsiz kılıyor

ACA bir UPM paketidir. Testleri `~/Projects/bm-fixture` test host'unda koşar ve o host **ACA'nın
kontrol etmediği başka bir paketi de** derliyor (`com.blackmountains.animation-library`) ve kendi
testlerini de taşıyor. Yani "toplam test sayısı" ACA'ya ait bir sayı değildir.

Bugün ölçülen (M14–M16):

| | `STATE.md`'de yazılı (WP-07, bir gün önce) | Bugün ölçülen |
|---|---:|---:|
| Toplam | 186 | **190** |
| `…AICharacterAuthoring.Editor.Tests` | 34 | 34 |
| `…AICharacterAuthoring.Runtime.Tests` | 26 | 26 |
| `…AuthoringKernel.Editor.Tests` | 13 | 13 |
| **ACA payı** | **73** | **73** |
| `AnimationLibrary` + fixture | 113 | **117** |

Toplam **bir günde 4 arttı** ve artışın tamamı ACA'nın dışından geldi. ACA'nın kendi payı zerre
değişmedi.

Bu WP-07'nin de yaşadığı şeydi: o gün WP-06'nın kaydettiği 143'e karşı baseline **172** ölçülmüştü.
Yani bu tek seferlik bir tuhaflık değil, **hattın kalıcı bir özelliği**.

## Drift günlerce değil, dakikalarca sürebiliyor — canlı örnek

Bu maddenin yazıldığı iş paketinde, **aynı oturum içinde**, baseline koşumu ile doğrulama koşumu
arasında `AnimationLibrary.Editor.Tests` 41'den 43'e çıktı. İki test eklenmişti:
`HumanoidClip_ScaleWithoutARigIdentity_RefusesToProduceMillimetres` ve
`HumanoidSpace_IsDetectedEvenThoughHasMotionCurvesIsFalse`. Kaynağı, komşu paketin deposuna
**paralel çalışan başka bir oturumun** araya giren commit'iydi.

Yani drift için bir gün beklemek gerekmiyor; **iki koşum arası yeter**. Bu, "önce baseline sonra
değişiklik" kuralını bir alışkanlıktan zorunluluğa çeviriyor: bu iş paketi kırılımı yazmasaydı
kendi +12'sini +14 diye raporlayacaktı.

## Bu bilgi tasarımda neyi değiştiriyor

**1. "Regresyon yok" iddiası ancak aynı oturumda ölçülen bir baseline'a karşı kurulabilir.**
Dünkü sayıyla bugünkü sayıyı karşılaştıran bir ajan, kendi yapmadığı bir değişikliği ya kendi
başarısı ya kendi hatası sanır. İkisi de yanlış rapor.

**2. Teslim kanıtı ham toplamı değil, *sahiplik kırılımını* taşımalı.** Tek bir "190" satırı hiçbir
şey kanıtlamaz. Assembly bazlı kırılım, artışın nereden geldiğini gösterir ve ACA'nın payının
sabit kaldığını **görünür** kılar. `STATE.md`'deki koşum tabloları bu yüzden assembly kırılımlı.

**3. Bir iş paketi ölçüm sırası: önce baseline, sonra değişiklik.** Değişikliği yaptıktan sonra
baseline'ı "geri alarak" ölçmek de geçerlidir (WP-07 böyle yaptı) ama **aynı makinede, aynı gün**
olmak zorunda. Farklı bir güne yayılan karşılaştırma ölçüm değil, tahmindir.

**4. Bu doğrudan CI stratejisini biçimlendiriyor.** Toplam test sayısını sabit bir eşik olarak
kilitleyen bir CI kuralı (`assert total == 190`) bu hatta **yanlış** olurdu: komşu paketin normal
gelişimi ACA'nın build'ini kırardı. Kilitlenebilecek olan toplam değil, **ACA'nın sahip olduğu üç
assembly'nin** davranışıdır.

## Sınırlar

- **Yalnız paylaşılan test host'u olan paketler için.** Kendi Unity projesi olan bir repoda toplam
  sayı gerçekten o repoya aittir ve bu madde gereksizdir.
- **Sayım tekniğine bağlı.** Kırılım NUnit XML'inin `test-suite type="Assembly"` düğümlerinden
  okundu. Farklı bir sayım (örneğin `[TestCase]` varyantlarını tek test sayan) farklı rakam
  üretir; iki koşumun **aynı yöntemle** sayılması esastır.
- **Sayı, kapsamın vekili değildir.** "73 test geçti" ile "73 testin ölçtüğü şey doğru" ayrı
  cümlelerdir. Bu madde yalnız regresyon iddiasının nasıl kurulacağı hakkındadır; testlerin
  yeterliliği hakkında hiçbir şey söylemez (bkz. `a-comparer-that-walks-nothing-finds-nothing`).
- **Bugün EditMode'a özgüdür.** ACA'nın `[UnityTest]` sayısı 0 ve her iki ACA test asmdef'i
  `includePlatforms: ["Editor"]`. PlayMode devreye girerse tablo iki eksenli olur ve bu madde
  yeniden yazılmalıdır.
- **Bir dondurma çağrısı değildir.** Komşu paketin büyümesi sağlıklıdır; madde onu engellemeye
  değil, **ACA'nın raporunu ondan bağımsız kılmaya** hizmet eder.
