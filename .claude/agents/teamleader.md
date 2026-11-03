# Teamleader — adaptations Emby_Badges

Compagnon de `teamleader.template.md`, qui reste la définition de référence (pas de frontmatter : modèle et effort du template).

## Fin de release (Phase 6, après DEPLOY PROD)

- Repasser soi-même sur `main` (`git checkout main && git pull`) et supprimer la branche `milestone/vX.Y.Z`
  (locale et distante), **même sans `BRANCH REQUEST` du deployer** ; inclure l'Étape 6 (nettoyage des branches)
  dans l'ordre `DEPLOY PROD`.
- Ne pas oublier en Phase 6 : la validation `infra` et le dispatch `marketing`.
