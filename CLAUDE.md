# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Emby_Badges** is a C# Emby Media Server plugin that overlays visual badges on media thumbnails/posters in the library. The badges indicate:

- **Resolution** of available versions: SD, HD, FHD (1080p), 4K
- **Audio/subtitle languages** available: VO, EN, FR (configurable)
- **Multiple versions** of the same media (e.g., several files with different quality)

The plugin includes a **configuration page** (standard Emby plugin config UI) allowing users to:
- Enable/disable each badge type individually
- Set badge color and size
- Choose which languages to identify and display

## Technology Stack

- **Language:** C# / .NET 6
- **Build:** NuGet / `dotnet` CLI
- **Image rendering:** SkiaSharp (bundled with the plugin)
- **Target:** Emby Media Server plugin (`net6.0`)
- **Deployment:** Kubernetes (Rancher cluster)

## Build

Le SDK .NET 6 est installé en user-level (pas dans le PATH système) :

```bash
DOTNET="C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe"
"$DOTNET" build src/EmbyBadges/ --configuration Release --output dist/
```

Produit `dist/EmbyBadges.dll`.

## Deploy

Le serveur Emby tourne dans Kubernetes (namespace `media`, deployment `emby2`). Le kubeconfig est dans `private/kubeconfig.yml` (gitignored).

### Procédure complète

```bash
KUBECONFIG="C:/Users/cyril/Documents/VScode/GITHUB/Emby_Badges/private/kubeconfig.yml"

# 1. Récupérer le nom du pod courant
POD=$(MSYS_NO_PATHCONV=1 kubectl --kubeconfig "$KUBECONFIG" -n media get pods -l app=emby2 -o jsonpath='{.items[0].metadata.name}')

# 2. Copier le DLL (chemin source relatif obligatoire — Git Bash convertit les chemins absolus)
cd C:/Users/cyril/Documents/VScode/GITHUB/Emby_Badges
MSYS_NO_PATHCONV=1 kubectl --kubeconfig "$KUBECONFIG" -n media cp dist/EmbyBadges.dll $POD:/config/plugins/EmbyBadges/EmbyBadges.dll

# 3. Redémarrer Emby et attendre
MSYS_NO_PATHCONV=1 kubectl --kubeconfig "$KUBECONFIG" -n media rollout restart deployment/emby2
MSYS_NO_PATHCONV=1 kubectl --kubeconfig "$KUBECONFIG" -n media rollout status deployment/emby2 --timeout=60s
```

> `MSYS_NO_PATHCONV=1` est requis sur Git Bash/Windows pour éviter la conversion des chemins de destination kubectl. Toujours utiliser un chemin **relatif** pour la source du `cp`.

Le plugin est chargé depuis `/config/plugins/EmbyBadges/EmbyBadges.dll` dans le pod.


## Release CI (GitHub Actions)

Le workflow `.github/workflows/release.yml` se déclenche sur un tag `vX.Y.Z` :

```bash
git tag v1.0.0
git push origin v1.0.0
```

Build en Release avec la version extraite du tag → GitHub Release créée automatiquement avec `dist/EmbyBadges.dll`.

## Emby Plugin Architecture

### Badge rendering — `IImageEnhancer`

Badges are rendered using the official Emby `IImageEnhancer` interface (`MediaBrowser.Controller.Providers`). This is the correct, supported approach — **not** CSS/JS injection (impossible server-side) and **not** a custom HTTP interceptor.

Emby auto-discovers any class implementing `IImageEnhancer` in the plugin DLL. No manual registration needed in `Plugin.cs`.

**Key methods to implement:**

| Method | Role |
|---|---|
| `Supports(item, imageType)` | Filter which image types (Primary, Thumb) and item types (Movie, Episode…) to process |
| `EnhanceImageAsync(item, inputFile, outputFile, ...)` | Read inputFile, composite badges with SkiaSharp, write outputFile |
| `GetConfigurationCacheKey(item, imageType)` | Return a deterministic string — Emby uses this to cache/invalidate enhanced images |
| `GetEnhancedImageSize(item, imageType, index, originalSize)` | Return `originalSize` (badges drawn in-canvas, no resize) |

The enhancer pipeline is chained: each enhancer's `outputFile` becomes the next one's `inputFile`. Use `MetadataProviderPriority.Last` to run after all others.

**Badge data source:** Read resolution and language from `item` media stream properties (available via `BaseItem` API — no separate API call needed).

### Plugin registration — `Plugin.cs`

`Plugin.cs` must inherit `BasePlugin<PluginConfiguration>` and override:
- `Id` — a stable GUID (never change after first release)
- `Name` — display name in Emby UI

`PluginConfiguration` inherits `BasePluginConfiguration` and holds all user settings (badge toggles, colors, sizes, languages).

### Project structure

```
EmbyBadges.sln
libs/                                # DLLs Emby (committées pour le CI)
│   MediaBrowser.Common.dll
│   MediaBrowser.Controller.dll
│   MediaBrowser.Model.dll
dist/                                # Sortie du build (gitignorée)
src/EmbyBadges/
├── EmbyBadges.csproj
├── Plugin.cs                        # BasePlugin<PluginConfiguration>
├── PluginConfiguration.cs           # BasePluginConfiguration — user settings
├── Enhancer/
│   ├── BadgeEnhancer.cs             # IImageEnhancer — auto-découvert par Emby
│   └── BadgeDataExtractor.cs        # Extrait résolution, langues, versions multiples
├── ImageProcessing/
│   └── BadgeRenderer.cs             # Dessin SkiaSharp
└── Configuration/
    └── configPage.html              # UI de config Emby (HTML + JS ApiClient)
```

Les DLLs dans `libs/` sont référencées via `<HintPath>` avec `<Private>false</Private>` (présentes au runtime Emby, non bundlées). Copier depuis `Emby_Virtuallib/libs/` ou depuis une installation Emby locale.

### Dépendances

Les DLLs Emby sont référencées localement depuis `libs/` via `<HintPath>` avec `<Private>false</Private>` — elles sont présentes au runtime Emby et ne doivent pas être bundlées dans le DLL du plugin.

SkiaSharp (`2.88.6`) est la seule dépendance NuGet bundlée avec le plugin.

### Reference implementation

**[EmbyIcons](https://github.com/yocksers/EmbyIcons)** (MIT) is an open-source plugin that does exactly this (resolution, language, audio codec, HDR badges). Use it as the primary implementation reference.

## Repository Conventions

- **Language:** Code comments and documentation may be in French
- **Private files:** All secrets, tokens, API keys, and kubeconfigs live in `private/` — gitignored, never commit
- **YAML files:** All `.yml`/`.yaml` files are gitignored except those under `.github/`
- **Emby DLLs:** The `libs/` directory contains Emby SDK DLLs committed intentionally for CI builds without a local Emby installation — do not gitignore `libs/`
