# Release v1.4.0 - Vos playlists partagées se voient sur les affiches

**Statut** : brouillon (LOT-1 seulement). La section progression (LOT-2) sera ajoutée après son livraison.
**Date** : à définir à la publication
**Version de développement** : 1.4.0 (le numéro de build QUALIF est rapporté par le déploiement)

## Qualité avant publication

- **Tests unitaires** : 99/99 réussis (`dotnet test`, dernier commit de correction LOT-1).
- **Revue de code et QA sur QUALIF** : à compléter avant publication.

## Nouveautés

### Pictogramme de partage sur les playlists
Une playlist partagée (un propriétaire et au moins un membre) affiche un pictogramme de synchronisation sur sa vignette :
- sarcelle si aucune étiquette PlaySync n'est active ;
- coloré selon le type le plus fort sinon : rouge (supprimé), orange (lu), vert (position).

### Favoris sur les playlists
Le cœur Favoris s'affiche aussi sur les playlists, comme sur les films et les séries. La note n'est jamais affichée sur une playlist.

## Réglages

Dans la page de configuration, section « Playlist partagée » :
- activation de l'indicateur, position (défaut : Haut gauche), taille, marge et opacité.

## Prérequis

- Une playlist partagée, avec un propriétaire et au moins un membre.
- Sans partage, aucune playlist n'affiche de pictogramme. Le pictogramme ne dépend pas de PlaySync : sans étiquette PlaySync, il reste sarcelle.

## Limitations

- **Badges globaux** : comme les favoris, l'état est le même pour tous les utilisateurs d'Emby. Le cœur reflète le premier utilisateur.
- **Playlists automatiques** : une playlist générée automatiquement pourrait ne pas recevoir de pictogramme (à confirmer sur le serveur).
- **Délai** : un changement de partage ou d'étiquette apparaît au plus 5 minutes plus tard.

## Comment mettre à jour

1. Remplacez `EmbyBadges.dll` à la racine du dossier `plugins/` d'Emby.
2. Redémarrez Emby.
3. **Videz le cache d'images** (`.jpg` et `.webp`) : la clé de rendu change, donc toutes les images badgées sont régénérées une fois. Sans purge, les vignettes redimensionnées peuvent garder l'ancien rendu.
4. Rafraîchissez le navigateur (Ctrl+F5).

## Liens

- [Dépôt](https://github.com/CCoupel/Emby_Badges)
