# Release v1.4.0 - Vos playlists se voient sur les affiches

**Statut** : brouillon. LOT-1 et LOT-2 sont dans la release ; la validation QUALIF de LOT-2 reste à faire.
**Date** : à définir à la publication
**Version de développement** : 1.4.0 (le numéro de build QUALIF est rapporté par le déploiement)

## Qualité avant publication

- **LOT-1 (partage et favori)** : revue de code approuvée après correctifs des réserves ; QA validée (99/99 tests) ; CI de branche verte ; validée par l'utilisateur sur QUALIF 1.4.0.2 (sans exécution de la procédure détaillée). Point ouvert : « collage automatique » non vérifié.
- **LOT-2 (progression)** : revue de code approuvée (les observations mineures ne bloquent pas) ; tests unitaires 131/131 ; QA validée (réserve sur la procédure levée, e20c809) ; CI en cours ; déploiement QUALIF 1.4.0.3 à faire.

## Nouveautés

### Pictogramme de partage sur les playlists
Une playlist partagée (un propriétaire et au moins un membre) affiche un pictogramme de synchronisation sur sa vignette :
- sarcelle si aucune étiquette PlaySync n'est active ;
- coloré selon le type le plus fort sinon : rouge (supprimé), orange (lu), vert (position).

### Progression de playlist
Un compteur `lus/total` s'affiche sur la vignette d'une playlist. Il compte les médias déjà lus par le **propriétaire** de la playlist :
- gris tant qu'il reste des médias à voir, vert quand tout est lu ;
- les séries et saisons sont développées en épisodes, et un média présent deux fois compte une fois ;
- visible sur toutes les playlists qui ont un propriétaire, partagées ou non.

### Favoris sur les playlists
Le cœur Favoris s'affiche aussi sur les playlists, comme sur les films et les séries. La note n'est jamais affichée sur une playlist.

## Réglages

Dans la page de configuration :
- section « Playlist partagée » : activation, position (défaut : Haut gauche), taille, marge et opacité ;
- section « Progression de playlist » : activation, position (défaut : Bas droite), taille, marge et opacité.

Avec les défauts, le cœur Favoris et le compteur sont tous deux en bas à droite, côte à côte.

## Prérequis

- Pour le pictogramme : une playlist partagée, avec un propriétaire et au moins un membre. Le pictogramme ne dépend pas de PlaySync : sans étiquette PlaySync, il reste sarcelle.
- Pour le compteur : une playlist qui a un propriétaire.

## Limitations

- **Badges globaux** : comme les favoris, l'état est le même pour tous les utilisateurs d'Emby. Le cœur reflète le premier utilisateur, et le compteur reflète le propriétaire de la playlist.
- **Délai** : un changement de partage ou d'étiquette apparaît au plus 5 minutes plus tard. Un visionnage ou une modification de playlist apparaît au plus tôt 30 secondes plus tard, à l'affichage suivant de la vignette.
- **Playlists automatiques** : une playlist générée automatiquement pourrait ne pas recevoir de pictogramme (à confirmer sur le serveur).

## Comment mettre à jour

1. Remplacez `EmbyBadges.dll` à la racine du dossier `plugins/` d'Emby.
2. Redémarrez Emby.
3. **Videz le cache d'images** (`.jpg` et `.webp`) : la clé de rendu change, donc toutes les images badgées sont régénérées une fois. Sans purge, les vignettes redimensionnées peuvent garder l'ancien rendu.
4. Rafraîchissez le navigateur (Ctrl+F5).

## Liens

- [Dépôt](https://github.com/CCoupel/Emby_Badges)
