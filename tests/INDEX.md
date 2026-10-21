# Index des tests

> Tests de specification ecrits par le test-writer, ranges en lots; statuts tenus par le CDP.
> Convention : `context/COMMON.md` section 15. Statuts : `feature` | `regression` | `quarantaine`. Tags : `smoke`, `critical`, `slow`.

| Chemin | Niveau | Composant | Feature | Statut | Tags |
|--------|--------|-----------|---------|--------|------|
| tests/EmbyBadges.Tests/ | unitaire | plugin-enhancer, plugin-renderer | FEATURE #4 badge Synchro (EvaluateTags, IsShared, LettersOf, ColorOf, clé de cache) | feature | - |
| tests/procedures/badges/synchro/ | procedure manuelle (QUALIF) | plugin-enhancer, plugin-renderer, config-page | FEATURE #4 badge Synchro CA1-CA11 | feature | smoke, critical |
