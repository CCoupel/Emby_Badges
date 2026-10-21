# Changelog des contrats

> Changements de contrats (API, schemas) : `BREAKING` | `CHANGED` | `ADDED`. Lu par le CDP au GATE 2.

## [20261021] — FEATURE #4 : badge « Synchro » PlaySync (v1.3.0)

- **[NEW]** `contracts/models.md` — modèle `SyncTypes` (flags Position/Read/Removed), règles d'évaluation des étiquettes PlaySync, union par média et par série/saison
- **[NEW]** `PluginConfiguration` — groupe `Sync` (`GroupConfig`), `ShowSync`, `SyncRenderMode` (`Letters`|`Color`), `ShowSyncPosition`, `ShowSyncRead`, `ShowSyncRemoved`
- **[CHANGED]** `MediaInfo` — nouveau champ `SyncTypes` (rétrocompatible, défaut `None`)
- **[CHANGED]** `GetConfigurationCacheKey` — 7 segments ajoutés (config du groupe Sync + `SyncTypes` de l'item) : toutes les images badgées sont régénérées une fois après mise à jour (comportement attendu, pas un breaking)
- Aucun changement BREAKING. Pas de dépendance à la DLL PlaySync : lecture via SDK Emby (`IItemRepository.GetUserItemShares`, `Playlist.Tags`, `GetChildren` / `ListIds`).
