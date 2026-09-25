# Deploy — adaptations projet Emby_Badges

> Compagnon de `deploy.template.md`. Les regles ci-dessous **prevalent** sur le template.
> Origine : conversion de l'ancien `/build` du projet (build + kubectl cp sur `emby2`) vers le
> modele BUILD / PUBLISH / DEPLOY. Procedures par environnement : `environments/{publish,deploy}.<env>.md`.

## Contexte projet

- Artefact unique : `EmbyBadges.dll` (plugin Emby, `net6.0`). Pas de Docker, pas de tarball.
- Outillage (hors PATH) : `DOTNET="C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe"`, `python` (WSL/Windows).
- Pas de tests automatises (`commands.test` vide) : le prerequis "tests" du template est sans objet.
  La validation fonctionnelle se fait en QUALIF (instance `emby2`) par l'utilisateur.
- Version : `<Version>` de `src/EmbyBadges/EmbyBadges.csproj` (`version_file` du project-config). Format historique
  a 3 champs (`1.2.0`) : le lire comme `X.Y.Z.0`, et ecrire au format `X.Y.Z.a` (4 champs, valide pour .NET).
  Extraction : `sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/EmbyBadges/EmbyBadges.csproj`
  (ne jamais faire `cat` du fichier, contrairement aux exemples du template). Ecriture : remplacer
  uniquement le contenu de la balise `<Version>`.
- Le projet travaille directement sur `main` (pas de branche `milestone/vX.Y.Z`) sauf mention contraire.

## Tache BUILD (remplace l'etape 3 du template)

Les etapes 1-2 (verification `git status` propre, bump `a+1`, commit `chore(version)`, push) restent celles
du template, sans `npm test`. L'etape 3 devient :

```bash
DOTNET="C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe"
REPO_ROOT=$(git rev-parse --show-toplevel)
BUILD_DIR="$REPO_ROOT/build/candidate_v$DIR_VERSION"
mkdir -p "$BUILD_DIR"

# 3a. Pages de configuration generees (icones PNG inlinees en base64) — obligatoire avant compilation
python gen_config_page.py

# 3b. Compilation Release, version injectee. Sortie dans un dossier temporaire du candidat :
#     ne pas ecrire dans dist/ (dist/ est suivi par git pour EmbyBadges.deps.json)
"$DOTNET" build src/EmbyBadges/EmbyBadges.csproj --configuration Release \
  -p:Version=$VERSION --output "$BUILD_DIR/tmp"
# Verifier "0 Erreur(s)" — sinon BUILD FAILED (Probleme : compilation)

# 3c. Artefact candidat, nomme avec le `a`
cp "$BUILD_DIR/tmp/EmbyBadges.dll" "$BUILD_DIR/EmbyBadges-$VERSION.dll"
rm -rf "$BUILD_DIR/tmp"
```

Notification : `Candidat : build/candidate_v$DIR_VERSION/EmbyBadges-$VERSION.dll`. Ajouter la taille en octets
(`stat -c %s`) — elle sert a la verification en DEPLOY.

> Un build hors process (developpement local rapide) reste possible avec la commande de `CLAUDE.md`
> (`--output dist/`), mais n'est jamais publie ni deploye.

## Variables d'environnement

Les acces cluster sont dans `private/kubeconfig.yml` (gitignore, hors `.claude/`). Les fichiers
`environments/<env>.env` ne portent donc que `KUBE_CONTEXT`/`KUBECONFIG` — voir chaque `deploy.<env>.md`.

## Regle absolue

**Ne jamais deployer sur `emby` (production) sans ordre explicite de l'utilisateur** dans le message courant
(voir `environments/deploy.prod.md`). Le CDP ne l'enchaine jamais automatiquement.
