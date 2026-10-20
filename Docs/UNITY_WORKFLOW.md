# Unity workflow audit and changes

The standing preference is recorded in [AGENTS.md](../AGENTS.md): use Unity-native
APIs and tooling wherever they improve workflow, while keeping authoritative rules
and seeded decisions independent of the engine. Versions checked in the repository:
Unity 6000.3.26f1, Input System 1.17.0, uGUI 2.0.0 and Test Framework 1.6.0.
No package versions, existing scene YAML or existing asset GUIDs were changed.

## Audit scope

The initial inventory contained 178 C# files: 80 Rules, 41 Application, 11 Data,
16 Presentation, 2 Editor and 28 Tests. The pass inventoried direct Unity imports,
assembly dependencies, engine lifecycle, input dispatch, asset loading/conversion,
content authoring/validation, serialization and rendering/profiling paths.
[The per-file inventory](UNITY_USAGE_INVENTORY.md) records the resulting 193 files.
It is a static source audit, not a Unity compilation or runtime measurement.

| Area | Finding | Change |
| --- | --- | --- |
| Dialogue editing | JSON and a generated reference existed, but Unity had no searchable editing surface | Native UI Toolkit EditorWindow with source selection, text search, entry/metadata inspection, staged prose editing, Undo/Redo, validation and save |
| Source integrity | Directly serializing a Unity DTO could discard or default optional metadata | Portable structural JSON editing preserves all unrelated fields/conditions; runtime schemas still validate before save |
| Source persistence | An editor save must respect external edits, checkout and meta GUIDs | Compare source revision, AssetDatabase.MakeEditable, temporary-file replacement and targeted ImportAsset; close/source-switch use native save/discard controls |
| Reference generation | Refreshing the dialogue reference required Python outside Unity | Native C# exporter runs after canonical source saves; Tools → SocialStim → Refresh Dialogue Catalogue is also available; the Python CI checker remains |
| Content validation | A manual menu validated one hardcoded starter catalog | AssetDatabase discovers every project catalog and authored role/setup/reasoning/effect/style/text definition; shared checks include IDs, references, schemas, markup and setup constraints |
| Import workflow | Content mistakes could remain unnoticed until the manual menu or game startup | AssetPostprocessor coalesces relevant import/move/delete batches and validates through EditorApplication.delayCall, outside the active import operation |
| Build workflow | Player builds had no project content gate | IPreprocessBuildWithReport rejects invalid authored content with asset paths and checks the Input System backend |
| Inspectors | Role/catalog errors appeared late | Native serialized inspectors retain Unity property handling and multi-object Undo, with cached validation errors and direct navigation to referenced dialogue |
| Keyboard/gamepad input | Text advance and modal close used device-specific polling | InputAction callbacks, optional inspector InputActionReference overrides, and owned action clones with enable/disable/dispose lifecycle |
| Pointer input | Manual mouse/touch rectangle tests bypassed native pointer dispatch | IPointerClickHandler on the current line, with uGUI raycasting and modal blockers |
| Scene input setup | Startup always created an EventSystem, even if one existed | FindObjectsByType reuses one compatible enabled EventSystem; conflicting systems/modules produce a clear configuration error |
| TMP profiling | No named project scopes distinguished layout capture from effects updates | ProfilerMarker scopes SocialStim.TextEffects.Capture and SocialStim.TextEffects.Apply |

## What remains deliberately engine-independent

All 121 Rules/Application files still have no direct Unity imports and retain
`noEngineReferences` assembly boundaries. RunSession, AI/beliefs, commands, night
resolution, role/victory logic, seeded IRandomSource and playback/effect sampling
remain portable. Moving those decisions into MonoBehaviours or UnityEngine.Random
would undermine reproducibility and shared tests.

Strict JSON parsing remains portable rather than being replaced with JsonUtility:
content needs unknown-field/duplicate rejection, dictionaries, optional-condition
preservation and the same validation in Unity and headless tests. The Unity editor
uses its own asset/Undo/window APIs around that validated data. SettingsStore keeps
its atomic, testable file behavior; Unity still supplies persistentDataPath, pause/
quit callbacks and the one-time legacy PlayerPrefs migration adapter.

Existing ScriptableObjects, CreateAssetMenu, SerializeField/Range/Min, uGUI layouts,
TMP mesh callbacks, unscaled Unity time, InputSystemUIInputModule, Test Framework,
and the IMGUI effects playground already fit their purposes and are retained.
The UI Toolkit window is editor tooling; the working runtime UI remains uGUI/TMP.
No Addressables, localization package or other new dependency is introduced.

## Using the native dialogue editor

Open Tools → SocialStim → Dialogue and Text Editor, or select a GameCatalog and
click Edit referenced dialogue in its inspector. Choose StarterDialogue.json for
spoken choices or SocialStimText.json for narration/results/interface wording.
Search by ID, intent/group or text, select an entry, then edit Authored text.
The complete selected entry remains visible below for inspecting conditions and
metadata. This pass edits prose in the window; adding entries or changing metadata
uses Open JSON source and the existing documented schema.

Edits commit to the staging buffer when the delayed field loses focus. Undo/Redo
records only that buffer, leaving the source baseline outside Undo. Save validates
the full document and semantic markup, checks external edits/read-only state, then
replaces/reimports the original asset without replacing its meta file. Closing
or changing source with staged edits invokes native Save/Discard/Cancel behavior.
Malformed JSON structure can be repaired through Open JSON source. Source wording
still loads for setup/new runs, not through hot mutation of an active run.

The canonical-source save also refreshes Docs/DIALOGUE_CATALOGUE.md in C#, so normal
Unity prose editing does not require launching Python. CI compares the generated
reference and required bindings independently. Extra, noncanonical content files
remain editable but do not automatically become runtime catalog references.

Optional Advance Action and Cancel Action references are on GameBootstrap. They
are cloned into independent action instances. Defaults are Space for text advance
and Escape/gamepad B for closing the top modal. Native buttons continue handling
navigation/submit, including the selected Continue button. Mouse/touch use uGUI
pointer dispatch. Same-frame reveal/advance guards and modal input gates remain.

## Validation evidence and limits

Initial code-head Actions run 39385484139 passed 188 shared tests with zero failures
or skips, plus NUnit 3.5/.NET Framework compilation. Tests cover real text rewriting,
preservation of every unrelated line/condition/weight/metadata entry, invalid staged
copy rejection/repair and byte-for-byte parity with the complete catalogue. Local
scaffold, metadata, dependency-boundary, catalogue-freshness and whitespace checks
pass. Native input lifecycle/EventSystem regressions are added to the Unity runner.
Final-head validation results are recorded in the PR.

Unity is unavailable here. The editor window, inspector APIs, import/build callbacks,
Input System callbacks, pointer routing and profiler scopes have been reviewed against
the pinned API versions but have not been compiled or executed inside Unity. No
editor/player build, UI/input validation, measured allocation reduction or frame-time
improvement is claimed.

## Manual Unity checklist (pending)

- [ ] Open 6000.3.26f1, resolve existing packages, import TMP Essential Resources,
      enable the Input System backend, and check for compiler/Console errors.
- [ ] Run EditMode and PlayMode suites, including UnityWorkflowTests and the existing
      first-press/second-press, settings and TMP rendering tests.
- [ ] Open both canonical JSON sources in the new window. Search/select later entries,
      edit text, change focus, Undo/Redo, save, undo again after save, and switch source.
      Verify the displayed selection and dirty state, including after script reload.
- [ ] Close a dirty window: exercise Save/Discard/Cancel. Verify invalid markup or an
      unavailable token cannot save, and other fields/IDs/conditions remain intact.
- [ ] Modify the source externally while edits are staged; save must refuse to overwrite.
      Test read-only/checkout failure, Unicode/quotes/newlines, and preserved meta GUIDs.
- [ ] Save a canonical source and compare the regenerated reference with the Python
      freshness check. Check errors when the reference file is not writable.
- [ ] Introduce/fix invalid role/claim data, role-list references, copy tokens, markup
      and effect definitions. Import/move/delete them and verify deferred validation.
- [ ] Test valid and invalid player builds; invalid project content must stop the build
      with an asset path. Confirm standalone nonreferenced draft definitions are also
      included by the current all-content validation policy.
- [ ] Check inspector multi-selection, property Undo/Redo, referenced-dialogue navigation
      and validation refresh after a referenced asset changes.
- [ ] Verify Space/touch/mouse/gamepad Continue and Escape/B closing, including modals,
      setup, rapid same-frame input and disabling/re-enabling/destroying the view.
      Assign custom action references and confirm source action maps remain unchanged.
- [ ] Start with no EventSystem and with one configured InputSystemUIInputModule;
      verify exactly one dispatcher. Conflicting active systems/modules must report
      a clear error rather than creating another dispatcher.
- [ ] Profile warmed effects and layout changes separately using both new markers.
      Record actual results before making performance claims.

## API references checked

- [Unity 6.3 UI Toolkit editor windows](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-HowTo-CreateEditorWindow.html)
- [EditorWindow.SaveChanges](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorWindow.SaveChanges.html)
- [Undo.RecordObject](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Undo.RecordObject.html)
- [AssetPostprocessor.OnPostprocessAllAssets](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html)
- [IPreprocessBuildWithReport](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.IPreprocessBuildWithReport.html)
- [Input System 1.17 InputAction](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.InputAction.html)
- [Unity ProfilerMarker](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerMarker.html)
