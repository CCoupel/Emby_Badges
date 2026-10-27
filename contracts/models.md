# Contrats de données — Emby_Badges

> Référence en cas de divergence entre le code et les attentes QA. Créé pour FEATURE #4 (v1.3.0).

## Badge « Synchro » (PlaySync) — FEATURE #4, v1.3.0

### 1. Source de vérité (lecture seule, SDK Emby uniquement — aucune dépendance à la DLL PlaySync)

| Donnée | Où | Comment la lire |
|---|---|---|
| Playlists | items Emby de type `Playlist` | `ILibraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = ["Playlist"], Recursive = true })` |
| Partage | table `UserItemShare` | `IItemRepository.GetUserItemShares(new UserItemShareQuery { ItemIds = [...] })` → lignes `(ItemId, UserId, ShareLevel)` |
| Propriétaire | ligne `ShareLevel == ManageDelete` | posée par Emby à la création |
| Membre | ligne `ShareLevel >= Read` | — |
| **Playlist partagée** | propriétaire connu **et** ≥ 1 membre ≠ propriétaire | même définition que PlaySync (`ShareClassifier`) ; les playlists publiques sans ligne de partage ne sont jamais partagées |
| Étiquettes PlaySync | `Playlist.Tags` (string[]) | 3 familles : `remove-si-lu`, `propager-lu`, `propager-avancement` |
| Contenu d'une playlist | enfants de la playlist | (1) `playlist.GetChildren(new InternalItemsQuery(ownerUser))` ; (2) repli `ILibraryManager.GetItemList(new InternalItemsQuery { ListIds = [playlist.InternalId] })` |

**Évaluation d'une famille d'étiquettes** (identique à `MarkerEvaluator` de PlaySync) :
- motif `^\s*<famille>\s*=\s*(NON|OUI)\s*\z`, casse ignorée, espaces tolérés ;
- toute autre chaîne (`remove-si-lu-oui`, `remove-si-lu=OUIX`, `remove-si-lu=`) est ignorée ;
- état : `OUI` seule → **active** ; `NON` seule, `OUI`+`NON` (NON l'emporte), aucune → inactive.

### 2. Modèle `SyncTypes` (C#, `EmbyBadges.Enhancer`)

```csharp
[Flags]
public enum SyncTypes
{
    None     = 0,
    Position = 1,   // propager-avancement=OUI   → lettre « P »
    Read     = 2,   // propager-lu=OUI           → lettre « L »
    Removed  = 4    // remove-si-lu=OUI          → lettre « S »
}
```

Règles :
- **Par playlist** : `Position` si `propager-avancement` active ; `Read` si `propager-lu` active ;
  `Removed` si `remove-si-lu` active **et** `propager-lu` active (état *effectif* : PlaySync ≥ 1.2.0 rend le
  retrait inerte sans `propager-lu`) — hypothèse H3, à valider au GATE 2 (alternative : état brut de l'étiquette).
- **Par média** : union (`|`) des `SyncTypes` de toutes les playlists partagées qui le contiennent.
- **Série / saison** : un épisode **propage** ses types à sa saison et à sa série (fait dans l'index `SyncDetector`,
  pas dans `AggregateEpisodes`) ; le `SyncTypes` d'une saison/série = union des propagations de ses épisodes et de ses
  propres playlists. **Seule la propagation épisode → saison → série est faite** : une saison ou une série placée
  directement dans une playlist n'est pas propagée à son parent (limitation connue).
- `MediaInfo.SyncTypes` (nouveau champ, défaut `None`).

### 3. Configuration (`PluginConfiguration`)

| Champ | Type | Défaut | Rôle |
|---|---|---|---|
| `Sync` | `GroupConfig` | `Position = TopLeft`, autres défauts de `GroupConfig` | position / taille / marge / opacité du groupe |
| `ShowSync` | `bool` | `true` | activation du groupe |
| `SyncRenderMode` | `enum SyncRenderMode { Letters, Color }` | `Letters` | mode de rendu |
| `ShowSyncPosition` | `bool` | `true` | inclure le type « position » |
| `ShowSyncRead` | `bool` | `true` | inclure le type « lu » |
| `ShowSyncRemoved` | `bool` | `true` | inclure le type « supprimé » |

Types **affichés** = `SyncTypes` détectés ∩ types activés. Résultat `None` → aucun badge.

### 4. Rendu

**Mode `Letters`** — un seul `TextBadge`, fond `ColorSync = (0, 120, 140)` (sarcelle, distinct de VO bleu /
MULTI orange / note or), libellé = concaténation des lettres actives dans l'**ordre fixe `P L S`** :
`P`, `L`, `S`, `PL`, `PS`, `LS`, `PLS`. Un seul type actif → une seule lettre (badge plus étroit, même hauteur).

**Mode `Color`** — un seul `SyncBadge` (pictogramme « sync », deux flèches circulaires, dessiné en vecteur
SkiaSharp comme `HeartBadge`, carré `h × h`), fond arrondi coloré selon le type **le plus fort** affiché :

| Priorité | Type | Couleur (RGB) |
|---|---|---|
| 1 (plus fort) | `Removed` | rouge `(200, 30, 30)` |
| 2 | `Read` | orange `(220, 120, 0)` |
| 3 | `Position` | vert `(40, 150, 60)` |

(hypothèse H1 — mapping et pictogramme vectoriel à valider au GATE 2 ; alternative : PNG `Icons/sync.png` teinté.)

### 5. Clé de cache (`GetConfigurationCacheKey`)

Ajouts à la clé existante (même `string.Join("_", …)`) :
`GC(config.Sync)`, `config.ShowSync`, `config.SyncRenderMode`, `config.ShowSyncPosition`, `config.ShowSyncRead`,
`config.ShowSyncRemoved`, `(int)mediaInfo.SyncTypes`.

### 6. Index de synchro en mémoire (`SyncDetector`)

- Instantané `Dictionary<long /*InternalId*/, SyncTypes>` construit en **une passe** sur toutes les playlists
  partagées ayant ≥ 1 famille active ; TTL **5 min** (`SyncIndexTtl`).
- **Premier build** : bloquant sur le thread appelant (les autres appels concurrents voient un index vide pendant ce temps).
- **Reconstructions suivantes** (TTL expiré) : l'**ancien instantané est servi** ; la reconstruction tourne en
  **tâche de fond** (`Task.Run`), en *single flight* (`Interlocked.CompareExchange` sur un drapeau : une seule
  reconstruction à la fois, les autres appels se contentent de l'ancien instantané).
- **Échec de reconstruction** : log `Warn`, l'ancien index est **conservé** (jamais d'index partiel, jamais d'exception
  remontée au rendu).
- Lecture des enfants d'une playlist, dans l'ordre, arrêt au premier résultat non vide : (1) chaque utilisateur ayant
  accès à la playlist, par niveau de partage décroissant (propriétaire d'abord), via `GetChildren(new InternalItemsQuery(user))` ;
  (2) lecture sans utilisateur ; (3) repli `GetItemList(ListIds = [playlist])`.
- Coût par calcul de clé de cache : une recherche `O(1)`. Coût par reconstruction : 1 requête playlists +
  1 requête partages (batch) + 1 lecture d'enfants par playlist synchronisée.
- `ShowSync = false` : `GetSyncTypes` renvoie `None` avant tout appel, l'index n'est jamais construit.
- Conséquence : un changement d'appartenance / d'étiquette est visible au plus **5 min** après (puis cache images
  Emby : clé changée → `enhanced-images` régénéré ; `resized-images` reste soumis à la limitation connue).
- PlaySync absent ou aucune playlist étiquetée : index vide, `SyncTypes.None` partout, aucun badge.

---

## Badges de playlist : partage (#5) et progression (#6), v1.4.0

> Décisions utilisateur du 2026-10-27 (GATE 1.5) : badges dessinés sur les vignettes des items `Playlist` ;
> progression selon le **propriétaire** ; pictogramme Synchro coloré selon les étiquettes de la playlist (sarcelle sans
> étiquette active) ; texte « lus/total » ; toutes les playlists ayant un propriétaire ; mise à jour sur événement (~30 s).
> Maquette : `_work/mockup/v1.4.0/ui/badge_playlist__indicateurs.html`. GATE 2 (2026-10-27) : H1 et H2 acceptées ;
> **H3 amendée** : le badge Favoris s'affiche aussi sur les playlists, la Note reste exclue.

### 7. Source de vérité (ajouts à §1)

| Donnée | Où | Comment la lire |
|---|---|---|
| Propriétaire d'une playlist | première ligne `ShareLevel == ManageDelete` de `GetUserItemShares` | `SyncDetector.OwnerOf(rows)` ; aucune ligne → `null` (« propriétaire introuvable ») |
| Playlist partagée | `SyncDetector.IsShared(rows)` (inchangé, §1) | appliqué à **toutes** les playlists, étiquetées ou non |
| Étiquettes propres | `SyncDetector.EvaluateTags(playlist.Tags)` (inchangé) | état effectif, H3 v1.3.0 |
| Contenu | `ReadChildren` (inchangé, §6) | les entrées dossier (`Series`, `Season`, `Folder`) sont développées en leurs médias non-dossier (lecture avec l'utilisateur propriétaire) |
| Lu | `IUserDataManager.GetUserData(owner, media).Played` | propriétaire uniquement, jamais l'utilisateur courant (cache global) |

### 8. Modèle `PlaylistInfo` (C#, `EmbyBadges.Enhancer`, interne à l'index)

```csharp
internal sealed record PlaylistInfo(
    bool      IsShared,    // IsShared(rows)
    long?     OwnerId,     // OwnerOf(rows) ; null = propriétaire introuvable
    SyncTypes OwnTypes,    // EvaluateTags(playlist.Tags), indépendant du partage
    int       Played,      // médias lus par le propriétaire (0 si OwnerId == null)
    int       Total);      // médias lisibles (0 si OwnerId == null)
```

Règles :
- `OwnerId == null` → `IsShared == false` (déjà vrai par définition) et **aucune** progression (`Total = 0`).
- `Total = 0` → pas de badge de progression (playlist vide, ou contenu illisible).
- Un même média présent deux fois dans la playlist compte **une fois** (dédoublonnage par `InternalId`).
- Les médias d'une entrée dossier comptent individuellement (une série de 10 épisodes compte pour 10).

### 9. `MediaInfo`, nouveaux champs (rétrocompatibles, défauts neutres pour tout item non-Playlist)

| Champ | Type | Défaut | Valeur pour une `Playlist` |
|---|---|---|---|
| `IsPlaylist` | `bool` | `false` | `true` |
| `PlaylistShared` | `bool` | `false` | `PlaylistInfo.IsShared` |
| `PlaylistSyncTypes` | `SyncTypes` | `None` | `PlaylistInfo.OwnTypes` |
| `PlaylistPlayed` | `int` | `0` | `PlaylistInfo.Played` |
| `PlaylistTotal` | `int` | `0` | `PlaylistInfo.Total` |

Pour une `Playlist`, le champ existant **`IsFavorite`** est aussi rempli, par la même règle que les médias
(`DetectFavorite` : premier utilisateur, `userManager.Users.FirstOrDefault()`). **Aucun autre champ n'est rempli** :
résolution, langue, versions, note (`Rating = null`) et `SyncTypes` restent vides. La playlist porte donc les badges de
playlist et le badge Favoris (H3 amendée au GATE 2).

### 10. Configuration (`PluginConfiguration`), ajouts

| Champ | Type | Défaut | Rôle |
|---|---|---|---|
| `PlaylistShare` | `GroupConfig` | `Position = TopLeft` | position / taille / marge / opacité de l'indicateur de partage |
| `ShowPlaylistShare` | `bool` | `true` | activation de l'indicateur de partage |
| `PlaylistProgress` | `GroupConfig` | `Position = BottomRight` | position / taille / marge / opacité de la progression |
| `ShowPlaylistProgress` | `bool` | `true` | activation de la progression |

Page de config (`gen_config_page.py`) : deux sections « Playlist partagée » et « Progression de playlist » après
« Synchro (PlaySync) », listes de groupes JS étendues à `'PlaylistShare', 'PlaylistProgress'`.

### 11. Rendu

- **Partage** : `ShowPlaylistShare && PlaylistShared` → un `SyncBadge` (pictogramme vectoriel existant, carré `h×h`),
  fond = `ShareColorOf(PlaylistSyncTypes)` :
  `None` → sarcelle `ColorSync (0,120,140)` ; sinon `ColorOf(types)` (rouge > orange > vert, §4).
  Ne dépend **ni** de `SyncRenderMode` **ni** de `ShowSyncPosition/Read/Removed`.
- **Progression** : `ShowPlaylistProgress && PlaylistTotal > 0` → un `TextBadge` de libellé
  `ProgressLabel(played, total)` = `"{played}/{total}"` (culture invariante), fond `ColorProgress (60,60,60)`, ou
  `ColorSyncPosition (40,150,60)` si `played == total` (H1).
- Valeurs défensives : `played` borné à `[0, total]`.
- **Favoris** (H3 amendée) : `ShowFavorites && IsFavorite` → le `HeartBadge` existant, groupe `Favorites`, même
  couleur, même position et mêmes réglages que sur les médias. Avec les défauts, Favoris et Progression sont tous deux
  en `BottomRight` et sont donc dessinés côte à côte dans le même groupe de position (comportement standard de `DrawGroup`).
- **Note** : jamais affichée sur une playlist (`Rating` non rempli).

### 12. Clé de cache (`GetConfigurationCacheKey`), ajouts à §5

`GC(config.PlaylistShare)`, `GC(config.PlaylistProgress)`, `config.ShowPlaylistShare`, `config.ShowPlaylistProgress`,
`mediaInfo.IsPlaylist`, `mediaInfo.PlaylistShared`, `(int)mediaInfo.PlaylistSyncTypes`, `mediaInfo.PlaylistPlayed`,
`mediaInfo.PlaylistTotal`.
Favoris d'une playlist : **déjà couvert** par les segments existants `GC(config.Favorites)`, `config.ShowFavorites` et
`mediaInfo.IsFavorite`. Aucun segment supplémentaire n'est nécessaire.
Ajouter des segments change la clé de **toutes** les images badgées : chacune est régénérée une fois après la mise à jour
(même effet qu'en v1.3.0).

### 13. Index (`SyncDetector`), évolutions de §6

- L'instantané porte trois tables : `Items: Dictionary<long, SyncTypes>` (inchangée), `Playlists:
  Dictionary<long, PlaylistInfo>` et `Members: HashSet<long>` (tous les médias présents dans une playlist indexée, pour
  filtrer les événements).
- **Une seule passe** : 1 requête playlists (toutes) + 1 requête partages (batch, **toutes** les playlists) + lecture des
  enfants **seulement si nécessaire** :
  - pour `Items` : playlist partagée **et** étiquetée **et** `ShowSync` ;
  - pour la progression : propriétaire connu **et** `ShowPlaylistProgress`.
  Une playlist lue pour les deux usages n'est lue qu'une fois.
- **Gating** (`IndexNeeded(config)`) : l'index n'est construit que si `ShowSync || ShowPlaylistShare || ShowPlaylistProgress`.
  Si tous sont `false`, `GetSyncTypes` et `GetPlaylistInfo` renvoient le neutre **avant** tout appel (extension de CA11 v1.3.0).
  `ShowSync = false` seul → `GetSyncTypes` renvoie `None` (comportement v1.3.0 inchangé), même si l'index existe pour les playlists.
- **Fraîcheur** : `IsStale(builtAt, now, dirty)` = `now - builtAt >= 5 min` **ou** (`dirty` **et** `now - builtAt >= 30 s`).
  La reconstruction reste en tâche de fond, en single flight, l'ancien instantané servi entre-temps ; `dirty` est remis à
  `false` au **début** de la reconstruction, pour qu'un événement survenu pendant celle-ci ne soit pas perdu.
- **Invalidation sur événement** (`MarkDirty`), via un `IServerEntryPoint` (`PlaylistEventsEntryPoint`, abonnement dans
  `Run`, désabonnement dans `Dispose`) :

| Événement | Filtre | Effet |
|---|---|---|
| `IUserDataManager.UserDataSaved` | `SaveReason ∈ {PlaybackFinished, TogglePlayed}` **et** `Item.InternalId ∈ Members` | `MarkDirty` |
| `IPlaylistManager.PlaylistItemsAdded` / `PlaylistItemsRemoved` | aucun | `MarkDirty` |
| `ILibraryManager.ItemRemoved` | item ∈ `Members`, ou item `Playlist` | `MarkDirty` |
| `ILibraryManager.ItemAdded` | item `Playlist` | `MarkDirty` |

  `PlaybackProgress` et `PlaybackStart` sont **ignorés** : ils arrivent toutes les quelques secondes pendant la lecture.
  Un changement de partage ou d'étiquette n'a pas d'événement fiable : il est couvert par le TTL de 5 min.
- Les gestionnaires d'événements ne font **aucun** appel bloquant ni aucune exception remontée : ils positionnent un drapeau.
