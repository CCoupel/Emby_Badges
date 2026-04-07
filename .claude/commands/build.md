# Commande /build — Compiler et déployer le plugin

## Usage
```
/build
```

---

## Règles critiques à ne jamais oublier

1. **Kubeconfig** : toujours `export KUBECONFIG=private/kubeconfig.yml` en premier — sans ça kubectl ne trouve pas le cluster.
2. **Chemin de destination** : le DLL se copie à `/config/plugins/EmbyBadges.dll` (à la racine du dossier plugins, **pas** dans un sous-dossier). C'est comme tous les autres plugins sur ce serveur (VirtualLib.dll, Iconic.dll…).
3. **`MSYS_NO_PATHCONV=1`** : obligatoire sur Git Bash/Windows pour tout argument kubectl contenant un chemin absolu Linux — sans ça Git Bash convertit `/config/...` en `C:/Program Files/Git/config/...`.
4. **Chemin source relatif** : utiliser `dist/EmbyBadges.dll` (relatif), pas un chemin absolu Windows — Git Bash casse les chemins absolus dans kubectl cp.
5. **Copier AVANT le restart** : le restart crée un nouveau pod qui relit le volume persistant. La copie doit donc être faite sur le pod courant **avant** le `rollout restart`. Après le restart, vérifier que le nouveau pod a bien la bonne taille de fichier.
6. **Vérification obligatoire** : après chaque déploiement, vérifier la taille du DLL dans le **nouveau** pod (pas l'ancien). La taille doit correspondre à `dist/EmbyBadges.dll` local.

---

## Séquence complète

```bash
# Depuis le répertoire racine du projet
cd "C:/Users/cyril/Documents/VScode/GITHUB/Emby_Badges"

# 1. Build
"C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe" build \
  src/EmbyBadges/EmbyBadges.csproj \
  --configuration Release \
  --output dist
# Vérifier : "0 Erreur(s)" dans la sortie

# 2. Kubeconfig
export KUBECONFIG=private/kubeconfig.yml

# 3. Récupérer le pod courant
POD=$(kubectl get pods -n media --no-headers | grep emby2 | awk '{print $1}')
echo "Pod cible : $POD"

# 4. Copier le DLL sur le pod courant (avant restart)
MSYS_NO_PATHCONV=1 kubectl cp dist/EmbyBadges.dll media/$POD:/config/plugins/EmbyBadges.dll

# 5. Restart Emby pour qu'il recharge le plugin
kubectl rollout restart deployment/emby2 -n media
kubectl rollout status deployment/emby2 -n media --timeout=90s

# 6. Vérifier dans le NOUVEAU pod
NEW_POD=$(kubectl get pods -n media --no-headers | grep emby2 | awk '{print $1}')
echo "Nouveau pod : $NEW_POD"
MSYS_NO_PATHCONV=1 kubectl exec -n media $NEW_POD -- ls -la /config/plugins/EmbyBadges.dll
# La taille doit correspondre à : $(ls -la dist/EmbyBadges.dll)
```

---

## Notes

- SDK .NET : `C:/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe` (install utilisateur, pas dans PATH)
- Namespace Kubernetes : `media`
- Kubeconfig : `private/kubeconfig.yml` (non committé)
- **Ne jamais déployer sur `emby`** (production) — uniquement sur `emby2`
