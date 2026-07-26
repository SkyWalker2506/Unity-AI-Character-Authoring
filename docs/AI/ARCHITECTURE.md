# ACA — Mimari

Tek UPM paketi: `Packages/com.blackmountains.ai-character-authoring`.
WP-06'dan itibaren paket **iki katmandır**: domain-bağımsız *authoring kernel* ve karakter domaini.

## Assembly haritası (WP-06 sonrası)

| Assembly | Klasör | Platform | Engine ref | Neye referans verir |
|---|---|---|---|---|
| `BlackMountains.AuthoringKernel` | `Runtime/Kernel/` | hepsi | **yok** (`noEngineReferences: true`) | — |
| `BlackMountains.AuthoringKernel.Editor` | `Editor/Kernel/` | Editor | **yok** (`noEngineReferences: true`) | Kernel runtime · `Unity.Nuget.Newtonsoft-Json` |
| `BlackMountains.AICharacterAuthoring.Runtime` | `Runtime/` | hepsi | yok | Kernel runtime |
| `BlackMountains.AICharacterAuthoring.Editor` | `Editor/` | Editor | var | Kernel (ikisi) · ACA runtime · Newtonsoft |
| `BlackMountains.AuthoringKernel.Editor.Tests` | `Tests/Kernel/Editor/` | Editor | var | **yalnız kernel ikilisi** |
| `BlackMountains.AICharacterAuthoring.Runtime.Tests` | `Tests/Runtime/` | Editor | var | Kernel runtime · ACA runtime |
| `BlackMountains.AICharacterAuthoring.Editor.Tests` | `Tests/Editor/` | Editor | var | Kernel (ikisi) · ACA (ikisi) |

Bağımlılık yönü **tek yönlüdür**: ACA → kernel. Ters yön yoktur ve olamaz.

```
Runtime/Kernel  <──  Runtime          (ACA domain)
      ▲                  ▲
      │                  │
Editor/Kernel   <──  Editor           (ACA domain: compiler · registry · CLI)
```

## Zorlanan kural — kernel domain görmez

`Editor/Kernel/*.asmdef` referans listesi ACA assembly'lerini **içermez**. Bir kernel dosyası bir
NPC/karakter tipine dokunmaya kalkarsa **derleme hatası** alır; asmdef'e ACA eklenirse
`Tests/Kernel/Editor/KernelBoundaryTests.cs` kırmızıya döner. İki katman:

1. `KernelAssemblyDoesNotReferenceCharacterAuthoring` — derlenmiş metadata referansları (reflection).
2. `KernelAssemblyDefinitionDeclaresNoCharacterAuthoringReference` — asmdef'te *bildirilen* referanslar
   (`UnityEditor.Compilation.CompilationPipeline`). Kullanılmayan ama bildirilmiş bir referansı da yakalar.

Ek olarak `KernelAssemblyIsEngineFree`, iki kernel assembly'sinin `UnityEngine`/`UnityEditor`'a
referans vermediğini doğrular — kernel'in taşınabilirlik iddiası budur.

Kernel test assembly'si **kasıtlı olarak** ACA'ya referans vermez: kernel'in tek başına tüketilebilir
olduğunun kanıtı, o assembly'nin derleniyor olmasıdır.

## Kernel'de ne var (12 dosya + 2 türev)

- `Runtime/Kernel/Values/` — 6 durumlu değer modeli, `CanonicalValue` ağacı, kanonik JSON + SHA-256.
- `Runtime/Kernel/Diagnostics/` — `AuthoringDiagnostic` / `DiagnosticList`.
- `Runtime/Kernel/Model/Identifiers.cs` — id grameri (`AuthoringIdentifier`) + id struct'ları.
- `Runtime/Kernel/Model/GenerationPlan.cs` — plan/operation/security level/reversibility/lifecycle.
- `Runtime/Kernel/Model/GenerationPlanHashing.cs` — deterministik operation sırası + plan digest'i
  (WP-06'da `Editor/Compilation/GenerationPlanCompiler.cs`'den çıkarıldı; domain bilmiyor).
- `Editor/Kernel/Execution/` — `IOperationHandler`, registry, `MutationScope`, `ApprovalToken`,
  preview/apply döngüsü.
- `Editor/Kernel/Recovery/` — proje kimliği + external path politikası, journal, apply lock, recovery.
- `Editor/Kernel/State/` — field schema, `NormalizedSnapshot` + hasher, ownership, manifest DTO/store.
- `Editor/Kernel/Merge/` — alan düzeyi 3-yönlü merge + drift sınıflandırması.
- `*/Kernel/Compat/` — yalnızca `[Obsolete]` ad köprüleri. Kernel ağacında "Character" kelimesinin
  geçmesine izin verilen **tek** yer burasıdır; WP-52'de silinir.

## Kernel'e girmeyecekler

`Npc*` tipleri, recipe planner, behavior/ekipman katalogları, ecosystem/narrative sözleşmeleri,
`CharacterSpec`, provider registry, CLI. Ve **preview render rig** — kernel Unity-hafif kalmalıdır
(bugün kernel'in 7 Editor dosyasının hiçbiri Unity'ye dokunmuyor).

## WP-52 (kernel paket çıkarımı) için ne yapılacak

`Runtime/Kernel/`, `Editor/Kernel/` ve `Tests/Kernel/` klasörlerini yeni `bm-authoring-kernel`
paketine taşı, `Compat/` klasörlerini sil, ACA `package.json`'una bağımlılık ekle. Refactor değil,
klasör taşıma işidir — asmdef adları ve namespace'ler zaten nötrdür.
