# Release v1.3.0 - Vos affiches indiquent où en est la synchronisation

**Date** : à définir à la publication

## Nouveautés

### Badge Synchro (PlaySync)
Un badge indique si un film ou un épisode est suivi par PlaySync dans une playlist partagée :
- `P` : position synchronisée ;
- `L` : marqué comme lu ;
- `S` : supprimé (effectif : retrait actif et propagation du statut « lu »).

Les types se cumulent (`PLS` si les trois). Une série ou une saison reçoit les types de ses épisodes.

### Deux modes de rendu
- **Lettres** : `P`, `L`, `S` ou une combinaison, sur fond sarcelle.
- **Pictogramme coloré** : une icône de synchronisation, colorée selon le type le plus fort (rouge : supprimé, orange : lu, vert : position).

### Réglages
Dans la page de configuration, section « Synchro (PlaySync) » :
- activation du groupe, et de chaque type (position, lu, supprimé) ;
- mode de rendu (lettres ou pictogramme) ;
- position (défaut : Haut gauche), taille, marge et opacité, comme les autres groupes.

## Prérequis

- PlaySync installé, avec des playlists partagées (propriétaire et au moins un membre) portant les étiquettes `propager-avancement`, `propager-lu` ou `remove-si-lu`.
- Sans PlaySync, ou sans playlist étiquetée, aucun badge Synchro n'apparaît et les autres badges ne changent pas.

## Limitations

- **Badges globaux** : comme les favoris, l'état est le même pour tous les utilisateurs d'Emby.
- **Délai de 5 minutes** : un ajout ou retrait dans une playlist, ou un changement d'étiquette, est visible au plus 5 minutes plus tard.
- **Playlists uniquement** : une saison ou une série placée directement dans une playlist ne transmet pas ses types à sa série parente ; ce sont les épisodes qui remontent.

## Comment mettre à jour

1. Remplacez `EmbyBadges.dll` à la racine du dossier `plugins/` d'Emby.
2. Redémarrez Emby.
3. **Videz le cache d'images** : la clé de rendu change, donc toutes les images badgées sont régénérées une fois. Sans purge, les vignettes redimensionnées peuvent garder l'ancien rendu.
4. Rafraîchissez le navigateur (Ctrl+F5).

## Liens

- [Dépôt](https://github.com/CCoupel/Emby_Badges)
