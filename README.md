# Emby Badges

Plugin Emby Media Server qui superpose des badges visuels sur les vignettes de la bibliothèque.

## Badges disponibles

| Groupe | Badges |
|--------|--------|
| Résolution | SD, HD (720p), Full HD (1080p), 4K |
| Langue audio | Français, Anglais, VO (langue inconnue) |
| Versions multiples | Initiale du connecteur VirtualLib ou "MULTI" |
| Favoris | Icône cœur (si l'item est en favori) |
| Synchro (PlaySync) | `P` position, `L` lu, `S` supprimé — en lettres, ou pictogramme coloré (voir ci-dessous) |

Chaque groupe dispose de paramètres indépendants : position, taille, marge, opacité. Les badges individuels dans un groupe peuvent être activés/désactivés séparément.

## Positions disponibles

8 positions : TopLeft, TopCenter, TopRight, CenterLeft, CenterRight, BottomLeft, BottomCenter, BottomRight.

## Intégration VirtualLib

Quand le plugin VirtualLib est installé, le badge multi-version affiche l'initiale du connecteur source de chaque version. Le déclencheur est configurable :
- **Uniquement si versions multiples** (comportement par défaut)
- **Toujours si le média provient de VirtualLib** (même version unique)

## Synchro (PlaySync)

Le badge Synchro indique l'état de synchronisation d'un film ou d'un épisode présent dans une **playlist partagée** portant une étiquette PlaySync :

| Lettre | Type | Étiquette PlaySync active |
|--------|------|---------------------------|
| `P` | Position | `propager-avancement=OUI` |
| `L` | Lu | `propager-lu=OUI` |
| `S` | Supprimé | `remove-si-lu=OUI` **et** `propager-lu=OUI` |

- Les types se cumulent sur toutes les playlists partagées qui contiennent l'item (`PLS` si les trois).
- Une série ou une saison reçoit les types de ses épisodes.
- Une étiquette `NON` l'emporte sur `OUI`. Les playlists non partagées sont ignorées.
- **Modes de rendu** : lettres (`PLS`, fond sarcelle) ou pictogramme coloré. En mode couleur, la couleur suit le type le plus fort : rouge (supprimé), orange (lu), vert (position).
- **Options** : activation du groupe, position (défaut Haut gauche), taille, marge, opacité, mode de rendu, et activation de chaque type.

Limites :
- Le plugin ne lit que les étiquettes : sans PlaySync, aucun badge Synchro n'apparaît.
- Les badges sont globaux, comme les favoris : tous les utilisateurs voient le même état.
- Un changement de playlist ou d'étiquette apparaît au plus 5 minutes plus tard, puis Emby doit régénérer les images (vider le cache d'images d'Emby pour un rafraîchissement immédiat).

## Build

```bash
"C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe" build \
  src/EmbyBadges/EmbyBadges.csproj --configuration Release --output dist/
```

## Installation

Copier `dist/EmbyBadges.dll` dans le dossier `plugins/` du serveur Emby (à la racine, pas dans un sous-dossier).

## Développement

Voir [CLAUDE.md](CLAUDE.md) pour les instructions de build, déploiement et architecture.
