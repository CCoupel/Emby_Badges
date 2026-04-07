# Emby Badges

Plugin Emby Media Server qui superpose des badges visuels sur les vignettes de la bibliothèque.

## Badges disponibles

| Groupe | Badges |
|--------|--------|
| Résolution | SD, HD (720p), Full HD (1080p), 4K |
| Langue audio | Français, Anglais, VO (langue inconnue) |
| Versions multiples | Initiale du connecteur VirtualLib ou "MULTI" |
| Favoris | Icône cœur (si l'item est en favori) |

Chaque groupe dispose de paramètres indépendants : position, taille, marge, opacité. Les badges individuels dans un groupe peuvent être activés/désactivés séparément.

## Positions disponibles

8 positions : TopLeft, TopCenter, TopRight, CenterLeft, CenterRight, BottomLeft, BottomCenter, BottomRight.

## Intégration VirtualLib

Quand le plugin VirtualLib est installé, le badge multi-version affiche l'initiale du connecteur source de chaque version. Le déclencheur est configurable :
- **Uniquement si versions multiples** (comportement par défaut)
- **Toujours si le média provient de VirtualLib** (même version unique)

## Build

```bash
"C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe" build \
  src/EmbyBadges/EmbyBadges.csproj --configuration Release --output dist/
```

## Installation

Copier `dist/EmbyBadges.dll` dans le dossier `plugins/` du serveur Emby (à la racine, pas dans un sous-dossier).

## Développement

Voir [CLAUDE.md](CLAUDE.md) pour les instructions de build, déploiement et architecture.
