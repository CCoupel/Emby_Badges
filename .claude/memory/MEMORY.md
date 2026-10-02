# MEMORY — Emby_Badges

## Etat courant
- Version : `<Version>` dans `src/EmbyBadges/EmbyBadges.csproj` (dernier tag : v1.2.0) — format 3 champs, a passer en `X.Y.Z.a` au premier `/build`.
- Template Claude synchronise en `v3.9.0` (2026-10-02, commit `22ec917`), pousse sur `main`.
- CI `release.yml` auditee par `infra` (conforme avec reserves) puis corrigee (commit `a676337`) ; `.gitattributes` LF (`20bb88a`).

## Decisions
- Ancien `/build` (build + kubectl cp sur emby2) converti en BUILD/PUBLISH/DEPLOY : `.claude/agents/deploy.md` + `.claude/agents/environments/{publish,deploy}.{qualif,prod}.md` (compagnons, prevalent sur les `*.template.md`).
- QUALIF = `deployment/emby2`, PROD = `deployment/emby` (namespace `media`). PROD deploye uniquement via `/deploy prod` sur ordre explicite, depuis l'asset de la GitHub Release (CI `release.yml`, tag `vX.Y.Z`).
- `kubectl rollout undo` inoperant (DLL sur volume) : rollback = restaurer `EmbyBadges.dll.prev`.
- Pas de tests automatises ni de CHANGELOG.md.
- Agents : 9 retenus dans la table de CLAUDE.md (pas de dev-backend/dev-frontend).

- CI : le step "Check tag matches csproj version" est une ERREUR (choisie 2026-10-02) : bumper `<Version>` du csproj (actuellement 1.1.3) a la version du tag AVANT de tagger, sinon le build echoue. `gen_config_page.py` execute en CI avant le build.
- `.gitattributes` force LF (`*.yml *.cs *.csproj *.sln *.py *.md *.json`) : le bruit CRLF a disparu de `git status`.
- Ecarts CI differes : somme de controle / nom versionne du livrable, CHANGELOG.md, epinglage des actions par SHA.

## A verifier / prochaine session
- Premier `/build` → `/publish qualif` → `/deploy qualif` : procedures kubectl jamais executees (detection du pod PROD `^emby-` a valider).
- Working tree propre hors `.claude/settings.local.json` (reglage local, volontairement non commite). Les anciens "fichiers modifies" n'etaient que du bruit CRLF.
- Avant le prochain tag : bumper le csproj ; `actionlint` non installe, syntaxe du workflow non verifiee hors `yaml.safe_load`.
- Stash ancien `stash@{0}` (feat/initial-scaffold) toujours present.
