# Deploy PROD — adaptations projet Emby_Badges

> Compagnon de `deploy.prod.template.md` : **remplace** la procedure Helm generique. Meme mecanique que
> `deploy.qualif.md` (copie du DLL dans `/config/plugins`, restart, verification de taille) mais :
> cible `deployment/emby` (namespace `media`) et artefact = asset de la GitHub Release (BORE : jamais
> le DLL local de QUALIF, la CI a reconstruit depuis le tag).

## GARDE-FOU

`CLAUDE.md` : « Never deploy to `emby` (production) — only `emby2` ». Cette procedure ne s'execute que si
l'ordre de l'utilisateur, dans le message courant, demande explicitement le deploiement PROD **et** la version.
Sinon : STOP, remonter a `main` sans rien executer. Ne jamais l'enchainer automatiquement apres PUBLISH PROD.

```bash
# 1. Verification + recuperation de l'artefact publie (Release GitHub du tag)
export KUBECONFIG="${KUBECONFIG:-private/kubeconfig.yml}"
[ -n "$KUBE_CONTEXT" ] && kubectl config use-context "$KUBE_CONTEXT"
mkdir -p build/prod_v$VERSION
gh release download "v$VERSION" -R CCoupel/Emby_Badges -p EmbyBadges.dll -D "build/prod_v$VERSION" --clobber
ARTIFACT="build/prod_v$VERSION/EmbyBadges.dll"
EXPECTED_SIZE=$(stat -c %s "$ARTIFACT")

# 2. Install (memes regles critiques que deploy.qualif.md : MSYS_NO_PATHCONV, chemin relatif,
#    copie AVANT restart, destination /config/plugins/EmbyBadges.dll a la racine)
POD=$(kubectl get pods -n media --no-headers | grep -E '^emby-' | grep -v emby2 | awk '{print $1}')
echo "Pod PROD cible : $POD"   # controle visuel : ne doit pas contenir "emby2"
MSYS_NO_PATHCONV=1 kubectl exec -n media "$POD" -- cp /config/plugins/EmbyBadges.dll /config/plugins/EmbyBadges.dll.prev 2>/dev/null || true
MSYS_NO_PATHCONV=1 kubectl cp "$ARTIFACT" media/$POD:/config/plugins/EmbyBadges.dll
kubectl rollout restart deployment/emby -n media

# 3. Rollout + verification dans le NOUVEAU pod
kubectl rollout status deployment/emby -n media --timeout=120s
ROLLOUT_STATUS=$?
NEW_POD=$(kubectl get pods -n media --no-headers | grep -E '^emby-' | grep -v emby2 | awk '{print $1}')
ACTUAL_SIZE=$(MSYS_NO_PATHCONV=1 kubectl exec -n media "$NEW_POD" -- stat -c %s /config/plugins/EmbyBadges.dll)
[ "$ACTUAL_SIZE" = "$EXPECTED_SIZE" ] || ROLLOUT_STATUS=1
```

Variables : `KUBECONFIG` (defaut `private/kubeconfig.yml`), `KUBE_CONTEXT` optionnel. `gh` authentifie.

Cache d'images : meme purge optionnelle que QUALIF, sur `deployment/emby` — a proposer a l'utilisateur, jamais automatique en PROD.

## Echec / Rollback

`kubectl rollout undo` est inoperant (DLL sur volume). Restaurer `EmbyBadges.dll.prev` puis
`kubectl rollout restart deployment/emby -n media` (commandes de `deploy.qualif.md`, cible `emby`).
Le tag/la Release restent tels quels sauf decision de `main` (voir Rollback de `publish.prod.md`).
