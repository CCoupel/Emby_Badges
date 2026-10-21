# MEMORY — Emby_Badges

## Etat courant
- Version : `<Version>` dans `src/EmbyBadges/EmbyBadges.csproj` = 1.1.3 (dernier tag : v1.2.0) — format 3 champs, a passer en `X.Y.Z.a` au premier `/build`.
- Template Claude synchronise en `v3.23.3` (2026-10-21, registre des agents `agents.registry[]` cree) ; changements NON commites : `CLAUDE.md`, `.claude/project-config.json` (`commands.ci`), `.claude/settings.json`, `.gitignore`, `.claude/commands/init-project.md`, `scripts/ci.sh`, `.github/workflows/ci-branch.yml`, `contracts/`.
- Site marketing v1.2.0 publie le 2026-10-08 : gh-pages `87502e9`, release notes + posts sur main `1d5085c` (`docs/releases/v1.2.0/`). GitHub Pages actif (source gh-pages `/`), URL https://ccoupel.github.io/Emby_Badges/ ; etait en "building" (404) a la cloture : a reverifier. Posts X/LinkedIn/Reddit rediges mais PAS envoyes.
- CI `release.yml` auditee par `infra` (conforme avec reserves) puis corrigee (commit `a676337`) ; `.gitattributes` LF (`20bb88a`).

## Decisions
- Ancien `/build` (build + kubectl cp sur emby2) converti en BUILD/PUBLISH/DEPLOY : `.claude/agents/deploy.md` + `.claude/agents/environments/{publish,deploy}.{qualif,prod}.md` (compagnons, prevalent sur les `*.template.md`).
- QUALIF = `deployment/emby2`, PROD = `deployment/emby` (namespace `media`). PROD deploye uniquement via `/deploy prod` sur ordre explicite, depuis l'asset de la GitHub Release (CI `release.yml`, tag `vX.Y.Z`).
- `kubectl rollout undo` inoperant (DLL sur volume) : rollback = restaurer `EmbyBadges.dll.prev`.
- Pas de tests automatises ni de CHANGELOG.md.
- Agents : 9 retenus dans la table de CLAUDE.md (pas de dev-backend/dev-frontend).
- CI : le step "Check tag matches csproj version" est une ERREUR (choisie 2026-10-02) : bumper `<Version>` du csproj a la version du tag AVANT de tagger, sinon le build echoue. `gen_config_page.py` execute en CI avant le build. (Regle aussi dans CLAUDE.md, section Release.)
- `.gitattributes` force LF (`*.yml *.cs *.csproj *.sln *.py *.md *.json`).
- Ecarts CI differes : somme de controle / nom versionne du livrable, CHANGELOG.md, epinglage des actions par SHA.
- Plugin : Primary et Thumb badges pour Movie/Episode/Series/Season ; Backdrop badge pour Episode/Series/Season, PAS pour Movie. Cas visible verifie : vignettes paysage "Reprendre la lecture". Le fond plein ecran des fiches (ex. Ranma ½) n'a PAS de badge observe : cause non elucidee (comportement Emby, cache ou defaut plugin). Le site ne promet rien sur le fond des fiches ni des films.
- Marketing : ton fun et ludique, site FR/EN sur gh-pages (worktree `MARKETING/`, exclu via `.git/info/exclude` et `.gitignore`). Captures reelles prises sur l'Emby de l'utilisateur (http://media.local/emby) via Chrome.

## Pratiques / pieges
- Teammates : l'alias `main` est refuse dans leurs sessions ; ils repondent a `team-lead`. Pas d'outil TeamCreate : agents spawnes par nom canonique.
- Chrome sur Emby : cliquer au centre d'une affiche LANCE la lecture. Ouvrir une fiche en cliquant le bord de l'affiche ; ne jamais capturer le tableau de bord (IP, activite) ni le champ cle TMDB / le profil du menu lateral.

## A verifier / prochaine session
- Verifier que https://ccoupel.github.io/Emby_Badges/ s'affiche (build Pages) et le rendu mobile du site (non controle visuellement).
- Emby : une lecture a ete lancee par erreur (probablement "Spider-Man : Brand New Day") pendant la prise de captures : verifier/supprimer l'entree dans "Reprendre la lecture".
- Premier `/build` → `/publish qualif` → `/deploy qualif` : procedures kubectl jamais executees (detection du pod PROD `^emby-` a valider).
- Avant le prochain tag : bumper le csproj ; `ci-branch.yml` jamais execute sur GitHub (a pousser puis `gh workflow run ci-branch.yml`) ; `actionlint` non installe, syntaxe du workflow non verifiee hors `yaml.safe_load`.
- A signaler au mainteneur du template : alias `main` non resolu pour les teammates ; contextes referencent `context/PROJECT_CONTEXT.md` (compagnon cree localement).
- Stash ancien `stash@{0}` (feat/initial-scaffold) toujours present.
