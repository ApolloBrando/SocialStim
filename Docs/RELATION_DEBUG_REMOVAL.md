# Relationship debug removal

The relationship debug button, modal chart, arrow renderer, privileged snapshot API and chart-only tests have been removed at the owner’s request. The night-action debug tool remains. NPC belief state, evidence, trust, suspicion and decision making remain active.

Removed assets and their metadata (no serialized scene, prefab or asset references were found):

- `Assets/_Project/Presentation/Text/RelationChartPanel.cs` — GUID `eaee43dcdece4f229d5a56ff6edc91e5`
- `Assets/_Project/Presentation/Effects/RelationArrowGraphic.cs` — GUID `304db1e952344df4bc8f77a6914031f2`
- `Assets/_Project/Tests/PlayMode/RelationChartLayoutTests.cs` — GUID `bd53c4d57ea34e75b23b47cfc9f7f337`
- `Assets/_Project/Rules/Reasoning/SocialDebugRecord.cs` — GUID `e13011df4131409799cc5e11e1f560ca`

Existing serialized fields and remaining asset GUIDs are preserved. Old relationship text keys remain in the editable catalogue for compatibility with existing content; no relationship debug UI consumes them. Historical chart design documents describe the removed implementation.

The AI changes and current opinion model are explained in TRUST_AND_SUSPICION.md.

## Overlay-import compatibility

The former `Assets/_Project/Tests/PlayMode/RelationChartLayoutTests.cs` path now contains only a retirement comment, with its original metadata GUID. Main had deleted this test, but overlay imports can retain a stale copy that calls the removed `GetSocialDiagnostics` API and breaks compilation. The empty replacement overwrites that stale test without restoring any chart or debug mechanics. A Git update that applies deletions would already remove the obsolete test.
