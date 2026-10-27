# Index des tests

> Tests de specification ecrits par le test-writer, ranges en lots; statuts tenus par le CDP.
> Convention : `context/COMMON.md` section 15. Statuts : `feature` | `regression` | `quarantaine`. Tags : `smoke`, `critical`, `slow`.

| Chemin | Niveau | Composant | Feature | Statut | Tags |
|--------|--------|-----------|---------|--------|------|
| tests/EmbyBadges.Tests/ | unitaire | plugin-enhancer, plugin-renderer | FEATURE #4 badge Synchro (EvaluateTags, IsShared, LettersOf, ColorOf, clé de cache) | feature | - |
| tests/procedures/badges/synchro/ | procedure manuelle (QUALIF) | plugin-enhancer, plugin-renderer, config-page | FEATURE #4 badge Synchro CA1-CA11 | feature | smoke, critical |
| tests/EmbyBadges.Tests/ (PlaylistShareTests) | unitaire | plugin-enhancer, plugin-renderer | FEATURE #5 partage de playlist (OwnerOf, IsShared, IndexNeeded, ShareColorOf, clé de cache) | feature | - |
| tests/procedures/badges/playlist/ | procedure manuelle (QUALIF) | plugin-enhancer, plugin-renderer, config-page | FEATURE #5 partage + favori playlist CA1-CA7b | feature | smoke, critical |
| tests/EmbyBadges.Tests/ (PlaylistProgressTests) | unitaire | plugin-enhancer, plugin-renderer | FEATURE #6 progression de playlist (CountProgress, ProgressLabel, IsStale, ShouldInvalidateOnUserData, clé de cache) | feature | - |
| tests/procedures/badges/playlist/ (playlist-lot2-qualif.md) | procedure manuelle (QUALIF) | plugin-enhancer, plugin-renderer, config-page | FEATURE #6 progression de playlist CA8-CA14 | feature | critical |
