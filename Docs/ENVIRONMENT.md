# Environment decisions and references

Milestone 00 pins **Unity 6000.3.26f1**, a Unity 6.3 LTS release published on
October 8, 2026. This is a reproducible baseline, not a claim that it is the
latest patch. The editor has not been installed or run in the creation environment.

Direct dependencies are pinned in `Packages/manifest.json`: Input System 1.17.0,
Unity Test Framework 1.6.0, uGUI 2.0.0, and selected built-in modules. Unity must
resolve and generate the transitive lock; package compatibility is not verified
until that import succeeds. Do not invent a `packages-lock.json` from memory.

uGUI 2.0 includes TextMeshPro. The presentation assembly references the package's
actual assembly names: `UnityEngine.UI`, `Unity.TextMeshPro`, and
`Unity.InputSystem`. Package IDs are not assembly names. No separate deprecated
`com.unity.textmeshpro` dependency is added.

Initial rendering is Built-in with a planned uGUI text view. No URP, Cinemachine,
Addressables, or post-processing package is needed for a scaffold without scenes.
Future text effects use TMP mesh data and screen effects begin with UI overlays.
Changing the pipeline later must not affect Rules or Application.

## Primary references checked during preparation

- [Unity 6000.3.26f1 release](https://unity.com/releases/editor/whats-new/6000.3.26f1)
- [Unity 6.3 LTS overview](https://docs.unity.com/en-us/engine/6000.7/manual/whats-new/unity63)
- [Input System package versions](https://docs.unity3d.com/kr/6000.0/Manual/com.unity.inputsystem.html)
- [Test Framework 1.6 changelog](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/changelog/CHANGELOG.html)
- [Unity's uGUI assembly definition](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UnityEngine.UI.asmdef)
- [TMP migration into uGUI, Unity package maintainer](https://discussions.unity.com/t/2023-2-latest-development-on-textmesh-pro/917387)

Online reference branches and docs may advance independently of the pinned
editor. Package resolution and editor validation remain the authoritative local
compatibility check.
