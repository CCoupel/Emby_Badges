# Procédure de Test — Badges de playlist, LOT-2 : progression « lus/total » (#6)

**Version** : 1.4.0
**Date** : 2026-10-27
**Testeur** : QA
**Plan** : `.claude/workflow/plan-v1.4.0.md` — **Contrat** : `contracts/models.md` §8, §11–13

## Prérequis

- [ ] Environnement : QUALIF (`emby2`, namespace `media`), DLL candidate 1.4.0 (LOT-2) déployée via `/deploy qualif`
- [ ] `export KUBECONFIG=private/kubeconfig.yml`
- [ ] 2 utilisateurs Emby : `U1` (propriétaire, premier utilisateur) et `U2` (membre)
- [ ] Médias : films `F1`..`F5` ; une série `SER` de 3 épisodes `E1`..`E3`
- [ ] Playlists (créées par U1) :
  - `PL-P` partagée avec U2, contenant `F1`, `F2`, `F3`, `F4` ; U1 a vu `F1` et `F2` seulement (attendu 2/4)
  - `PL-NP` **non partagée**, contenant `F1`, `F2` (U1 a vu `F1`)
  - `PL-DUP` partagée, contenant `F1` **deux fois** et `F3`
  - `PL-SER` contenant la série `SER` (3 épisodes ; U1 en a vu 1) et `F4`
  - `PL-MANQ` contenant une série `SER-M` avec des épisodes **manquants** ou **non diffusés** (au moins 1 de chaque si possible) + `F4`
  - `PL-VIDE` playlist vide
  - `PL-ALL` contenant uniquement des médias entièrement vus par U1
  - `PL-RM` partagée, `remove-si-lu=OUI` + `propager-lu=OUI`, contenant `F5` non vu
  - `PL-FAV` playlist favorite de U1 (cohabitation avec le cœur)
- [ ] U2 n'a vu **aucun** de ces médias (pour distinguer le propriétaire du membre)
- [ ] Config plugin : `ShowPlaylistProgress` actif, `ShowFavorites` actif, positions par défaut (progression `BottomRight`)

**Logs de reconstruction (CA11, CA12, CA13)** : le message `SyncIndex reconstruit : N items, M playlists, X ms` est émis
au niveau **Debug du serveur Emby**, via le logger de `SyncDetector`. Activer le journal Debug du serveur
(Tableau de bord > Journaux > niveau Debug, ou `Logging` Debug côté Emby) ; l'option `DebugMode` du plugin n'agit
que sur l'overlay des images, **pas** sur ces logs. Lire :

```bash
kubectl logs -n media deployment/emby2 --since=2m | grep -i "SyncIndex"
```

**Purge du cache images — à faire entre CHAQUE observation visuelle** (`.jpg` ET `.webp`) :

```bash
kubectl exec -n media deployment/emby2 -- find /config/cache/images \( -name "*.jpg" -o -name "*.webp" \) -delete
```

Puis rechargement forcé du client web (Ctrl+F5). Les lectures de log servent à mesurer les délais (le rafraîchissement visuel dépend de `resized-images`).

## Scénarios

### CA11-pré — Entry point d'événements actif au démarrage (prérequis de CA11/CA12)

**Validation** : AUTO — **Issues** : #6 (lecture de log, aucun jugement humain)

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Journal Debug serveur actif, redémarrer le pod `emby2` | — | | |
| 2 | `kubectl logs -n media deployment/emby2 \| grep "PlaylistEventsEntryPoint"` | Ligne `EmbyBadges: PlaylistEventsEntryPoint actif (abonnements posés)` présente | | |
| 3 | `kubectl logs -n media deployment/emby2 \| grep -i "EmbyBadges" \| grep -i "warn"` | Aucun Warn d'injection de dépendances (`IPlaylistManager`, `IUserDataManager`, `IServerEntryPoint`) | | |

**Verdict** : [ ] PASS  [ ] FAIL — Si la ligne est absente, CA11 et CA12 ne sont pas évaluables : arrêter et remonter.

---

### CA8 — Badge « lus/total » du propriétaire, identique pour tous

**Validation** : HUMAIN — **Issues** : #6

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Purger, se connecter en `U1`, ouvrir la page Playlists | `PL-P` affiche **2/4** en bas à droite | | |
| 2 | Observer `PL-NP` (non partagée) | **1/2** (la progression n'exige pas le partage) | | |
| 3 | Se connecter en `U2`, mêmes playlists | `PL-P` affiche **2/4** (état du propriétaire, pas celui de U2) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA9 — Règles de comptage

**Validation** : HUMAIN — **Issues** : #6

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Observer `PL-DUP` (`F1` en double + `F3`) | **1/2** : le doublon compte une fois | | |
| 2 | Observer `PL-SER` (série de 3 épisodes dont 1 vu + `F4`) | **1/4** : la série compte pour ses 3 épisodes | | |
| 3 | Observer `PL-VIDE` | Aucun badge de progression | | |
| 4 | Observer `PL-MANQ` (série `SER-M` dont certains épisodes sont **manquants** ou **non diffusés** : épisodes virtuels / date de diffusion future, + `F4`) | Le total ne compte que les épisodes réels (filtre `IsVirtualItem=false` et `IsUnaired=false`) : ex. 3 épisodes réels + 2 manquants/non diffusés + `F4` → total **4**, pas 6 | | |
| 5 | Playlist sans propriétaire (ex. playlist dont la ligne de partage propriétaire a été supprimée, si réalisable) | Ni progression ni pictogramme de partage | | |

**Verdict** : [ ] PASS  [ ] FAIL — Étape 5 : [ ] réalisée  [ ] non réalisable (noter pourquoi)

---

### CA10 — Fond vert quand tout est lu

**Validation** : HUMAIN — **Issues** : #6

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Observer `PL-ALL` | Badge **N/N** sur fond **vert** | | |
| 2 | Observer `PL-P` (2/4) | Badge sur fond **gris** | | |
| 3 | En `U1`, marquer `F3` et `F4` comme lus, attendre ≤ 1 min, purger, recharger | `PL-P` passe à **4/4**, fond vert | | |
| 4 | Remarquer `F3` et `F4` non lus (remise à l'état initial) | `PL-P` revient à 2/4 gris | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA11 — Mise à jour ≤ ~1 min après un événement

**Validation** : HUMAIN — **Issues** : #6 (mesure par log + rendu)

**Latence attendue : « ≥ 30 s puis 2 affichages de la vignette »** (reconstruction paresseuse). L'événement ne
fait que marquer l'index « dirty » ; la reconstruction n'est lancée que lorsqu'une vignette est demandée
**et** que 30 s se sont écoulées. Donc : (a) attendre **≥ 30 s** après l'action ; (b) un **premier affichage** de la
vignette déclenche la reconstruction en tâche de fond et sert encore l'ancienne valeur (ancien instantané) ;
(c) un **second affichage**, après la fin de la reconstruction (ligne de log), montre la nouvelle valeur. Sans
affichage de vignette, aucune ligne `SyncIndex reconstruit` n'apparaît : ce n'est pas un défaut.

Pour chaque événement : relever l'heure `T0` de l'action, puis l'heure de la ligne `SyncIndex reconstruit` suivante.
Attendu : après ≥ 30 s et un premier affichage de la vignette, reconstruction (≤ ~1 min au total), sans attendre les 5 min ; la valeur à jour apparaît au second affichage (avec purge `.jpg`+`.webp`).

| Etape | Action (en `U1`) | Résultat Attendu | Résultat Obtenu (heure log, Δ) | OK ? |
|-------|-----------------|-----------------|-------------------------------|------|
| 1 | Lire un média de `PL-P` **jusqu'à la fin** (fin de lecture) | Après ≥ 30 s : ouvrir la page Playlists (1er affichage), ligne `SyncIndex reconstruit` ; purger, rouvrir (2e affichage) : compteur +1 | | |
| 2 | Marquer un média de `PL-P` **lu**, puis **non lu** (deux actions espacées de > 1 min) | Une reconstruction ≤ ~1 min après chaque action | | |
| 3 | Ajouter un média à `PL-P` | Reconstruction ≤ ~1 min ; total +1 | | |
| 4 | Retirer un média de `PL-P` | Reconstruction ≤ ~1 min ; total −1 | | |
| 5 | Lire `F5` de `PL-RM` jusqu'à la fin (retrait automatique par `remove-si-lu`) | Reconstruction ≤ ~1 min ; `F5` n'est plus compté | | |
| 6 | Pour chaque étape, purger le cache et recharger | Nouvelle valeur visible (si l'ancienne persiste malgré la purge `.jpg`+`.webp`, noter : limitation S2) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA12 — La progression de lecture ne reconstruit pas

**Validation** : HUMAIN — **Issues** : #6

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Attendre qu'aucune reconstruction ne soit en cours (log calme ≥ 6 min, ou noter l'heure de la dernière) | — | | |
| 2 | En `U1`, démarrer la lecture d'un média de `PL-P`, la laisser tourner 2 min **sans la terminer**, puis arrêter | — | | |
| 3 | `kubectl logs -n media deployment/emby2 --since=3m \| grep -i "SyncIndex"` | **Aucune** ligne `SyncIndex reconstruit` provoquée par la lecture (hors TTL de 5 min éventuel) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA13 — Désactivation et réglages ; aucune lecture inutile

**Validation** : HUMAIN — **Issues** : #6

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Config : décocher `ShowPlaylistProgress`, sauvegarder, purger, recharger | Aucun badge de progression sur les playlists | | |
| 2 | Avec `ShowSync` et `ShowPlaylistShare` actifs, attendre une reconstruction (log) | Reconstruction sans lecture d'enfants pour les playlists non étiquetées ou non partagées (pas de ligne d'erreur ; durée courte) | | |
| 3 | Recocher ; régler position (`BottomLeft`), taille, marge et opacité du groupe « Progression de playlist » ; purger, recharger | Réglages appliqués | | |
| 4 | Ouvrir la config du plugin | Section « Progression de playlist » présente ; valeurs sauvegardées et rechargées | | |
| 5 | Remettre `BottomRight` et valeurs par défaut | — | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA14 — Non-régression et latence Synchro (critical)

**Validation** : HUMAIN — **Issues** : #6

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Rejouer `playlist-lot1-qualif.md` CA5 (badges des médias identiques à v1.3.0, Synchro inclus) | Identiques | | |
| 2 | Retirer un film d'une playlist PlaySync étiquetée, observer le log | Reconstruction ≤ ~30 s–1 min après l'événement ; sinon au plus 5 min | | |
| 3 | Vérifier que le badge Synchro du film se met à jour après purge | Latence ≤ celle de v1.3.0 | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Cohabitation progression + cœur Favoris en BottomRight

**Validation** : HUMAIN — **Issues** : #5 #6

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Positions par défaut : progression et Favoris tous deux en `BottomRight`. Marquer `PL-FAV` favorite en `U1`, purger, recharger | Cœur **et** badge « lus/total » côte à côte en bas à droite, sans chevauchement ni rognage | | |
| 2 | Décocher `ShowFavorites`, purger, recharger | Seul le badge « lus/total » reste | | |
| 3 | Recocher `ShowFavorites`, retirer `PL-FAV` des favoris, purger, recharger | Seul le badge « lus/total » reste | | |
| 4 | Passer le groupe Favoris en `TopRight` | Cœur en haut à droite, progression en bas à droite | | |

**Verdict** : [ ] PASS  [ ] FAIL

## Critères de Validation

- [ ] CA8 à CA13 passent ; CA14 sans régression (critical)
- [ ] Délais de reconstruction ≤ ~1 min relevés dans le log pour CA11
- [ ] Cohabitation Favoris / progression correcte

## Notes QA

