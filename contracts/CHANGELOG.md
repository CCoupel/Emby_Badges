# Changelog des contrats

> Changements de contrats (API, schemas) : `BREAKING` | `CHANGED` | `ADDED`. Lu par le CDP au GATE 2.

## [20261021] — FEATURE #4 : badge « Synchro » PlaySync (v1.3.0)

- **[NEW]** `contracts/models.md` — modèle `SyncTypes` (flags Position/Read/Removed), règles d'évaluation des étiquettes PlaySync, union par média et par série/saison
- **[NEW]** `PluginConfiguration` — groupe `Sync` (`GroupConfig`), `ShowSync`, `SyncRenderMode` (`Letters`|`Color`), `ShowSyncPosition`, `ShowSyncRead`, `ShowSyncRemoved`
- **[CHANGED]** `MediaInfo` — nouveau champ `SyncTypes` (rétrocompatible, défaut `None`)
- **[CHANGED]** `GetConfigurationCacheKey` — 7 segments ajoutés (config du groupe Sync + `SyncTypes` de l'item) : toutes les images badgées sont régénérées une fois après mise à jour (comportement attendu, pas un breaking)
- Aucun changement BREAKING. Pas de dépendance à la DLL PlaySync : lecture via SDK Emby (`IItemRepository.GetUserItemShares`, `Playlist.Tags`, `GetChildren` / `ListIds`).

## [20261021] — FEATURE #4 : alignement du contrat sur l'implémentation (95be4f7)

- **[CHANGED]** `contracts/models.md` §6 — index `SyncDetector` : premier build bloquant, reconstructions suivantes en tâche de fond (`Task.Run`, single flight `Interlocked`), ancien instantané servi pendant la reconstruction, échec → `Warn` + ancien index conservé ; ordre de lecture des enfants (utilisateurs par niveau décroissant → sans utilisateur → `ListIds`)
- **[CHANGED]** `contracts/models.md` §2 — propagation épisode → saison → série uniquement (faite dans l'index) ; une saison/série placée directement dans une playlist n'est pas propagée à son parent

## [20261027] — FEATURE #5 / #6 : badges de playlist (partage + progression) (v1.4.0)

- **[NEW]** `contracts/models.md` §7–13 — modèle `PlaylistInfo` (partagée, propriétaire, types propres, lus, total), règles de comptage (dédoublonnage, dossiers développés), rendu partage (pictogramme Synchro, couleur selon les étiquettes, sarcelle sinon) et progression (`TextBadge` « lus/total »)
- **[NEW]** `PluginConfiguration` — groupes `PlaylistShare` (TopLeft) et `PlaylistProgress` (BottomRight), flags `ShowPlaylistShare`, `ShowPlaylistProgress` (défaut `true`)
- **[CHANGED]** `MediaInfo` — champs `IsPlaylist`, `PlaylistShared`, `PlaylistSyncTypes`, `PlaylistPlayed`, `PlaylistTotal` (rétrocompatibles, défauts neutres)
- **[CHANGED]** `BadgeEnhancer.Supports` — accepte `Playlist` (Primary, Thumb)
- **[CHANGED]** badge Favoris — affiché aussi sur les playlists (`IsFavorite` rempli pour une `Playlist`, premier utilisateur, H3 amendée au GATE 2) ; la Note reste exclue ; clé de cache déjà couverte par `mediaInfo.IsFavorite`
- **[CHANGED]** `GetConfigurationCacheKey` — 9 segments ajoutés : toutes les images badgées sont régénérées une fois après la mise à jour (attendu, pas un breaking)
- **[CHANGED]** index `SyncDetector` — partages chargés pour **toutes** les playlists ; gating étendu (`ShowSync || ShowPlaylistShare || ShowPlaylistProgress`) ; fraîcheur 30 s après un événement (`UserDataSaved` fin de lecture/marquage, `PlaylistItemsAdded/Removed`, `ItemAdded/ItemRemoved`), 5 min sinon
- **[NEW]** `PlaylistEventsEntryPoint` (`IServerEntryPoint`) — abonnement aux événements Emby, désabonnement au `Dispose`
- Aucun changement BREAKING. Badge Synchro des médias inchangé (mêmes entrées, même rendu, même gating `ShowSync`).
