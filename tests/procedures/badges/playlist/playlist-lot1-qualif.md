# Procédure de Test — Badges de playlist, LOT-1 : partage + favori (#5)

**Version** : 1.4.0
**Date** : 2026-10-27
**Testeur** : QA
**Plan** : `.claude/workflow/plan-v1.4.0.md` — **Contrat** : `contracts/models.md` §7–12

## Prérequis

- [ ] Environnement : QUALIF (`emby2`, namespace `media`), DLL candidate 1.4.0 déployée via `/deploy qualif`
- [ ] `export KUBECONFIG=private/kubeconfig.yml`
- [ ] 2 utilisateurs Emby : `U1` (propriétaire, premier utilisateur) et `U2` (membre)
- [ ] Médias de test : films `F1`..`F3`, un épisode, une série
- [ ] Playlists (créées par U1) :
  - `test` : playlist partagée existante (étiquettes à relever à l'étape 1 de CA1)
  - `PL-S` partagée avec U2 (niveau ≥ Lecture), **sans étiquette**
  - `PL-V` partagée avec U2, `propager-avancement=OUI` seul
  - `PL-L` partagée avec U2, `propager-lu=OUI`
  - `PL-R` partagée avec U2, `remove-si-lu=OUI` + `propager-lu=OUI`
  - `PL-R0` partagée avec U2, `remove-si-lu=OUI` seul
  - `PL-N` **non partagée**, étiquette `propager-lu=OUI`
  - `PL-F` playlist à marquer favorite par U1 (partagée ou non)
  - `PL-AUTO` playlist **sans image propre** (collage automatique), partagée
- [ ] Config plugin : badges activés, `ShowPlaylistShare` actif, `ShowFavorites` actif, positions par défaut
- [ ] Log Debug du plugin activé (pour CA6)

**Purge du cache images — à faire entre CHAQUE cas** (`.jpg` ET `.webp`) :

```bash
kubectl exec -n media deployment/emby2 -- find /config/cache/images \( -name "*.jpg" -o -name "*.webp" \) -delete
```

Puis rechargement forcé du client web (Ctrl+F5). Un changement de partage ou d'étiquette peut mettre jusqu'à 5 min à apparaître.

## Scénarios

### S1 — Cas de collage automatique (spike, a/b)

**Validation** : HUMAIN — **Issues** : #5

**Objectif** : déterminer si l'enhancer reçoit les images de playlist à collage automatique (cas a) ou seulement celles à image propre (cas b).

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Ouvrir `PL-AUTO` (collage auto) dans la page Playlists, sans image personnalisée | Vignette affichée | | |
| 2 | Lire le log : `kubectl logs -n media deployment/emby2 --tail=500 \| grep "EmbyBadges: enhance"` | Une ligne pour `PL-AUTO` (cas **a**) ou aucune (cas **b**) | | |
| 3 | Définir une image propre sur `PL-S`, purger le cache, recharger, relire le log | Une ligne pour `PL-S` | | |
| 4 | Consigner le cas : a / b | Cas noté ; aucune ligne pour aucune playlist = NO-GO (c) | | |

**Verdict** : [ ] PASS  [ ] FAIL — Cas constaté : [ ] a  [ ] b  [ ] c

---

### CA1 — Pictogramme sur playlist partagée (propriétaire et membre)

**Validation** : HUMAIN — **Issues** : #5

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Relever les étiquettes de la playlist `test` (page de détail, onglet étiquettes) | Étiquettes notées | | |
| 2 | Purger le cache, se connecter en `U1`, ouvrir la page Playlists | `test` et `PL-S` affichent un pictogramme en haut à gauche | | |
| 3 | Se connecter en `U2`, ouvrir la page Playlists | Même pictogramme, même couleur, sur les mêmes playlists | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA2 — Couleur selon les étiquettes

**Validation** : HUMAIN — **Issues** : #5

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Observer `PL-S` (aucune étiquette) | Pictogramme **sarcelle** | | |
| 2 | Observer `PL-V` (`propager-avancement=OUI`) | **Vert** | | |
| 3 | Observer `PL-L` (`propager-lu=OUI`) | **Orange** | | |
| 4 | Observer `PL-R` (`remove-si-lu=OUI` + `propager-lu=OUI`) | **Rouge** | | |
| 5 | Observer `PL-R0` (`remove-si-lu=OUI` seul) | **Sarcelle** (état effectif) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA3 — Playlist non partagée

**Validation** : HUMAIN — **Issues** : #5

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Observer `PL-N` (étiquetée mais non partagée) | Aucun pictogramme | | |
| 2 | Retirer le dernier membre de `PL-S`, attendre 5 min, purger, recharger | Le pictogramme de `PL-S` a disparu | | |
| 3 | Re-partager `PL-S` avec U2, attendre 5 min, purger, recharger | Le pictogramme réapparaît | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA4 — Désactivation et réglages du groupe

**Validation** : HUMAIN — **Issues** : #5

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Config : décocher `ShowPlaylistShare`, sauvegarder, purger, recharger | Aucun pictogramme sur les playlists | | |
| 2 | Recocher ; régler position `BottomLeft`, taille, marge, opacité du groupe « Playlist partagée » ; purger, recharger | Position, taille, marge et opacité appliquées | | |
| 3 | Remettre `TopLeft` et valeurs par défaut | — | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA5 — Non-régression des médias (critical)

**Validation** : HUMAIN — **Issues** : #5

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Purger, ouvrir affiches de film, épisode, série, saison (dont un média en playlist PlaySync) | Badges résolution, langue, version, note, favori et Synchro identiques à v1.3.0 | | |
| 2 | Décocher `ShowSync`, purger, recharger | Le badge Synchro des médias disparaît ; le pictogramme de playlist reste | | |
| 3 | Recocher `ShowSync` | Badge Synchro revenu (lettres et couleur) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA6 — Gating de l'index

**Validation** : AUTO — **Issues** : #5 (équivalent automatisé : `IndexNeededTests`, 8 combinaisons)

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Config : décocher `ShowSync`, `ShowPlaylistShare` et `ShowPlaylistProgress`, sauvegarder | — | | |
| 2 | Redémarrer le pod, purger, ouvrir les pages Films et Playlists | Aucun badge Synchro / playlist | | |
| 3 | `kubectl logs -n media deployment/emby2 \| grep -i "GetUserItemShares\|index"` | Aucune construction d'index ni appel de partage dans le log Debug | | |
| 4 | Recocher `ShowPlaylistShare` seul, recharger | Index construit (log Debug) ; pictogrammes de nouveau présents | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA7 — Page de configuration

**Validation** : HUMAIN — **Issues** : #5

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Ouvrir la config du plugin | Section « Playlist partagée » présente avec case d'activation et réglages du groupe | | |
| 2 | Modifier les valeurs, sauvegarder, recharger la page | Valeurs conservées | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA7b — Favori sur playlist, jamais de note

**Validation** : HUMAIN — **Issues** : #5

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | En `U1`, marquer `PL-F` favorite ; purger, recharger | Cœur Favoris visible, groupe Favoris (position, taille) respecté | | |
| 2 | Décocher `ShowFavorites`, purger, recharger | Cœur absent | | |
| 3 | Recocher, retirer `PL-F` des favoris, purger, recharger | Cœur absent | | |
| 4 | Observer toutes les playlists | Aucune note ni autre badge média (résolution, langue) | | |

**Verdict** : [ ] PASS  [ ] FAIL

## Critères de Validation

- [ ] Tous les scénarios passent ; S1 : cas a ou b (c = NO-GO, escalade)
- [ ] CA5 sans régression (critical)

## Notes QA

