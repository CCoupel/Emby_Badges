# MEMORY — Emby_Badges

## Etat courant
- Version : PROD `v1.4.0` livree le 2026-10-27 sur `emby` (tag `v1.4.0`, merge `530ddf3`, Release GitHub avec `EmbyBadges.dll` 691712 o ; `.prev` = v1.3.0, 672256 o). QUALIF `emby2` en 1.4.0.3 (meme contenu). csproj `<Version>` = `1.4.0` sur `main` (le prochain milestone repasse en `X.Y.Z.0` a l'ouverture de sa branche).
- Milestone `v1.4.0 - Indicateurs de playlist` (n°2) CLOS — #5 (pictogramme de partage + coeur Favoris sur playlist) et #6 (badge « lus/total » du proprietaire, maj sur evenement ~30 s) fermees. Aucune issue ni milestone ouverts. Branche `main` poussee ; `milestone/v1.4.0` supprimee (local + remote) ; `gh-pages` conservee.
- Template Claude `v3.27.0` (synchronise le 2026-10-25). Mise a jour disponible (`3347a2e`) non appliquee ; `.claude/commands/init-project.md` contient deja une partie de cette version, non commitee (a regler par `/init-project` option d).
- Tests : `tests/EmbyBadges.Tests/` xUnit 131 tests (Synchro + PlaylistShare + PlaylistProgress). Procedures manuelles `tests/procedures/badges/{synchro,playlist}/`.
- Site marketing a jour v1.4.0 (+ rattrapage v1.3.0), gh-pages `6b322c0`. Release notes datees (v1.3.0 2026-10-22, v1.4.0 2026-10-27).

## Decisions
- v1.4.0 : badges de playlist = items `Playlist` dans `Supports` (Primary/Thumb). Partage = pictogramme Synchro colore selon les etiquettes de la playlist (sarcelle sans etiquette) ; progression = lus/total selon le **proprietaire** (cache global), toutes playlists avec proprietaire ; Favoris (1er utilisateur) aussi sur playlist, pas de Note. Detail : CLAUDE.md « Playlist badges », contrat `contracts/models.md` §7–13.
- Planner sur `opus/high` via le compagnon `.claude/agents/implementation-planner.md` (quota Fable epuise le 2026-10-27).
- `IItemRepository` injecte directement dans `BadgeEnhancer` ; `PlaylistEventsEntryPoint` (`IServerEntryPoint`) demarre bien en QUALIF et PROD.
- BUILD/PUBLISH/DEPLOY : `.claude/agents/deploy.md` + `environments/{publish,deploy}.{qualif,prod}.md`. Rollback = restaurer `EmbyBadges.dll.prev`. Bump csproj avant tag : CLAUDE.md § Release.
- Pas de CHANGELOG.md (decision maintenue) ; release notes par version dans `docs/releases/vX.Y.Z/`.
- Agents : 9 dans la table CLAUDE.md ; `infra` et `marketing-release` ponctuels.
- Marketing : ton fun, site FR/EN sur gh-pages (worktree `MARKETING/`). Regles Chrome/Emby : CLAUDE.md « Emby web UI (Chrome) ».

## Pratiques / pieges
- Teammates : pas d'outil TeamCreate (adressage `team-lead` : voir TEAMMATES_PROTOCOL). Respawn d'un teammate tombe : le nouveau recoit un suffixe (`planner-2`) tant que l'ancien nom est pris.
- Les commits des teammates portent le trailer de leur propre modele (Haiku/Sonnet) : normal, ne pas corriger.
- Teamleader : apres DEPLOY PROD, repasser soi-meme sur `main` (`checkout main` + `pull`) et supprimer la branche milestone, meme sans `BRANCH REQUEST` du deployer ; inclure l'Etape 6 (nettoyage des branches) dans l'ordre PROD. Ne pas oublier en Phase 6 : validation `infra` et dispatch `marketing`.
- kubeconfig Rancher (`private/kubeconfig.yml`) expire : 403 `system:unauthenticated` → l'utilisateur le regenere depuis Rancher.
- Cache images Emby en `.webp` : purger `.jpg` ET `.webp` (CLAUDE.md « Image Cache »). Une playlist sans image n'appelle jamais l'enhancer.
- Boot de `emby`/`emby2` : exceptions IsoMounter/VirtualLib/AudioBookFolderNfoProvider sans lien avec EmbyBadges. Le journal courant n'est pas toujours `embyserver.txt` (chercher « Loading EmbyBadges »).
- Debug serveur actif sur `emby2` depuis le 2026-10-27 (inactif sur `emby`).

## A verifier / prochaine session
- Non verifie en QUALIF (validations utilisateur sans procedure detaillee) : badge sur les playlists a **collage automatique** ; total d'une serie avec episodes manquants (`IsVirtualItem`/`IsUnaired`) ; validation visuelle CA1–CA11 du badge Synchro (v1.3.0).
- Avertissement de build CS0414 : `PlaylistEventsEntryPoint._subscribed` jamais lu (a nettoyer).
- Ecarts infra differes : E4 `scripts/ci.sh` n'execute pas `dotnet test` ; E5 `release.yml` non reduit a `bash scripts/ci.sh` ; epinglage des actions par SHA.
- Site marketing : pas de capture du badge Synchro ni du coeur sur playlist (cartes sans image). URL PROD `emby` non notee.
- Emby : lecture lancee par erreur (« Spider-Man : Brand New Day ») a retirer de « Reprendre la lecture ».
- A signaler au mainteneur du template :
  - `/start-session` cite encore `TEAMMATES_PROTOCOL.md` sans suffixe ; `/milestone` n'a pas de mode « start » ; CDP_WORKFLOWS Phase Init ecrit la version avec `echo > csproj` (adapter : `sed` sur `<Version>`).
  - Phase 6 : rien ne fait repasser le teamleader sur `main` si le deployer n'envoie pas de `BRANCH REQUEST` (Etape 6 de `deploy.template.md`) ; ajouter un controle au `DEPLOY DONE` (« Branches nettoyees : … »).
