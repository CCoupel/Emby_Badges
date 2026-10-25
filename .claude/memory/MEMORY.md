# MEMORY — Emby_Badges

## Etat courant
- Version : PROD `v1.3.0` livree le 2026-10-22 sur `emby` (tag `v1.3.0`, merge `e7aa232`, Release GitHub avec `EmbyBadges.dll` 672256 o). QUALIF `emby2` a la meme DLL (1.3.0.1). csproj `<Version>` = `1.3.0` sur `main` (le prochain milestone repasse en `X.Y.Z.0` a l'ouverture de sa branche).
- Milestone `v1.3.0` (n°1) CLOS — issue #4 (badge Synchro PlaySync) fermee. Aucune issue ouverte, aucun milestone ouvert. Branche `main` = `e7aa232`, poussee. Branches `milestone/v1.3.0`, `feat/*` supprimees ; `gh-pages` conservee.
- Template Claude `v3.27.0` (synchronise le 2026-10-25, commit `a38f01d`). CI `ci-branch.yml` desormais executee et verte sur `milestone/**`.
- Premier projet de tests : `tests/EmbyBadges.Tests/` (xUnit, 61 tests, logique pure du badge Synchro) ; `commands.test` renseigne dans `project-config.json`. Procedure manuelle `tests/procedures/badges/synchro/synchro-qualif.md` (CA1–CA11).
- Site marketing v1.2.0 (gh-pages) : les release notes v1.3.0 (`docs/releases/v1.3.0/release-notes.md`) ne sont PAS publiees sur le site ; posts v1.2.0 jamais envoyes.

## Decisions
- Feature #4 (v1.3.0) : badge « Synchro » = 6e groupe de badges, source = etiquettes PlaySync (`remove-si-lu`, `propager-lu`, `propager-avancement`) lues comme `Tags` des playlists Emby partagees via SDK seul (`IItemRepository.GetUserItemShares`, pas de dependance a la DLL PlaySync). Union des types sur toutes les playlists ; « supprime » = etat effectif (remove ET propager-lu) ; modes de rendu Lettres `PLS` (fond sarcelle) ou Couleur (rouge supprime > orange lu > vert position, pictogramme vectoriel SkiaSharp) ; position par defaut TopLeft ; index memoire TTL 5 min, 1er build bloquant puis rebuild en tache de fond ; propagation episode → saison → serie seulement. Contrat : `contracts/models.md`. Images globales (meme limitation que Favoris).
- `IItemRepository` injecte directement dans le constructeur de `BadgeEnhancer` (sans repli) : verifie OK en QUALIF et PROD (plugin charge sans exception).
- Ancien `/build` converti en BUILD/PUBLISH/DEPLOY : `.claude/agents/deploy.md` + `environments/{publish,deploy}.{qualif,prod}.md`. Procedures executees de bout en bout pour la 1re fois le 2026-10-21/22 : OK (sous WSL : `python3` + `/mnt/c/.../dotnet.exe`, `export KUBECONFIG=private/kubeconfig.yml` ; verifier la DLL via `kubectl exec deployment/<x> -- stat -c %s` car l'ancien pod peut etre encore liste apres rollout).
- Environnements QUALIF/PROD : voir CLAUDE.md § Deploy. Rollback = restaurer `EmbyBadges.dll.prev`.
- CI / bump csproj avant tag : voir CLAUDE.md § Release (fait par le deployer en PUBLISH PROD). `.gitattributes` force LF.
- Pas de CHANGELOG.md (decision maintenue) ; release notes par version dans `docs/releases/vX.Y.Z/`.
- Agents : 9 dans la table CLAUDE.md ; `infra` ponctuel spawne en Phase 5 pour validation QUALIF (lecture seule).
- Marketing : ton fun, site FR/EN sur gh-pages (worktree `MARKETING/`). Captures sur l'Emby de l'utilisateur via Chrome (jamais le tableau de bord ni la cle TMDB).

## Pratiques / pieges
- Teammates : pas d'outil TeamCreate (adressage `team-lead` : voir TEAMMATES_PROTOCOL).
- Les commits des teammates portent le trailer de leur propre modele (Haiku/Sonnet) : normal, ne pas corriger.
- Logs Emby : « SyncIndex reconstruit » est en niveau Debug (`EnableDebugLevelLogging` necessaire pour l'observer). Boot de `emby`/`emby2` : exceptions IsoMounter/VirtualLib (`ILogger<T>`) sans lien avec EmbyBadges.
- Chrome sur Emby : cliquer au centre d'une affiche LANCE la lecture ; ouvrir une fiche par le bord.

## A verifier / prochaine session
- Validation visuelle CA1–CA11 du badge Synchro par l'utilisateur dans l'interface (QUALIF `emby2` : 1 playlist partagee `test`, 2 films ; etiquettes non verifiees) → `/bugfix` si ecart. Cohabitation TopLeft Synchro/Note/MultiVersion a controler a l'oeil.
- Ecarts infra differes (rapport infra 2026-10-21) : E4 `scripts/ci.sh` n'execute pas `dotnet test` ; E5 `release.yml` non reduit a `bash scripts/ci.sh` ; somme de controle / epinglage des actions par SHA. Groupe Note (Rating) ajoute au README/CLAUDE.md seulement en v1.3.0.
- `/marketing v1.3.0` : publier les release notes sur le site gh-pages ; verifier https://ccoupel.github.io/Emby_Badges/ et le rendu mobile.
- Emby : lecture lancee par erreur (« Spider-Man : Brand New Day ») a retirer de « Reprendre la lecture ».
- A signaler au mainteneur du template : chemin `TEAMMATES_PROTOCOL.md` sans suffixe encore cite dans `/start-session` (texte, le prompt de spawn est corrige en v3.27.0) ; `/milestone` n'a pas de mode « start » (demarrer un milestone = `/feature #N`) ; CDP_WORKFLOWS Phase Init ecrit la version avec `echo > csproj` (a adapter : `sed` sur `<Version>`).
- Stash ancien `stash@{0}` (feat/initial-scaffold) toujours present.
