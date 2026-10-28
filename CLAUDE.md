# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Emby_Badges** is a C# Emby Media Server plugin that overlays visual badges on media thumbnails/posters. The badges display:

- **Resolution** of all available versions: SD (480p), HD (720p), Full HD (1080p), 4K
- **Audio language**: French, English, VO (audio present but unknown language)
- **Multi-version / VirtualLib**: connector initial letter (or "MULTI"), with configurable trigger
- **Favorites**: heart icon when the item is marked as favorite by the first user
- **Rating (Note)**: community rating shown on the poster (since v1.2.0)
- **Synchro (PlaySync)**: sync state of the item in shared playlists tagged for PlaySync — `P` (position), `L` (read), `S` (removed), as letters or as a coloured sync pictogram (see « PlaySync integration »)
- **Shared playlist (v1.4.0, #5)**: on `Playlist` thumbnails, a sync pictogram when the playlist is shared (teal without an active PlaySync label, otherwise coloured by the strongest label), and the favorites heart when the first user marked the playlist as favorite. Playlists never show the Rating. See « Playlist badges »
- **Playlist progress (v1.4.0, #6)**: on `Playlist` thumbnails, a `played/total` counter (grey; green when everything is played), counted from the **owner's** watch state. Bottom right by default

Each badge group (Resolution, Language, MultiVersion, Favorites, Rating, Synchro, Playlist share, Playlist progress) has independent position, size, margin, and opacity settings. Individual badges within a group share those settings but can be individually enabled/disabled.

## Technology Stack

- **Language:** C# / .NET 6
- **Build:** `dotnet` CLI (user-level install, not in PATH)
- **Image rendering:** SkiaSharp 2.88.6 (bundled)
- **Target:** Emby Media Server plugin (`net6.0`)
- **Deployment:** Kubernetes (Rancher cluster, namespace `media`, deployment `emby2`)

## Build

```bash
"/mnt/c/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe" build \
  src/EmbyBadges/EmbyBadges.csproj \
  --configuration Release \
  --output dist/
```

Reference shell is WSL (`python3`, `/mnt/c/...` paths), as in `commands.*` of `.claude/project-config.json`. On Git Bash/Windows use `python` and `C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe` instead.

**CI script:** `bash scripts/ci.sh` (`commands.ci`) runs `gen_config_page.py` + Release build and checks `dist/EmbyBadges.dll`; it auto-detects the user-level `dotnet` (override with `DOTNET=`). `.github/workflows/ci-branch.yml` runs it on `milestone/**` pushes and `workflow_dispatch` (pre-validation, read-only, no publish) — it is the barrier checked before `/publish prod`. `release.yml` still has its own steps (deferred gap: not yet reduced to `run: bash scripts/ci.sh`).

Produces `dist/EmbyBadges.dll`. Official process: `/build` (candidate) → `/publish qualif` → `/deploy qualif` (→ `/publish prod` → `/deploy prod`). Project-specific procedures live in `.claude/agents/deploy.md` and `.claude/agents/environments/{publish,deploy}.{qualif,prod}.md`.

### Config page generation

`configPage.html` and `configScript.js` are generated from `gen_config_page.py` (which inlines icon PNGs as base64):

```bash
python3 gen_config_page.py
```

Must be run before building when the config page changes.

### Tests

Unit tests (xUnit) live in `tests/EmbyBadges.Tests/` (pure logic: `SyncDetector.EvaluateTags`, `SyncDetector.IsShared`, `BadgeRenderer.LettersOf`/`ColorOf`, cache key; since v1.4.0 `PlaylistShareTests` — `OwnerOf`, `IndexNeeded`, `ShareColorOf` — and `PlaylistProgressTests` — `CountProgress`, `ProgressLabel`, `IsStale`, `ShouldInvalidateOnUserData`). Manual QUALIF procedures: `tests/procedures/` (index `tests/INDEX.md`). They reach internals via `InternalsVisibleTo`.

```bash
/mnt/c/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe test tests/EmbyBadges.Tests/
```

Config key: `commands.test` in `.claude/project-config.json`.

## Deploy

See `.claude/agents/environments/deploy.qualif.md` (and `deploy.prod.md`) for the full deploy sequence with all critical rules. Key points:

- **Kubeconfig**: `export KUBECONFIG=private/kubeconfig.yml` (gitignored, required)
- **Destination path**: `/config/plugins/EmbyBadges.dll` — root of plugins folder, **not** a subdirectory
- **`MSYS_NO_PATHCONV=1`**: required on Git Bash/Windows for all kubectl commands with absolute Linux paths
- **Source path**: relative (`build/qualif_vX.Y.Z/EmbyBadges-X.Y.Z.a.dll`), not absolute
- Copy **before** restart; verify DLL size in the **new** pod after restart
- **Environments**: QUALIF = `emby2`, PROD = `emby` (namespace `media`). Deploy to `emby` only through `/deploy prod`, on explicit user order, from the GitHub Release asset
- **Web UI**: QUALIF `emby2` = `http://media.coupel.net/emby/web/` (the user logs in; agents never type a password). PROD `emby` URL: not recorded yet

## Release

```bash
git tag v1.0.0
git push origin v1.0.0
```

The `.github/workflows/release.yml` CI triggers on `vX.Y.Z` tags, runs `gen_config_page.py`, builds in Release with version from tag, and creates a GitHub Release with `dist/EmbyBadges.dll`.

**Before tagging**, bump `<Version>` in `src/EmbyBadges/EmbyBadges.csproj` to the tag version (`X.Y.Z`): the CI step "Check tag matches csproj version" fails the build otherwise.

## Emby Plugin Architecture

### Badge rendering — `IImageEnhancer`

`BadgeEnhancer` implements `IImageEnhancer` (auto-discovered by Emby, no manual registration needed). Key contract:

| Method | Role |
|---|---|
| `Supports(item, imageType)` | Primary/Thumb images for Movie/Episode/Series/Season/Playlist; Backdrop only for Episode/Series/Season (not Movie, not Playlist) |
| `GetConfigurationCacheKey(item, imageType)` | Must include ALL state affecting rendering — Emby caches images globally (not per-user) |
| `EnhanceImageAsync(...)` | Read inputFile → composite badges via SkiaSharp → write outputFile |
| `GetEnhancedImageSize(...)` | Returns `originalSize` (badges drawn in-canvas, no resize) |

**Cache key completeness is critical.** Missing any field (e.g. `IsFavorite`, `IsFromVirtualLib`) causes stale cached images. The cache key includes: all `GroupConfig` fields (including `Sync`), all `ShowXxx` flags, `MultiVersionTrigger`, `ResolutionIcons`, `AudioLanguages`, `VersionConnectors`, `IsFromVirtualLib`, `IsFavorite`, `DebugMode`, TMDB key presence, and for Synchro: `ShowSync`, `SyncRenderMode`, `ShowSyncPosition/Read/Removed`, `SyncTypes` (the detected state of the item). For playlists (v1.4.0): `GC(PlaylistShare)`, `GC(PlaylistProgress)`, `ShowPlaylistShare`, `ShowPlaylistProgress`, `IsPlaylist`, `PlaylistShared`, `PlaylistSyncTypes`, `PlaylistPlayed`, `PlaylistTotal`; the Favorites segments already cover a playlist's heart.

**Favorites limitation:** Images are cached globally — true per-user favorites badges aren't feasible. The plugin checks only `userManager.Users.FirstOrDefault()` (the first/admin user).

### Config page — Emby HTML fragment

The config page is an HTML **fragment** (no `<html>/<head>/<body>`), with root `<div is="emby-scroller" data-controller="__plugin/EmbyBadgesConfigScript">`. JS is a separate AMD module: `define([], function(){ return function(view){ ... }; })`. Always use `view.querySelector` (scoped to fragment), not `document.querySelector`.

### VirtualLib integration

When VirtualLib plugin is installed, `BadgeDataExtractor` reads `{PluginConfigurationsPath}/VirtualLib.xml` to get `<VirtualLibraryRootPath>`. Media paths under this root follow `{root}/{ConnectorDisplayName}/...`. The connector name is extracted from the first path segment after root and used as the badge label initial.

### PlaySync integration (Synchro badge)

The Synchro badge reads the **PlaySync** labels stored on Emby items. It does **not** reference the PlaySync DLL: it only uses the Emby SDK. Without PlaySync, no playlist carries these labels, so no badge is shown and no error is logged.

- **Labels**: `Playlist.Tags` of the Emby `Playlist` items. Three families: `remove-si-lu`, `propager-lu`, `propager-avancement`. Value `OUI` or `NON`, case-insensitive, spaces tolerated, pattern `^\s*<famille>\s*=\s*(NON|OUI)\s*\z`. Any other string (`remove-si-lu-oui`, `remove-si-lu=OUIX`, `remove-si-lu=`) is ignored. `NON` wins over `OUI`.
- **Shared playlists only**: a playlist counts when `IItemRepository.GetUserItemShares` shows an owner line (`ManageDelete`) and at least one other member with `ShareLevel >= Read`. Unshared playlists are ignored (even if labelled).
- **Types per playlist**: `Position` ← `propager-avancement=OUI`; `Read` ← `propager-lu=OUI`; `Removed` ← `remove-si-lu=OUI` **and** `propager-lu=OUI`. `Removed` is the *effective* state: PlaySync ≥ 1.2.0 makes `remove-si-lu` inert without `propager-lu`.
- **Union**: an item's `SyncTypes` is the union over all shared playlists that contain it. An episode propagates its types to its season and to its series (done in the index, not in `AggregateEpisodes`).
- **Index**: `SyncDetector` builds an in-memory snapshot `Dictionary<InternalId, SyncTypes>` in one pass (playlists → shares in one batch → children of labelled shared playlists). Lookups are O(1) when computing the cache key. TTL `SyncIndexTtl` = 5 min, shortened to 30 s after a relevant Emby event (see « Playlist badges »). Since v1.4.0 the same snapshot also holds per-playlist data (`PlaylistInfo`).
  - The rebuild is logged as « SyncIndex reconstruit » at **Debug** level: visible only with Emby's server debug logging (`EnableDebugLevelLogging`, Dashboard → Logs), not with the plugin's `DebugMode`.
  - First build: blocking on the request thread (other calls see the empty index meanwhile).
  - Later rebuilds (TTL expired): the previous snapshot is served, the rebuild runs in the background (`Task.Run`, single flight). A failed rebuild is logged at Warn and keeps the previous snapshot.
  - Children are read in this order, stopping at the first non-empty result: (1) each user with access to the playlist, by decreasing share level (owner first), via `GetChildren(new InternalItemsQuery(user))`; (2) a read without user; (3) fallback `GetItemList(ListIds = [playlist])`. Reading without a user alone returns empty for PlaySync's playlists (observed by PlaySync, U11), hence the owner-first order.
- **Gating**: `ShowSync = false` → `GetSyncTypes` returns `None` before any call (CA11). The index itself is built only if `SyncDetector.IndexNeeded(config)` is true (see « Playlist badges » for the three flags).
- **Scope**: images are cached globally, so the Synchro badge is the same for every Emby user, like Favorites. It reflects **all** shared labelled playlists on the server, not only those of the first user.
- **Latency**: a change in labels or shares is visible after at most 5 min (index TTL); a change in playlist membership after ~30 s (event, see « Playlist badges »), then the Emby image cache must regenerate (the new key changes `enhanced-images`). `resized-images` still requires the manual purge (see « Image Cache »).
- **Known limitation**: a season or series placed directly in a playlist is not propagated to its parent (only episodes propagate upward).

### Playlist badges (v1.4.0, #5 sharing, #6 progress)

Badges drawn on the thumbnail of an Emby `Playlist` item: sharing indicator, progress counter and favorites heart. A playlist shows no other badge.

- **Shared** = the playlist has an owner line (`ManageDelete` in `GetUserItemShares`, see `SyncDetector.OwnerOf`) **and** at least one other member with `ShareLevel >= Read`. The same definition as PlaySync (`SyncDetector.IsShared`), applied to **all** playlists, labelled or not. A playlist without an owner line → "owner not found", never shared.
- **Pictogram colour**: `SyncBadge` (sync arrows, square `h × h`). Teal `ColorSync (0,120,140)` when no PlaySync label is active; otherwise the colour of the strongest active type (red `Removed` > orange `Read` > green `Position`, see « PlaySync integration »). It ignores `SyncRenderMode` and `ShowSyncPosition/Read/Removed`.
- **Labels**: read from the playlist's own `Tags` (`EvaluateTags`), independent of the sharing state.
- **Favorites**: the heart uses the first user (`userManager.Users.FirstOrDefault()`), exactly as for media. Rating is never filled for a playlist, so no rating badge.
- **No other badge**: resolution, language, versions and sync letters are not computed for a playlist.
- **Progress**: `ShowPlaylistProgress && PlaylistTotal > 0` → a `TextBadge` `"{played}/{total}"` (invariant culture), grey `ColorProgress (60,60,60)`, or green `ColorSyncPosition (40,150,60)` when `played == total`. `played` is clamped to `[0, total]`.
- **Progress counting** (`SyncDetector.ComputeProgress`): applies to every playlist that has an owner, shared or not. Read from the owner's children (`GetChildren` with the owner); folders (series, seasons) are expanded to their non-folder media (owner's view, no virtual or unaired episodes). A media listed twice counts once. The owner's watch state is read with `IUserDataManager.GetUserData(owner, media).Played`, never the current user (the cache is global). A playlist without owner, or empty, shows no progress badge.
- **Gating**: `SyncDetector.IndexNeeded(config)` = `ShowSync || ShowPlaylistShare || ShowPlaylistProgress`. With all three `false`, nothing is read. `ShowSync = false` alone keeps the v1.3.0 behaviour (`GetSyncTypes` returns `None`).
- **Events** (`PlaylistEventsEntryPoint`, `IServerEntryPoint`; subscriptions in `Run`, removed in `Dispose`, each handler isolated by try/catch): they only set a dirty flag (`SyncDetector.MarkDirty`), never a blocking call.
  - `IUserDataManager.UserDataSaved`: only `PlaybackFinished` or `TogglePlayed`, on an item that is a member of an indexed playlist. When the event's user is known, only the owner of that playlist counts (`ShouldInvalidateOnUserData`). `PlaybackStart` and `PlaybackProgress` are ignored (they fire every few seconds during playback).
  - `IPlaylistManager.PlaylistItemsAdded` / `PlaylistItemsRemoved`: always mark dirty.
  - `ILibraryManager.ItemRemoved`: an indexed member or a playlist. `ILibraryManager.ItemAdded`: a playlist.
  - Share and label changes have no reliable event: they are covered by the 5 min TTL.
- **Freshness**: `IsStale` = age ≥ 5 min, or (dirty and age ≥ 30 s). `dirty` is reset at the **start** of a rebuild, so an event during the rebuild is not lost.
- **Latency**: the rebuild is lazy. The first image request after 30 s still serves the old snapshot and starts the background rebuild; the new value appears on the **next** display of the thumbnail. Afterwards the usual `resized-images` limit applies (manual purge).
- **Index rebuild**: `Snapshot.HadItems` records whether the snapshot was built with `ShowSync` on. When `ShowSync` is enabled after the index was built, `GetSyncTypes` forces a background rebuild, at most every 10 s, so the change is taken into account without waiting for the 5 min TTL.
- **Open points, not verified** (v1.4.0 QUALIF validated by the user without running the detailed procedure): auto-generated playlists ("collage automatique") may carry no badge; the `PlaylistEventsEntryPoint active` Debug log at startup (if absent, Emby failed to inject the entry point and the plugin relies on the TTL alone); `IsVirtualItem = false` / `IsUnaired = false` on a series with missing episodes (they may inflate `Total` otherwise).

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
│   ├── BadgeDataExtractor.cs    # Extracts resolution, languages, versions, VL connectors
│   ├── SyncDetector.cs          # PlaySync labels + shares → SyncTypes; playlist progress; in-memory index (TTL 5 min, 30 s after an event)
│   └── PlaylistEventsEntryPoint.cs # IServerEntryPoint — Emby events → SyncDetector.MarkDirty (v1.4.0)
├── ImageProcessing/
│   ├── BadgeRenderer.cs         # SkiaSharp rendering; badge types: PngBadge, TextBadge, HeartBadge, SyncBadge
│   └── IconLoader.cs            # Thread-safe lazy loader for embedded PNGs and font
└── Configuration/
    ├── configPage.html           # Generated — HTML fragment for Emby config UI
    └── configScript.js           # Generated — AMD module for config page JS
tests/EmbyBadges.Tests/          # xUnit — pure logic (see « Tests »)
```

Emby SDK DLLs in `libs/` use `<HintPath>` with `<Private>false</Private>` (present at Emby runtime, not bundled). Copy from `../Emby_Virtuallib/libs/` or a local Emby install.

## Emby web UI (Chrome)

Rules for any agent driving the user's Emby in Chrome (screenshots, visual checks):
- Clicking the **centre** of a poster **starts playback**: open an item's page from the poster's edge or title, never start a playback.
- Never open or capture the Emby Dashboard, nor the plugin's TMDB API key.
- The user logs in; an agent never types a password.

## Image Cache

Emby has two cache layers that both need clearing when badge rendering changes aren't visible:
- `enhanced-images/` — output of `IImageEnhancer`, keyed by `GetConfigurationCacheKey`
- `resized-images/` — resized thumbnails served to clients; does NOT respect the enhancer cache key

To force a full refresh after config or code changes:
```bash
kubectl exec -n media deployment/emby2 -- find /config/cache/images \( -name "*.jpg" -o -name "*.webp" \) -delete
```
The purge must cover both `.jpg` and `.webp`: the emby2 cache is in webp. Then hard-refresh the browser. Without this, stale cached images persist even when the cache key changes.

## Repository Conventions

- Code comments and variable names may be in French
- `private/` is gitignored — kubeconfig and other secrets live there
- `libs/` is committed intentionally for CI builds

---

## Agents Disponibles

| Nom | Rôle | Fichier | Spawn |
|-----|------|---------|-------|
| `planner` | Plan d'implémentation + contrats API | `.claude/agents/implementation-planner.template.md` | permanent |
| `dev-plugin` | Plugin (Emby Media Server plugin, C# / .NET 6) | `.claude/agents/dev-plugin.template.md` | permanent |
| `test-writer` | Scripts de tests + procédures QA | `.claude/agents/test-writer.template.md` | permanent |
| `code-reviewer` | Revue de code | `.claude/agents/code-reviewer.template.md` | permanent |
| `qa` | Exécution des tests et validation | `.claude/agents/qa.template.md` | permanent |
| `doc-updater` | Documentation | `.claude/agents/doc-updater.template.md` | permanent |
| `deployer` | Build + Publication + Déploiement QUALIF/PROD | `.claude/agents/deploy.template.md` | permanent |
| `security` | Audit sécurité | `.claude/agents/security.template.md` | ponctuel |
| `infra` | Infrastructure (Kubernetes / CI — pas de Dockerfile ni Helm) | `.claude/agents/infra.template.md` | ponctuel |

> **Fichier** pointe vers le `.template.md` — géré par sync, toujours présent. Un compagnon
> `.md` (sans suffixe) peut exister à côté pour des adaptations projet ; il est optionnel et
> n'est jamais référencé ici puisqu'il ne contient jamais la définition complète de l'agent.

> **permanent** = spawné au `/start-session`, reste en IDLE toute la session.  
> **ponctuel** = spawné à la demande par la commande dédiée, fermé après DONE.

> Pour `deployer` : la procédure concrète de PUBLISH/DEPLOY (un fichier par tâche × environnement)
> vit dans `.claude/agents/environments/{publish,deploy}.<env>.md` — voir `agents/deploy.md`
> section "Fichiers d'Environnement".

---

<!-- BEGIN TEAMLEADER_PROTOCOL — maintenu par le template, ne pas modifier manuellement -->

## Rôle Teamleader — Règles Critiques

> Ce bloc est maintenu par le template. Pour le mettre à jour : `/init-project` option d (step d6).

### Identité

Tu es le **teamleader** et le **Chef De Projet (CDP)** — un seul rôle, jamais délégué à un agent séparé.  
Tu **coordonnes et dispatches**. Tu n'exécutes aucune tâche technique toi-même.

### Délégation Stricte — Outils Interdits

| Outil interdit | Déléguer à |
|---------------|-----------|
| `Edit`, `Write`, `MultiEdit` (sauf `Write` d'un ordre dans `_work/tasks/*.md` et de l'état `.claude/workflow-state.json`, `.claude/workflow/*`) | `dev-*`, `doc-updater` |
| `Bash` (build / test / git) | `qa`, `deployer`, `dev-*` |
| `Read` (code applicatif) | `code-reviewer`, `planner` |
| `Glob`, `Grep` (recherche code) | `planner`, `dev-*` |

**`Read` autorisé uniquement pour** : `CLAUDE.md`, `MEMORY.md`, `project-config.json`, `_work/tasks/*.md`, `_work/handoff/*.md`, `_work/reports/*.md`, `contracts/CHANGELOG.md`

**`Write` autorisé uniquement pour** : `_work/tasks/*.md` (ordres aux teammates, création seule — jamais `Edit`/`MultiEdit`) et l'état d'orchestration `.claude/workflow-state.json` / `.claude/workflow/plan-<version>.md` (reprise après interruption) — jamais un autre chemin

**Ne jamais** exécuter une tâche technique soi-même — spawner l'agent approprié.

### Dispatcher une tâche

Tous les teammates sont spawned au démarrage (`/start-session`) et sont en IDLE.
**Pendant la session : uniquement `SendMessage` — jamais de spawn.**

```
SendMessage({ to: "<nom-canonique>", content: "<tâche complète>" })
→ Attendre ACTIF (confirmation) + DONE (références fichiers)
```

**Ordre de plus de 3 lignes → fichier** : `Write` dans `_work/tasks/<agent>-<YYYYMMDD-HHmmss>.md` (un fichier par ordre,
jamais réécrit), puis `SendMessage` avec le seul chemin + un résumé d'une ligne :
`Tâche : _work/tasks/<agent>-<timestamp>.md — <résumé>`. Le fichier liste périmètre, fichiers à lire et livrables
attendus. Restent inline : ordre ≤ 3 lignes, `/clear`, réponses à un `BLOQUE`, correction de rapport invalide.
Relire l'ordre (`_work/tasks/*.md`) pour contrôler le `DONE` par rapport à la demande.

Plusieurs agents en parallèle — même tour :
```
SendMessage({ to: "dev-backend",  content: "<tâche>" })
SendMessage({ to: "dev-frontend", content: "<tâche>" })
```

### Nommage des Agents — Règle Absolue

Le paramètre `name` dans `Task` est **toujours le nom canonique simple** : `qa`, `dev-backend`, `planner`…  
**Jamais de suffixe** (`qa-1`, `qa-2`…). Un rôle = un nom = une adresse `SendMessage` permanente.

**Noms canoniques** :
```
planner, dev-backend, dev-frontend, dev-firmware, dev-plugin,
test-writer, code-reviewer, qa, doc-updater, deployer, security, infra
```

### Questions à l'utilisateur

**Règle absolue** : toute information, décision ou validation attendue de l'utilisateur (y compris les `BLOQUE` /
`FAILED` remontés par un teammate) passe **par l'outil `AskUserQuestion`** — jamais en texte dans le chat (pas de
liste numérotée, « OUI/NON », `[O/n]`). Les teammates ne parlent jamais à l'utilisateur : tu convertis leurs
questions en `AskUserQuestion`, puis tu leur renvoies les réponses via `SendMessage`.
Procédure, format et checklist : `.claude/agents/teamleader.template.md`, section « Questions à l'utilisateur ».

### Relayer l'avancement

Chaque jalon `[NOM] EN COURS — …` d'un teammate (ex. `QA EN COURS — lot 3/12 …`) est relayé à l'utilisateur en
une ligne, sans attendre le DONE. Un jalon n'est pas un DONE : ne pas enchaîner avant le DONE. Si un jalon signale des échecs, les nommer dans le relais.

### Labels d'issues — à chaque changement de phase

Mettre à jour les labels de **toutes** les issues suivies (`ISSUE_NUMS[]`) **avant** d'envoyer l'ordre suivant, **et dès l'événement** (retour de fin de tâche d'un teammate `DONE`/`FAILED`/`BLOQUE`, validation d'un GATE — `GITHUB.md` §9.12), avant de le relayer. L'état dit ce qui **reste à faire** (ex. `PRET` = plan validé, DEV pas lancé), pas ce qu'un teammate fait à cet instant :
`SendMessage` à `deployer` (`gh issue edit --add-label … --remove-label …`, `.claude/commands/context/GITHUB.template.md` §9).
La clôture du milestone reste soumise à la validation utilisateur.

### Validation des rapports DONE

Un `DONE` valide ne contient **jamais** de contenu inline (code, diff, extraits).  
Format attendu : références fichiers uniquement (`_work/reports/`, `_work/handoff/`, SHA).

Si un agent envoie du contenu inline → corriger :
```
SendMessage({
  to: "<agent>",
  content: "Rapport invalide — écris le contenu dans _work/reports/<agent>-<timestamp>.md et renvoie le DONE avec la référence."
})
```

<!-- END TEAMLEADER_PROTOCOL -->
