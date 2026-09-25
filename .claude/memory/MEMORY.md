# MEMORY — Emby_Badges

## Etat courant
- Version : `<Version>` dans `src/EmbyBadges/EmbyBadges.csproj` (dernier tag : v1.2.0) — format 3 champs, a passer en `X.Y.Z.a` au premier `/build`.
- Projet initialise depuis le template Claude `v3.3.1` (2026-09-25), commits `bccc5c7` et `abd1b56`, pousses sur `main`.

## Decisions
- Ancien `/build` (build + kubectl cp sur emby2) converti en BUILD/PUBLISH/DEPLOY : `.claude/agents/deploy.md` + `.claude/agents/environments/{publish,deploy}.{qualif,prod}.md` (compagnons, prevalent sur les `*.template.md`).
- QUALIF = `deployment/emby2`, PROD = `deployment/emby` (namespace `media`). PROD deploye uniquement via `/deploy prod` sur ordre explicite, depuis l'asset de la GitHub Release (CI `release.yml`, tag `vX.Y.Z`).
- `kubectl rollout undo` inoperant (DLL sur volume) : rollback = restaurer `EmbyBadges.dll.prev`.
- Pas de tests automatises ni de CHANGELOG.md.
- Agents : 9 retenus dans la table de CLAUDE.md (pas de dev-backend/dev-frontend).

## A verifier / prochaine session
- Premier `/build` → `/publish qualif` → `/deploy qualif` : procedures kubectl jamais executees (detection du pod PROD `^emby-` a valider).
- Working tree : fichiers modifies (src/, .sln, README, release.yml, dist deps.json) = probablement fins de ligne (CRLF) uniquement, non commites ; notes anterieures mentionnent un cumul res/langue par serie en cours.
- Stash ancien `stash@{0}` (feat/initial-scaffold) toujours present.
