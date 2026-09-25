# Deploy QUALIF — adaptations projet Emby_Badges

> Compagnon de `deploy.qualif.template.md` : **remplace integralement** la procedure Helm generique
> (pas de Helm ni d'image : le plugin est un DLL copie dans le volume `/config/plugins` du pod Emby).
> Cible : `deployment/emby2`, namespace `media`. **Jamais `emby`** (production).

## Regles critiques (issues de l'ancien /build)

1. **Kubeconfig** : `export KUBECONFIG=private/kubeconfig.yml` en premier (fichier gitignore). Sans lui kubectl ne trouve pas le cluster.
2. **Destination** : `/config/plugins/EmbyBadges.dll` — racine du dossier plugins, **pas** un sous-dossier (comme `VirtualLib.dll`, `Iconic.dll`...). Le nom dans le pod est `EmbyBadges.dll` (sans version).
3. **`MSYS_NO_PATHCONV=1`** obligatoire (Git Bash/Windows) pour tout argument kubectl contenant un chemin Linux absolu.
4. **Source relative** : chemin du DLL relatif a la racine du repo (`build/qualif_v.../...`), jamais un chemin Windows absolu (casse `kubectl cp`).
5. **Copier AVANT le restart** : le restart cree un pod qui relit le volume persistant ; la copie se fait sur le pod courant.
6. **Verification dans le NOUVEAU pod** : la taille du DLL doit egaler celle de l'artefact publie.

## Variables attendues

| Variable | Usage |
|----------|-------|
| `KUBECONFIG` | `private/kubeconfig.yml` (defaut si absent) |
| `KUBE_CONTEXT` | optionnel — contexte a selectionner si le kubeconfig en contient plusieurs |

```bash
# 1. Verification — la publication QUALIF doit exister
export KUBECONFIG="${KUBECONFIG:-private/kubeconfig.yml}"
[ -n "$KUBE_CONTEXT" ] && kubectl config use-context "$KUBE_CONTEXT"
ARTIFACT="build/qualif_v$DIR_VERSION/EmbyBadges-$VERSION.dll"
test -f "$ARTIFACT" || { echo "Publication QUALIF absente pour $VERSION — /publish qualif d'abord"; exit 1; }
EXPECTED_SIZE=$(stat -c %s "$ARTIFACT")

# 2. Install : sauvegarde du DLL en place (rollback), copie, restart
POD=$(kubectl get pods -n media --no-headers | grep emby2 | awk '{print $1}')
MSYS_NO_PATHCONV=1 kubectl exec -n media "$POD" -- cp /config/plugins/EmbyBadges.dll /config/plugins/EmbyBadges.dll.prev 2>/dev/null || true
MSYS_NO_PATHCONV=1 kubectl cp "$ARTIFACT" media/$POD:/config/plugins/EmbyBadges.dll
kubectl rollout restart deployment/emby2 -n media

# 3. Rollout + verification dans le NOUVEAU pod
kubectl rollout status deployment/emby2 -n media --timeout=90s
ROLLOUT_STATUS=$?
NEW_POD=$(kubectl get pods -n media --no-headers | grep emby2 | awk '{print $1}')
ACTUAL_SIZE=$(MSYS_NO_PATHCONV=1 kubectl exec -n media "$NEW_POD" -- stat -c %s /config/plugins/EmbyBadges.dll)
[ "$ACTUAL_SIZE" = "$EXPECTED_SIZE" ] || ROLLOUT_STATUS=1
```

> `.prev` : Emby ne charge que les `*.dll`, ce fichier est inerte. Si la sauvegarde echoue (premier deploiement), continuer.

**Cache d'images** (si le rendu des badges a change et n'est pas visible) — optionnel, a signaler a l'utilisateur :
```bash
kubectl exec -n media deployment/emby2 -- find /config/cache/images -name "*.jpg" -delete
```
(les deux couches `enhanced-images/` et `resized-images/` sont purgees ; puis hard-refresh du navigateur).

**Smoke test (etape `Smoke tests` du rapport)** : `ACTUAL_SIZE == EXPECTED_SIZE` et pod `Running`.
Le controle visuel des badges est fait par l'utilisateur au GATE QUALIF.

## Echec / Rollback

Echec d'installation ⇒ `DEPLOY FAILED` (format du template, `Rollback : restauration .prev`). `kubectl rollout undo`
est **inoperant** ici (le DLL vit dans le volume, pas dans l'image) — restaurer le DLL precedent :

```bash
POD=$(kubectl get pods -n media --no-headers | grep emby2 | awk '{print $1}')
MSYS_NO_PATHCONV=1 kubectl exec -n media "$POD" -- cp /config/plugins/EmbyBadges.dll.prev /config/plugins/EmbyBadges.dll
kubectl rollout restart deployment/emby2 -n media
```
