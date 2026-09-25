# Publish PROD — adaptations projet Emby_Badges

> Compagnon de `publish.prod.template.md` (mode `rebuild-ci`) : **prevaut** sur le template.
> La CI (`.github/workflows/release.yml`) se declenche sur le tag `vX.Y.Z` (3 champs, sans `a`),
> reconstruit avec `dotnet build -p:Version=X.Y.Z` et publie une GitHub Release contenant `dist/EmbyBadges.dll`.

Differences avec le template :

- **Version** : lire `<Version>` du csproj (pas `cat`), garder les 3 premiers champs : `VERSION=X.Y.Z`. La reecrire dans la balise `<Version>` et committer avant le tag.
- **Pas de branche milestone** : le projet travaille sur `main`. Remplacer l'etape 2 par `git push origin main` (verifier que le commit de version est bien pousse) ; pas de `git merge`, pas de suppression de branche distante (Etape 6 de DEPLOY sans objet). Si une branche `milestone/vX.Y.Z` est effectivement utilisee pour le cycle, revenir au flux du template.
- **CHANGELOG** : verification uniquement si `CHANGELOG.md` existe (il est absent aujourd'hui ; le README et les release notes generees par la CI en tiennent lieu) — sinon verifier que le `README.md` reflete les changements.
- **CI** : `gh run watch` sur le workflow `Release`. La CI ne passe par aucun test (aucune suite) : un echec est presque toujours CODE (compilation) ou CONFIG.
- **Artefact publie** : `https://github.com/CCoupel/Emby_Badges/releases/tag/vX.Y.Z`, asset `EmbyBadges.dll`. C'est l'entree de `deploy.prod.md`.
- **Rollback** : identique au template, avec suppression de la Release creee (`gh release delete vX.Y.Z --yes`) en plus du tag.
