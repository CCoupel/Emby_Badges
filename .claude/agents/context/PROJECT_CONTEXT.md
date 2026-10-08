# PROJECT_CONTEXT — Emby_Badges

> Contexte technique court, référencé par `context/COMMON.md`, `DEV_COMMON.md`, `VALIDATION_COMMON.md` et
> `DEVELOPMENT.md`. Source de vérité détaillée : `CLAUDE.md` (racine) — ne pas dupliquer ici.

## Stack

- Plugin Emby Media Server, C# / .NET 6 (`net6.0`), rendu d'image SkiaSharp 2.88.6.
- Pas de backend/frontend séparés, pas de base de données. Page de configuration = fragment HTML + module AMD
  générés par `gen_config_page.py`.
- SDK Emby dans `libs/` (commité pour la CI). Agent dev : `dev-plugin`.

## Structure

- `src/EmbyBadges/` : `Plugin.cs`, `PluginConfiguration.cs`, `Enhancer/` (`BadgeEnhancer`, `BadgeDataExtractor`),
  `ImageProcessing/` (`BadgeRenderer`, `IconLoader`), `Configuration/` (généré), `Icons/`.
- `build/` : artefacts BUILD/PUBLISH (`candidate_vX.Y.Z/`, `qualif_vX.Y.Z/`), gitignoré.
- `dist/` : sortie de build locale rapide, jamais publiée.

## Commandes

- Build : `commands.build` de `.claude/project-config.json` (`gen_config_page.py` puis `dotnet build` Release).
- Tests automatisés : aucun (`commands.test` vide). Validation fonctionnelle en QUALIF (`emby2`) par l'utilisateur.
- Version : `<Version>` de `src/EmbyBadges/EmbyBadges.csproj` ; tag `vX.Y.Z` = csproj (contrôlé par la CI).
- Déploiement : `/build` → `/publish qualif` → `/deploy qualif` (→ `/publish prod` → `/deploy prod`, sur ordre explicite).
  Procédures : `.claude/agents/deploy.md` et `.claude/agents/environments/`.
