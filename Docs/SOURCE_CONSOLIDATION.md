# Source-file consolidation

Baseline: main `01c073149ae900b299093f5ac2cb47aa18cc7583` (includes the relationship-debug removal).

This is a file-organization change. It merges 38 small plain C# source files into 14 existing owners without changing their declarations, namespaces, visibility, enum values, public API names, serialized field names or method bodies. The shared assembly boundaries and ordered/customizable night-stage pipeline remain intact.

The baseline has 555 tracked files, including 256 Unity metadata files and 188 C# files. Removing the 38 source/metadata pairs and adding this guide reduces the total to 480: **75 fewer files (13.5%)**, with **150 C# files instead of 188 (20.2% fewer)**. PR #51's independent two-file retirement replacement, if merged, adds two files to both sides of this comparison. This consolidation primarily reduces navigation/import clutter; it does not promise smaller player builds or faster gameplay.

## Where to find things

| Owner file | Related declarations now colocated |
| --- | --- |
| `Assets/_Project/Application/PresentationCues/EffectProfile.cs` | `EffectBlend`, `EffectChannel`, `EffectEnvelope`, `EffectTone`, `EffectFrame`, `EffectStep` |
| `Assets/_Project/Application/PresentationCues/DisplayPlayback.cs` | `DisplayCompletion`, `DisplayAcknowledgement` |
| `Assets/_Project/Application/PresentationCues/SemanticText.cs` | `SemanticStyle`, `SemanticSpan` |
| `Assets/_Project/Rules/Commands/GameCommand.cs` | `CommandKind`, `Verdict` |
| `Assets/_Project/Rules/Roles/RoleSpec.cs` | `AbilityKind`, `Faction`, `RoleCategory` |
| `Assets/_Project/Rules/Night/NightResolver.cs` | `INightStage`, `RoleBlockStage`, `ForgeryStage`, `VisitStage`, `ProtectionStage`, `AttackStage`, `WatchStage`, `InvestigationStage` |
| `Assets/_Project/Rules/Night/NightReport.cs` | `ReportKind`, `ReportedNightResult`, `NightDebugRecord`, `TeamNightRecord` |
| `Assets/_Project/Rules/Night/NightActor.cs` | `NightOrder` |
| `Assets/_Project/Rules/Night/NightResolution.cs` | `Visit` |
| `Assets/_Project/Rules/Reasoning/TeamNightPlanner.cs` | `TeamNightPlan`, `TeamProposal` |
| `Assets/_Project/Rules/Reasoning/BeliefDistribution.cs` | `FactionEstimate`, `RoleEstimate` |
| `Assets/_Project/Rules/Reasoning/EvidenceRecord.cs` | `EvidenceKind` |
| `Assets/_Project/Data/Dialogue/DialogueDocument.cs` | `DialogueRecord`, `DialogueConditionRecord`, `DialogueMetadataRecord` |
| `Assets/_Project/Data/Dialogue/GameTextDocument.cs` | `GameTextEntry` |

## Unity and content boundaries

Unity component scripts, ScriptableObject definitions, scenes, role assets and their existing GUIDs remain in their own files. Editable trait dialogue JSON stays separate for authoring. No README, package pin, assembly definition, test behavior or gameplay logic is changed.

The moved declarations are portable C# enums, structs, interfaces and ordinary classes. None derives from MonoBehaviour, ScriptableObject or MaskableGraphic. Before removing their former source assets, all serialized scenes, prefabs and assets were checked for references to the old script GUIDs; none were found. Destination files retain their original metadata. Types are copied into their original namespaces, so consumers continue to use the same type names.

Use a clean Git pull/merge that applies tracked deletions. Copying changed files over an old project can retain the removed source files and create duplicate C# type definitions. If updating manually, remove each old source file and its metadata using the manifest below. Do not remove Unity's other metadata files to reduce file count.

Historical audit inventories describe their original snapshots and are not current source-navigation indexes.

## Removed source assets and metadata

Every source path below, together with its `.meta` file, is removed. Its declarations now live in the owner listed above. This manifest records the retired GUIDs; it does not assign replacement GUIDs to the moved types.

| Removed source path | Retired GUID |
| --- | --- |
| `Assets/_Project/Application/PresentationCues/EffectBlend.cs` | `2995f29c921453f78db1e491076976eb` |
| `Assets/_Project/Application/PresentationCues/EffectChannel.cs` | `cf9bcde545a3538d8571d412c48e645f` |
| `Assets/_Project/Application/PresentationCues/EffectEnvelope.cs` | `c315b7ed917a57bab10eea94b4d6b128` |
| `Assets/_Project/Application/PresentationCues/EffectTone.cs` | `ea4c06edb57c4d6084ba03ef6bbd44af` |
| `Assets/_Project/Application/PresentationCues/EffectFrame.cs` | `853046a100115034aa37821ae100a4a1` |
| `Assets/_Project/Application/PresentationCues/EffectStep.cs` | `cef3696f37c25b239de4706742ac6f1e` |
| `Assets/_Project/Application/PresentationCues/DisplayCompletion.cs` | `3bfe98078ff8549991ed5e7e6216b7cb` |
| `Assets/_Project/Application/PresentationCues/DisplayAcknowledgement.cs` | `39f30722c02d5018bb9f63ec9c0a0eeb` |
| `Assets/_Project/Application/PresentationCues/SemanticStyle.cs` | `35dd2451a54f59ad87f2cc3efa2a0f93` |
| `Assets/_Project/Application/PresentationCues/SemanticSpan.cs` | `edddced578b65f20aa519079d87758c4` |
| `Assets/_Project/Rules/Commands/CommandKind.cs` | `13746a1ab5075abf9a6760981b7c5d7f` |
| `Assets/_Project/Rules/Commands/Verdict.cs` | `f78d9a4d23295cccbdc11ddcff6fdd4a` |
| `Assets/_Project/Rules/Roles/AbilityKind.cs` | `2e351ca2d94856308b2fe87d2e8bb794` |
| `Assets/_Project/Rules/Roles/Faction.cs` | `069bff3336795542af0030ef5cd4c092` |
| `Assets/_Project/Rules/Roles/RoleCategory.cs` | `fbcb3a5870ee5970a768d1cca687618d` |
| `Assets/_Project/Rules/Night/INightStage.cs` | `540ef18a5baa52d6b7afbc826286cc1b` |
| `Assets/_Project/Rules/Night/RoleBlockStage.cs` | `6da3074ed87352e699f2e5b34c3aaaa8` |
| `Assets/_Project/Rules/Night/ForgeryStage.cs` | `0f4c1499aa9a4bf6d3011ab79191312b` |
| `Assets/_Project/Rules/Night/VisitStage.cs` | `f81d3ecd2163558b9dd42f0cc780c964` |
| `Assets/_Project/Rules/Night/ProtectionStage.cs` | `0d19f461847f5d6d8bf1e6fb2c791435` |
| `Assets/_Project/Rules/Night/AttackStage.cs` | `5564fb6530c8572ca71638ad5ef5b069` |
| `Assets/_Project/Rules/Night/WatchStage.cs` | `b9b5467a2a00302842209eabec03756c` |
| `Assets/_Project/Rules/Night/InvestigationStage.cs` | `19ee936b9df25f0e992ee899f2a7a91a` |
| `Assets/_Project/Rules/Night/ReportKind.cs` | `2a89c0346af857b6b89828188fdc0f8e` |
| `Assets/_Project/Rules/Night/ReportedNightResult.cs` | `fbc784adcecb4997bde53fbca5555f45` |
| `Assets/_Project/Rules/Night/NightDebugRecord.cs` | `81f3943f058947c98c58af1a10f79813` |
| `Assets/_Project/Rules/Night/TeamNightRecord.cs` | `e6c43767d0b04b6681b161dd842df9c0` |
| `Assets/_Project/Rules/Night/NightOrder.cs` | `de8a987598fd59f189fae0fdb57cde8c` |
| `Assets/_Project/Rules/Night/Visit.cs` | `563b4a7178b15260bbd8e4907ae947c3` |
| `Assets/_Project/Rules/Reasoning/TeamNightPlan.cs` | `53cce7ddc0b15fcb9a81ed97cf6bc06d` |
| `Assets/_Project/Rules/Reasoning/TeamProposal.cs` | `f779dd1751d1552993b179537062e82c` |
| `Assets/_Project/Rules/Reasoning/FactionEstimate.cs` | `6228fb72aac95f41b731c4e0c9688def` |
| `Assets/_Project/Rules/Reasoning/RoleEstimate.cs` | `7eb205e541c25b38a7138c1ddcabc3f2` |
| `Assets/_Project/Rules/Reasoning/EvidenceKind.cs` | `ad2ceb45f64a5d5c87d3cf5aa9d63c0c` |
| `Assets/_Project/Data/Dialogue/DialogueRecord.cs` | `1f907b36b1e6530e94a4a271bc9137a8` |
| `Assets/_Project/Data/Dialogue/DialogueConditionRecord.cs` | `adedc6cd09d2593fa5843dde3f1d177d` |
| `Assets/_Project/Data/Dialogue/DialogueMetadataRecord.cs` | `fd4ea4301d8b517cb4661a677166d467` |
| `Assets/_Project/Data/Dialogue/GameTextEntry.cs` | `6228d5b3fef940698bf13ae2e9a2e93d` |
