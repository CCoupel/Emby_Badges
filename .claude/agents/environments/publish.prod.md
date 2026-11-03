# Publish PROD — adaptations projet Emby_Badges

> Compagnon de `publish.prod.template.md` (mode `rebuild-ci`) : **prevaut** sur le template.
> La CI (`.github/workflows/release.yml`) se declenche sur le tag `vX.Y.Z` (3 champs, sans `a`),
> reconstruit avec `dotnet build -p:Version=X.Y.Z` et publie une GitHub Release contenant `dist/EmbyBadges.dll`.

Differences avec le template (le cycle passe par la branche `milestone/vX.Y.Z`, mergee dans `main` par le flux du template) :

- **Version** : lire `<Version>` du csproj (pas `cat`), garder les 3 premiers champs : `VERSION=X.Y.Z`. La reecrire dans la balise `<Version>` et committer avant le tag.
- **CHANGELOG** : verification uniquement si `CHANGELOG.md` existe (il est absent aujourd'hui ; le README et les release notes generees par la CI en tiennent lieu) — sinon verifier que le `README.md` reflete les changements.
- **CI** : `gh run watch` sur le workflow `Release`. La CI ne lance pas les tests xUnit (`tests/EmbyBadges.Tests/`, deja valides par QA ; ecart E4 : `scripts/ci.sh` n'execute pas `dotnet test`) : un echec est presque toujours CODE (compilation) ou CONFIG.
- **Artefact publie** : `https://github.com/CCoupel/Emby_Badges/releases/tag/vX.Y.Z`, asset `EmbyBadges.dll`. C'est l'entree de `deploy.prod.md`.
- **Rollback** : identique au template, avec suppression de la Release creee (`gh release delete vX.Y.Z --yes`) en plus du tag.
