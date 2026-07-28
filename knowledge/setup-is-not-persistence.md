---
topicId: setup-is-not-persistence
title: Kurulum dogrulamak yetmez; kaliciligi dogrula
appliesTo: [SceneAuthoring, AssetAuthoring, CaptureHandoff]
confidence: high
sourceRefs: [bm-measured]
---

# Bellekte dogru olan diske gecmeyebilir, ve bu hicbir yerde hata uretmez

Kuran surec ile tuketen surec ayri iki Unity surecidir. Aralarindaki tek kopru
**dosyadir**: sahne ve varliklar. Kurulum kodu dogru calisip, log dogru yazip,
sonuc yine kaybolabilir — cunku serilestirme arada sessizce basarisiz olur.

Bu bir kenar durum degil; ayni ay icinde **iki** farkli mekanizmayla olculdu.

## Olcum 1 — bilesen sahneye hic yazilmadi

Bir prop custody bileseni (`PropCustody`) kuruldu. Kurulum logu iki devri de
yazdi:

```
[Stage] prop/lantern @ 0.0s  -> cast/father/hand_R (hand_r)
[Stage] prop/lantern @ 16.5s -> cast/child/hand_R  (hand_r)
```

Sahne kaydedildi. Ceken surec sahneyi acti ve:

```
[HeadlessRecord] PropCustody yok — fener sabit kalacak
```

Kaydedilmis `.unity` dosyasinda o script guid'ine bagli MonoBehaviour sayisi
olculdu: **2** — ikisi de baska bir siniftı. Bilesen serilestirilmemisti.

Sebep: **Unity bir `.cs` dosyasi icin TEK `MonoScript` uretir.** Ayni dosyadaki
ikinci MonoBehaviour'un script referansi hic olusmaz. `AddComponent<T>()`
bellekte calisir, sahne kaydedilir, bilesen yok olur. Derleme temiz, exit 0,
hicbir uyari yok.

## Olcum 2 — varligin alt-nesneleri diske hic yazilmadi

Bir `VolumeProfile` dokuz override ile kuruldu ve kaydedildi. Log:

```
[Grade] Volume kuruldu: 9 override -> Assets/Generated/vs0/NightForest_Grade.asset
```

Diskteki dosya **635 bayt** ve icerigi:

```yaml
  components:
  - {fileID: 0}
  - {fileID: 0}
  ... (dokuz kez)
```

Dokuz NULL. `VolumeProfile.Add<T>()` bileseni bellekte olusturur; `AssetDatabase
.AddObjectToAsset` cagrilmadikca **alt-varlik olarak yazilmaz**. Ayni oturumda
her sey dogru gorunur, cunku referanslar bellekte yasar. Ceken surec profili
DISKTEN okur ve hicbir override bulamaz.

Sonucu: tonemapping, pozlama, bloom, vignette, grain — post zincirinin tamami
**hicbir kayitta hic var olmadi**. Notlarda "post zinciri 13 kusuru cozdu"
yaziyordu; o hukum yanlisti ve bir ay boyunca dogru sanildi.

Olcum tarihi: 2026-07-28 · Unity 6000.4.3f1 · URP 17.4.0

## Kural

Dogrulama **kurulumu** degil **kaydedilmis sonucu** okur:

- Sahne icin: kaydettikten sonra `EditorSceneManager.OpenScene` ile YENIDEN AC
  ve tuketicinin gorecegini say (kac bilesen, kac referans saglam).
- Varlik icin: `AssetDatabase.SaveAssets()` + `ImportAsset(..., ForceUpdate)`,
  sonra `LoadAllAssetsAtPath` ile alt-varliklari say ve null ara.
- Her iki durumda **sayilar loglanir**. "Kuruldu" cumlesi kanit degildir.

Bunun bedeli bir kez odenir (yeniden acmak sahnedeki referanslari gecersiz
kilar, o yuzden en sonda yapilir); odenmedigi her seferde hata bulunmasi
gununu bulur.

## Iliskili

- `silent-loss-is-worse-than-a-throw` — sessiz kayip, atilan hatadan kotudur
- `authority-lives-where-serialization-is-lossless` — yetkinin nerede durdugu
- `bm-model-forge/knowledge/count-the-intervention-not-only-the-result`
