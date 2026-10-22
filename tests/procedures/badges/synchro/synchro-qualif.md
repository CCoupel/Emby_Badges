# Procédure de Test — Badge « Synchro » (PlaySync) — FEATURE #4

**Version** : 1.3.0
**Date** : 2026-10-21
**Testeur** : QA
**Plan** : `_work/reports/plan-20261021-204257.md` — **Contrat** : `contracts/models.md`

## Prérequis

- [ ] Environnement : QUALIF (`emby2`, namespace `media`), DLL candidate 1.3.0 déployée via `/deploy qualif`
- [ ] `export KUBECONFIG=private/kubeconfig.yml`
- [ ] Plugin PlaySync installé sur QUALIF (sauf CA10b, où il est volontairement absent/désactivé)
- [ ] 2 utilisateurs Emby : `U1` (propriétaire, premier utilisateur) et `U2` (membre)
- [ ] Films de test : `F1`..`F4` ; une série `S1` dont l'épisode `E1` sert au CA5
- [ ] Playlists (créées par U1) :
  - `PL-A` partagée avec U2 (niveau ≥ Lecture), étiquette `propager-avancement=OUI`
  - `PL-B` partagée avec U2, étiquettes `remove-si-lu=OUI` et `propager-lu=OUI`
  - `PL-C` **non partagée** (aucun membre), étiquette `propager-lu=OUI`
  - `PL-D` partagée avec U2, étiquettes `propager-avancement=NON`, `propager-lu=NON`, `remove-si-lu=NON`
- [ ] Config plugin : `EnableBadges` actif, groupe Synchro actif, mode `Letters`, 3 types cochés, position par défaut

**Purge du cache images — à faire entre CHAQUE cas** (puis attendre 5 min si l'index doit être rafraîchi, sauf mention) :

```bash
kubectl exec -n media deployment/emby2 -- find /config/cache/images -name "*.jpg" -delete
```

Puis recharger l'interface Emby avec un rafraîchissement forcé (Ctrl+F5).

## Scénarios

### CA10 (smoke) — Plugin chargé, badges existants intacts

**Objectif** : le plugin démarre sans erreur et les badges historiques ne régressent pas.

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Après déploiement, lire le log : `kubectl logs -n media deployment/emby2 --tail=500 \| grep -i "EmbyBadges\|BadgeEnhancer"` | Aucune exception `BadgeEnhancer` / `SyncDetector` | | |
| 2 | Ouvrir l'affiche d'un film **absent de toute playlist** (ex. `F4`) | Badges résolution / langue / note / favori présents comme en v1.2.x | | |
| 3 | Vérifier l'absence de badge Synchro sur cette affiche | Aucun badge teal / aucune lettre P/L/S | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA10b — PlaySync absent ou aucune playlist étiquetée

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Retirer (ou désactiver) PlaySync **ou** retirer toutes les étiquettes des playlists ; attendre 5 min ; purger le cache | — | | |
| 2 | Ouvrir les affiches de `F1`..`F4` | Aucun badge Synchro, badges existants intacts | | |
| 3 | Relire le log | Aucune erreur / exception liée à Synchro | | |
| 4 | Remettre PlaySync / les étiquettes pour la suite | — | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA1 — Film dans une playlist partagée avec `propager-lu=OUI`

Mise en place : `F1` ajouté à une playlist `PL-L` partagée avec U2, étiquette `propager-lu=OUI` seule. Attendre 5 min, purger.

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Mode `Letters` : ouvrir l'affiche de `F1` | Badge sarcelle avec la lettre `L` en haut à gauche | | |
| 2 | Passer en mode `Color`, sauvegarder, purger, recharger | Pictogramme « sync » sur fond **orange** | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA2 — Union de deux playlists (A et B)

Mise en place : `F2` ajouté à `PL-A` et `PL-B`. Attendre 5 min, purger.

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Mode `Letters` : affiche de `F2` | Badge `PLS` (ordre fixe P, L, S) | | |
| 2 | Mode `Color` : affiche de `F2` | Pictogramme sur fond **rouge** (le type le plus fort l'emporte) | | |
| 3 | Mode `Letters` : `F3` présent seulement dans `PL-B` | Badge `LS` (Removed effectif car `propager-lu=OUI`) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA3 — Playlist partagée sans étiquette active

Mise en place : `F3bis` dans `PL-D` (3 étiquettes `NON`). Variantes à tester successivement : (a) 3 étiquettes `NON` ; (b) aucune étiquette ; (c) `propager-lu=OUI` **et** `propager-lu=NON` ; (d) `remove-si-lu=OUI` seul (sans `propager-lu`).

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Pour chaque variante (a)–(d) : appliquer, attendre 5 min, purger, ouvrir l'affiche | Aucun badge Synchro | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA4 — Playlist non partagée

Mise en place : `F4bis` uniquement dans `PL-C` (non partagée, `propager-lu=OUI`). Attendre 5 min, purger.

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Ouvrir l'affiche de `F4bis` | Aucun badge Synchro | | |
| 2 | Partager `PL-C` avec U2 ; attendre 5 min ; purger ; recharger | Badge `L` apparaît | | |
| 3 | Retirer le partage ; attendre 5 min ; purger ; recharger | Le badge disparaît | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA5 — Série (agrégation des épisodes)

Mise en place : seul l'épisode `E1` de `S1` est dans `PL-A`. Attendre 5 min, purger.

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Poster de la série `S1` | Badge `P` | | |
| 2 | Thumb / fond de la série `S1` (accueil « Continuer à regarder » le cas échéant) | Badge `P` | | |
| 3 | Poster de la saison contenant `E1` | Badge `P` | | |
| 4 | Poster/thumb de l'épisode `E1` | Badge `P` | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA6 — Mode de rendu et régénération des images

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Page de config du plugin : section « Synchro (PlaySync) » | Sélecteur de mode `Letters` / `Color` présent | | |
| 2 | Choisir `Color`, sauvegarder, **sans** purger le cache `enhanced-images`, purger seulement `resized-images` si nécessaire, recharger | L'affiche `F2` passe de `PLS` au pictogramme rouge (clé de cache modifiée) | | |
| 3 | Revenir en `Letters`, sauvegarder, recharger | `PLS` de nouveau | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA7 — Types activables individuellement

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Décocher « Position » ; sauvegarder ; purger ; affiche `F2` | Badge `LS` | | |
| 2 | Décocher aussi « Supprimé » ; affiche `F2` | Badge `L` | | |
| 3 | Décocher aussi « Lu » ; affiche `F2` | Aucun badge Synchro | | |
| 4 | Rétablir les 3 cases | Badge `PLS` | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA8 — Position, taille, marge, opacité du groupe

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Config par défaut | Position du groupe Synchro = Haut gauche | | |
| 2 | Régler position `BottomRight`, taille, marge, opacité différentes ; sauvegarder ; purger | Le badge Synchro suit les 4 réglages, indépendamment des autres groupes | | |
| 3 | Mettre Synchro et Note à la **même position** | Badges côte à côte, sans chevauchement | | |
| 4 | Mettre Synchro dans chacune des 8 positions (échantillon : 3 au minimum) | Aucun chevauchement avec les groupes d'autres positions | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA9 — Fraîcheur après modification (≤ 5 min)

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Noter l'heure ; ajouter `F1` à `PL-A` (badge `P` à venir) | — | | |
| 2 | Après 5 min + purge du cache images | Badge `PL` (ou `P`+`L`) sur `F1` | | |
| 3 | Modifier l'étiquette de `PL-A` (`propager-avancement=NON`) ; attendre 5 min ; purger | Le `P` disparaît | | |
| 4 | Retirer `F1` de toutes les playlists ; attendre 5 min ; purger | Plus aucun badge Synchro | | |

Note : sans purge, la limitation connue du cache `resized-images` peut retarder l'affichage (voir CLAUDE.md « Image Cache »).

**Verdict** : [ ] PASS  [ ] FAIL

---

### CA11 — `ShowSync=false`

| Etape | Action | Résultat Attendu | Résultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Décocher le groupe Synchro ; sauvegarder ; purger | Aucun badge Synchro sur `F1`..`F4`, `S1` | | |
| 2 | Observer le log (niveau Debug si disponible) pendant le chargement de plusieurs affiches | Aucune ligne de reconstruction d'index Synchro | | |
| 3 | Vérifier les autres badges | Inchangés | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

## Criteres de Validation

- [ ] CA1 à CA11 passent (CA10 = smoke, bloquant)
- [ ] Aucune exception liée à Synchro dans le log Emby
- [ ] Les badges existants (résolution, langue, multi-version, favori, note) sont inchangés
- [ ] `dotnet test tests/EmbyBadges.Tests/` vert (tests automatisés du lot)

## Notes QA

[Observations]
