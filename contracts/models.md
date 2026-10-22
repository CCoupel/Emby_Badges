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
