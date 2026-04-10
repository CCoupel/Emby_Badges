# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Emby_Badges** is a C# Emby Media Server plugin that overlays visual badges on media thumbnails/posters. The badges display:

- **Resolution** of all available versions: SD (480p), HD (720p), Full HD (1080p), 4K
- **Audio language**: French, English, VO (audio present but unknown language)
- **Multi-version / VirtualLib**: connector initial letter (or "MULTI"), with configurable trigger
- **Favorites**: heart icon when the item is marked as favorite by the first user

Each badge group (Resolution, Language, MultiVersion, Favorites) has independent position, size, margin, and opacity settings. Individual badges within a group share those settings but can be individually enabled/disabled.

## Technology Stack

- **Language:** C# / .NET 6
- **Build:** `dotnet` CLI (user-level install, not in PATH)
- **Image rendering:** SkiaSharp 2.88.6 (bundled)
- **Target:** Emby Media Server plugin (`net6.0`)
- **Deployment:** Kubernetes (Rancher cluster, namespace `media`, deployment `emby2`)

## Build

```bash
"C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe" build \
  src/EmbyBadges/EmbyBadges.csproj \
  --configuration Release \
  --output dist/
```

Produces `dist/EmbyBadges.dll`. Run `/build` for the full build + deploy sequence.

### Config page generation

`configPage.html` and `configScript.js` are generated from `gen_config_page.py` (which inlines icon PNGs as base64):

```bash
python gen_config_page.py
```

Must be run before building when the config page changes.

## Deploy

See `.claude/commands/build.md` for the full deploy sequence with all critical rules. Key points:

- **Kubeconfig**: `export KUBECONFIG=private/kubeconfig.yml` (gitignored, required)
- **Destination path**: `/config/plugins/EmbyBadges.dll` — root of plugins folder, **not** a subdirectory
- **`MSYS_NO_PATHCONV=1`**: required on Git Bash/Windows for all kubectl commands with absolute Linux paths
- **Source path**: relative (`dist/EmbyBadges.dll`), not absolute
- Copy **before** restart; verify DLL size in the **new** pod after restart
- **Never deploy to `emby`** (production) — only `emby2`

## Release

```bash
git tag v1.0.0
git push origin v1.0.0
```

The `.github/workflows/release.yml` CI triggers on `vX.Y.Z` tags, builds in Release with version from tag, and creates a GitHub Release with `dist/EmbyBadges.dll`.

## Emby Plugin Architecture

### Badge rendering — `IImageEnhancer`

`BadgeEnhancer` implements `IImageEnhancer` (auto-discovered by Emby, no manual registration needed). Key contract:

| Method | Role |
|---|---|
| `Supports(item, imageType)` | Primary/Thumb images, Movie/Episode items only |
| `GetConfigurationCacheKey(item, imageType)` | Must include ALL state affecting rendering — Emby caches images globally (not per-user) |
| `EnhanceImageAsync(...)` | Read inputFile → composite badges via SkiaSharp → write outputFile |
| `GetEnhancedImageSize(...)` | Returns `originalSize` (badges drawn in-canvas, no resize) |

**Cache key completeness is critical.** Missing any field (e.g. `IsFavorite`, `IsFromVirtualLib`) causes stale cached images. The cache key includes: all `GroupConfig` fields, all `ShowXxx` flags, `MultiVersionTrigger`, `ResolutionIcons`, `AudioLanguages`, `VersionConnectors`, `IsFromVirtualLib`, `IsFavorite`, `DebugMode`, TMDB key presence.

**Favorites limitation:** Images are cached globally — true per-user favorites badges aren't feasible. The plugin checks only `userManager.Users.FirstOrDefault()` (the first/admin user).

### Config page — Emby HTML fragment

The config page is an HTML **fragment** (no `<html>/<head>/<body>`), with root `<div is="emby-scroller" data-controller="__plugin/EmbyBadgesConfigScript">`. JS is a separate AMD module: `define([], function(){ return function(view){ ... }; })`. Always use `view.querySelector` (scoped to fragment), not `document.querySelector`.

### VirtualLib integration

When VirtualLib plugin is installed, `BadgeDataExtractor` reads `{PluginConfigurationsPath}/VirtualLib.xml` to get `<VirtualLibraryRootPath>`. Media paths under this root follow `{root}/{ConnectorDisplayName}/...`. The connector name is extracted from the first path segment after root and used as the badge label initial.

### Badge positions

8 positions: `TopLeft`, `TopCenter`, `TopRight`, `CenterLeft`, `CenterRight`, `BottomLeft`, `BottomCenter`, `BottomRight`.

### Language & VO badge logic

- **Audio streams only** — language badges are based exclusively on audio streams. Subtitle streams are never considered.
- **Original language detection** — preferred: TMDB API (`original_language` field) via `item.GetProviderId("Tmdb")`. Fallback: `item.ProductionLocations` (less reliable — ordering issues with co-productions). `BaseItem.OriginalLanguage` does not exist in the Emby SDK. For `Episode` items, `ProductionLocations` is always empty — navigate up via `GetParent()` twice (Episode → Season → Series) to get the series' locations. TMDB endpoint: try `movie/{id}` first, fallback to `tv/{id}` for misidentified items (404).
- **TMDB API key** — configured in plugin settings (`TmdbApiKey`). Without it, falls back to `ProductionLocations`. Results cached in-memory (`ConcurrentDictionary`) per Emby session. HTTP calls use `HttpClient.Send()` (synchronous, avoids deadlocks).
- **Debug mode** — `DebugMode` config flag renders an overlay on each image showing source, `origIcon`, and audio stream languages. Useful for diagnosing metadata issues.
- **Managed languages**: French, English, Japanese. Any identified audio language outside this set is "unmanaged".
- **VO badge** — shown only when ALL three conditions hold: `HasUnmanagedAudioLanguage` (an unmanaged audio stream is present) AND `HasKnownOriginCountry` (TMDB has production location data) AND `OriginalLanguageIcon == null` (original language is not managed). A US film with Spanish/Portuguese dubs does NOT show VO (original = EN, managed).
- **Highlight** — gold border on the badge matching `OriginalLanguageIcon`. VO badge is always highlighted when shown (it IS the original language badge).

### Embedded resources

- `src/EmbyBadges/Icons/*.png` — badge icons (resolution, language flags). Source for flag icons: `yocksers/EmbyIcons` (EmbeddedIcons/ directory). Fetch: `gh api repos/yocksers/EmbyIcons/contents/EmbeddedIcons --jq '.[].name'`
- `src/EmbyBadges/Icons/badge_font.ttf` — Trebuchet Bold, required for text badges (VO, connector initials) on Alpine Linux where no system fonts are available
- `src/EmbyBadges/Configuration/configPage.html` and `configScript.js` — generated by `gen_config_page.py`

### Project structure

```
EmbyBadges.sln
gen_config_page.py               # Generates configPage.html + configScript.js
libs/                            # Emby SDK DLLs (committed for CI)
dist/                            # Build output (gitignored except deps.json)
src/EmbyBadges/
├── EmbyBadges.csproj
├── Plugin.cs                    # BasePlugin<PluginConfiguration>; registers config pages
├── PluginConfiguration.cs       # GroupConfig, BadgePosition, MultiVersionTrigger enums
├── Enhancer/
│   ├── BadgeEnhancer.cs         # IImageEnhancer — auto-discovered by Emby
│   └── BadgeDataExtractor.cs    # Extracts resolution, languages, versions, VL connectors
├── ImageProcessing/
│   ├── BadgeRenderer.cs         # SkiaSharp rendering; badge types: PngBadge, TextBadge, HeartBadge
│   └── IconLoader.cs            # Thread-safe lazy loader for embedded PNGs and font
└── Configuration/
    ├── configPage.html           # Generated — HTML fragment for Emby config UI
    └── configScript.js           # Generated — AMD module for config page JS
```

Emby SDK DLLs in `libs/` use `<HintPath>` with `<Private>false</Private>` (present at Emby runtime, not bundled). Copy from `../Emby_Virtuallib/libs/` or a local Emby install.

## Image Cache

Emby has two cache layers that both need clearing when badge rendering changes aren't visible:
- `enhanced-images/` — output of `IImageEnhancer`, keyed by `GetConfigurationCacheKey`
- `resized-images/` — resized thumbnails served to clients; does NOT respect the enhancer cache key

To force a full refresh after config or code changes:
```bash
kubectl exec -n media deployment/emby2 -- find /config/cache/images -name "*.jpg" -delete
```
Then hard-refresh the browser. Without this, stale cached images persist even when the cache key changes.

## Repository Conventions

- Code comments and variable names may be in French
- `private/` is gitignored — kubeconfig and other secrets live there
- `libs/` is committed intentionally for CI builds
