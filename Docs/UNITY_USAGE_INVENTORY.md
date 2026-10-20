# Unity API usage inventory

Snapshot after the Unity workflow pass. Every C# file is listed below; imports identify direct Unity coupling, including test-only or conditional imports. Rules/Application retain `noEngineReferences` assembly boundaries.

| File | Lines | Direct Unity imports |
| --- | ---: | --- |
| Assets/_Project/Application/Input/ActionLabels.cs | 15 | None |
| Assets/_Project/Application/Input/IPlayerInputProvider.cs | 11 | None |
| Assets/_Project/Application/Input/InteractionAction.cs | 5 | None |
| Assets/_Project/Application/Input/InteractionMenu.cs | 32 | None |
| Assets/_Project/Application/Input/PlayerInputRequest.cs | 15 | None |
| Assets/_Project/Application/Input/PlayerOption.cs | 26 | None |
| Assets/_Project/Application/PresentationCues/Daylight.cs | 17 | None |
| Assets/_Project/Application/PresentationCues/DefaultCueProfiles.cs | 29 | None |
| Assets/_Project/Application/PresentationCues/DisplayAcknowledgement.cs | 10 | None |
| Assets/_Project/Application/PresentationCues/DisplayCompletion.cs | 4 | None |
| Assets/_Project/Application/PresentationCues/DisplayPlayback.cs | 99 | None |
| Assets/_Project/Application/PresentationCues/EffectBlend.cs | 4 | None |
| Assets/_Project/Application/PresentationCues/EffectChannel.cs | 4 | None |
| Assets/_Project/Application/PresentationCues/EffectEnvelope.cs | 4 | None |
| Assets/_Project/Application/PresentationCues/EffectFrame.cs | 13 | None |
| Assets/_Project/Application/PresentationCues/EffectLifetime.cs | 5 | None |
| Assets/_Project/Application/PresentationCues/EffectProfile.cs | 54 | None |
| Assets/_Project/Application/PresentationCues/EffectSequence.cs | 25 | None |
| Assets/_Project/Application/PresentationCues/EffectStep.cs | 29 | None |
| Assets/_Project/Application/PresentationCues/EffectsSettings.cs | 23 | None |
| Assets/_Project/Application/PresentationCues/GlyphEffect.cs | 41 | None |
| Assets/_Project/Application/PresentationCues/GlyphEffectKind.cs | 7 | None |
| Assets/_Project/Application/PresentationCues/GlyphEffectSampler.cs | 61 | None |
| Assets/_Project/Application/PresentationCues/GlyphEffectSpan.cs | 15 | None |
| Assets/_Project/Application/PresentationCues/GlyphStyleCatalog.cs | 43 | None |
| Assets/_Project/Application/PresentationCues/GlyphTransform.cs | 26 | None |
| Assets/_Project/Application/PresentationCues/IGameView.cs | 15 | None |
| Assets/_Project/Application/PresentationCues/PresentationCue.cs | 10 | None |
| Assets/_Project/Application/PresentationCues/SemanticMarkup.cs | 82 | None |
| Assets/_Project/Application/PresentationCues/SemanticSpan.cs | 11 | None |
| Assets/_Project/Application/PresentationCues/SemanticStyle.cs | 5 | None |
| Assets/_Project/Application/PresentationCues/SemanticText.cs | 28 | None |
| Assets/_Project/Application/PresentationCues/SpeechPacing.cs | 18 | None |
| Assets/_Project/Application/PresentationCues/VisibleCueResolver.cs | 60 | None |
| Assets/_Project/Application/Sessions/GameController.cs | 128 | None |
| Assets/_Project/Application/Settings/GamePreferences.cs | 40 | None |
| Assets/_Project/Application/Settings/NewRunSettings.cs | 39 | None |
| Assets/_Project/Application/Settings/PreferenceKey.cs | 6 | None |
| Assets/_Project/Application/Settings/PreferencesEditor.cs | 53 | None |
| Assets/_Project/Application/Settings/SettingsCodec.cs | 62 | None |
| Assets/_Project/Application/Settings/SettingsStore.cs | 45 | None |
| Assets/_Project/Data/Definitions/GameCatalog.cs | 62 | UnityEngine |
| Assets/_Project/Data/Definitions/GameText.cs | 21 | UnityEngine |
| Assets/_Project/Data/Definitions/ReasoningDefinition.cs | 23 | UnityEngine |
| Assets/_Project/Data/Definitions/RoleDefinition.cs | 31 | UnityEngine |
| Assets/_Project/Data/Definitions/RoleListDefinition.cs | 34 | UnityEngine |
| Assets/_Project/Data/Dialogue/DialogueCatalogue.cs | 53 | None |
| Assets/_Project/Data/Dialogue/DialogueConditionRecord.cs | 22 | None |
| Assets/_Project/Data/Dialogue/DialogueDocument.cs | 40 | None |
| Assets/_Project/Data/Dialogue/DialogueMetadataRecord.cs | 15 | None |
| Assets/_Project/Data/Dialogue/DialogueRecord.cs | 39 | None |
| Assets/_Project/Data/Dialogue/DialogueSchema.cs | 69 | None |
| Assets/_Project/Data/Dialogue/EditableTextDocument.cs | 117 | None |
| Assets/_Project/Data/Dialogue/TextDocument.cs | 48 | None |
| Assets/_Project/Editor/ContentBuildValidation.cs | 23 | UnityEditor.Build, UnityEditor.Build.Reporting |
| Assets/_Project/Editor/ContentImportValidation.cs | 27 | UnityEditor, UnityEngine |
| Assets/_Project/Editor/ContentValidation.cs | 83 | UnityEditor, UnityEditor.SceneManagement, UnityEngine |
| Assets/_Project/Editor/DialogueCatalogueExport.cs | 32 | UnityEditor, UnityEngine |
| Assets/_Project/Editor/DialogueEditBuffer.cs | 14 | UnityEngine |
| Assets/_Project/Editor/DialogueEditorWindow.cs | 167 | UnityEditor, UnityEditor.UIElements, UnityEngine, UnityEngine.UIElements |
| Assets/_Project/Editor/EffectsPlayground/EffectsPlaygroundWindow.cs | 110 | UnityEditor, UnityEngine |
| Assets/_Project/Editor/GameCatalogEditor.cs | 22 | UnityEditor, UnityEngine |
| Assets/_Project/Editor/RoleDefinitionEditor.cs | 22 | UnityEditor |
| Assets/_Project/Editor/ValidatedDefinitionEditor.cs | 32 | UnityEditor, UnityEngine |
| Assets/_Project/Presentation/Effects/CueProfileDefinition.cs | 33 | UnityEngine |
| Assets/_Project/Presentation/Effects/CueProfileEntry.cs | 12 | None |
| Assets/_Project/Presentation/Effects/EffectTrackDefinition.cs | 20 | UnityEngine |
| Assets/_Project/Presentation/Effects/ScreenEffectOverlay.cs | 67 | UnityEngine, UnityEngine.UI |
| Assets/_Project/Presentation/Effects/TextEffectRenderer.cs | 113 | UnityEngine, Unity.Profiling |
| Assets/_Project/Presentation/Effects/TextPalette.cs | 33 | UnityEngine |
| Assets/_Project/Presentation/Effects/TextStyleDefinition.cs | 40 | UnityEngine |
| Assets/_Project/Presentation/Text/AuxiliaryDrawer.cs | 87 | UnityEngine, UnityEngine.EventSystems, UnityEngine.UI |
| Assets/_Project/Presentation/Text/ChoiceFocusIndicator.cs | 50 | UnityEngine, UnityEngine.EventSystems, UnityEngine.UI |
| Assets/_Project/Presentation/Text/CurrentLinePointer.cs | 17 | UnityEngine, UnityEngine.EventSystems |
| Assets/_Project/Presentation/Text/GameBootstrap.cs | 107 | UnityEngine, UnityEngine.InputSystem |
| Assets/_Project/Presentation/Text/GamePresenter.cs | 33 | None |
| Assets/_Project/Presentation/Text/ParticipantCard.cs | 54 | UnityEngine, UnityEngine.UI |
| Assets/_Project/Presentation/Text/SettingsPanel.cs | 174 | UnityEngine, UnityEngine.EventSystems, UnityEngine.UI |
| Assets/_Project/Presentation/Text/TextGameView.cs | 345 | UnityEngine, UnityEngine.EventSystems, UnityEngine.InputSystem, UnityEngine.UI |
| Assets/_Project/Presentation/Text/TextInputActions.cs | 37 | UnityEngine.InputSystem |
| Assets/_Project/Presentation/Text/TextUiFactory.cs | 121 | UnityEngine, UnityEngine.UI |
| Assets/_Project/Presentation/Text/UiEventSystem.cs | 32 | UnityEngine, UnityEngine.EventSystems, UnityEngine.InputSystem.UI |
| Assets/_Project/Presentation/Text/UiPreferences.cs | 39 | UnityEngine |
| Assets/_Project/Rules/Cast/CastMemberSpec.cs | 24 | None |
| Assets/_Project/Rules/Cast/CharacterId.cs | 32 | None |
| Assets/_Project/Rules/Cast/Participant.cs | 23 | None |
| Assets/_Project/Rules/Cast/Personality.cs | 8 | None |
| Assets/_Project/Rules/Cast/RoleList.cs | 103 | None |
| Assets/_Project/Rules/Cast/RoleSlot.cs | 16 | None |
| Assets/_Project/Rules/Cast/RunFactory.cs | 85 | None |
| Assets/_Project/Rules/Commands/CommandKind.cs | 5 | None |
| Assets/_Project/Rules/Commands/GameCommand.cs | 52 | None |
| Assets/_Project/Rules/Commands/PluralityResult.cs | 21 | None |
| Assets/_Project/Rules/Commands/Verdict.cs | 5 | None |
| Assets/_Project/Rules/Commands/VoteResolver.cs | 41 | None |
| Assets/_Project/Rules/Dialogue/DialogueCondition.cs | 66 | None |
| Assets/_Project/Rules/Dialogue/DialogueConditionKind.cs | 5 | None |
| Assets/_Project/Rules/Dialogue/DialogueContext.cs | 50 | None |
| Assets/_Project/Rules/Dialogue/DialogueLine.cs | 60 | None |
| Assets/_Project/Rules/Dialogue/DialogueMetadata.cs | 16 | None |
| Assets/_Project/Rules/Dialogue/DialogueSelection.cs | 15 | None |
| Assets/_Project/Rules/Dialogue/DialogueSelector.cs | 85 | None |
| Assets/_Project/Rules/Dialogue/NightActionText.cs | 15 | None |
| Assets/_Project/Rules/Dialogue/SpeechContract.cs | 25 | None |
| Assets/_Project/Rules/Dialogue/SpeechIntent.cs | 5 | None |
| Assets/_Project/Rules/Dialogue/TextCatalogContract.cs | 329 | None |
| Assets/_Project/Rules/Dialogue/TextTemplates.cs | 53 | None |
| Assets/_Project/Rules/Night/AttackStage.cs | 24 | None |
| Assets/_Project/Rules/Night/ForgeryStage.cs | 19 | None |
| Assets/_Project/Rules/Night/INightStage.cs | 11 | None |
| Assets/_Project/Rules/Night/InvestigationStage.cs | 21 | None |
| Assets/_Project/Rules/Night/NightActor.cs | 24 | None |
| Assets/_Project/Rules/Night/NightContext.cs | 39 | None |
| Assets/_Project/Rules/Night/NightLegality.cs | 24 | None |
| Assets/_Project/Rules/Night/NightOrder.cs | 15 | None |
| Assets/_Project/Rules/Night/NightReport.cs | 25 | None |
| Assets/_Project/Rules/Night/NightResolution.cs | 25 | None |
| Assets/_Project/Rules/Night/NightResolver.cs | 42 | None |
| Assets/_Project/Rules/Night/ProtectionStage.cs | 24 | None |
| Assets/_Project/Rules/Night/ReportKind.cs | 5 | None |
| Assets/_Project/Rules/Night/RoleBlockStage.cs | 24 | None |
| Assets/_Project/Rules/Night/Visit.cs | 15 | None |
| Assets/_Project/Rules/Night/VisitStage.cs | 21 | None |
| Assets/_Project/Rules/Night/WatchStage.cs | 24 | None |
| Assets/_Project/Rules/Phases/DiscussionExchange.cs | 63 | None |
| Assets/_Project/Rules/Phases/DiscussionSchedule.cs | 34 | None |
| Assets/_Project/Rules/Phases/GameCue.cs | 5 | None |
| Assets/_Project/Rules/Phases/GameObservation.cs | 36 | None |
| Assets/_Project/Rules/Phases/GamePhase.cs | 5 | None |
| Assets/_Project/Rules/Phases/PhaseMachine.cs | 40 | None |
| Assets/_Project/Rules/Phases/PlayerView.cs | 87 | None |
| Assets/_Project/Rules/Phases/RunOptions.cs | 48 | None |
| Assets/_Project/Rules/Phases/RunSession.cs | 597 | None |
| Assets/_Project/Rules/Phases/VotingMode.cs | 5 | None |
| Assets/_Project/Rules/Randomness/IRandomSource.cs | 12 | None |
| Assets/_Project/Rules/Randomness/SeededRandom.cs | 46 | None |
| Assets/_Project/Rules/Reasoning/AssemblyInfo.cs | 4 | None |
| Assets/_Project/Rules/Reasoning/BasicNpcStrategy.cs | 80 | None |
| Assets/_Project/Rules/Reasoning/BeliefDistribution.cs | 102 | None |
| Assets/_Project/Rules/Reasoning/BeliefState.cs | 167 | None |
| Assets/_Project/Rules/Reasoning/EvidenceKind.cs | 5 | None |
| Assets/_Project/Rules/Reasoning/EvidenceRecord.cs | 37 | None |
| Assets/_Project/Rules/Reasoning/FactionEstimate.cs | 25 | None |
| Assets/_Project/Rules/Reasoning/INpcStrategy.cs | 19 | None |
| Assets/_Project/Rules/Reasoning/KnowledgeView.cs | 48 | None |
| Assets/_Project/Rules/Reasoning/KnownCharacter.cs | 45 | None |
| Assets/_Project/Rules/Reasoning/ReasoningSettings.cs | 38 | None |
| Assets/_Project/Rules/Reasoning/ReportAssertion.cs | 15 | None |
| Assets/_Project/Rules/Reasoning/RoleEstimate.cs | 14 | None |
| Assets/_Project/Rules/Reasoning/SetupKnowledge.cs | 25 | None |
| Assets/_Project/Rules/Reasoning/SocialNpcStrategy.cs | 138 | None |
| Assets/_Project/Rules/Reasoning/TeamNightPlan.cs | 14 | None |
| Assets/_Project/Rules/Reasoning/TeamNightPlanner.cs | 28 | None |
| Assets/_Project/Rules/Reasoning/TeamProposal.cs | 17 | None |
| Assets/_Project/Rules/Roles/AbilityKind.cs | 5 | None |
| Assets/_Project/Rules/Roles/Faction.cs | 5 | None |
| Assets/_Project/Rules/Roles/RoleCategory.cs | 5 | None |
| Assets/_Project/Rules/Roles/RoleId.cs | 30 | None |
| Assets/_Project/Rules/Roles/RoleSpec.cs | 61 | None |
| Assets/_Project/Rules/Victory/CapabilityWinCondition.cs | 33 | None |
| Assets/_Project/Rules/Victory/IWinCondition.cs | 12 | None |
| Assets/_Project/Rules/Victory/OutcomeKind.cs | 5 | None |
| Assets/_Project/Rules/Victory/StarterWinCondition.cs | 21 | None |
| Assets/_Project/Tests/EditMode/AdvancedRoleTests.cs | 274 | None |
| Assets/_Project/Tests/EditMode/CoreLoopTests.cs | 78 | None |
| Assets/_Project/Tests/EditMode/DeterministicRunTests.cs | 66 | None |
| Assets/_Project/Tests/EditMode/DialogueAndKnowledgeTests.cs | 87 | None |
| Assets/_Project/Tests/EditMode/DialogueConditionTests.cs | 80 | None |
| Assets/_Project/Tests/EditMode/DialogueImportTests.cs | 56 | None |
| Assets/_Project/Tests/EditMode/DiscussionVoteTests.cs | 234 | None |
| Assets/_Project/Tests/EditMode/EditableTextTests.cs | 58 | None |
| Assets/_Project/Tests/EditMode/GlyphEffectsTests.cs | 103 | None |
| Assets/_Project/Tests/EditMode/InformationBoundaryTests.cs | 40 | None |
| Assets/_Project/Tests/EditMode/InteractionTests.cs | 116 | None |
| Assets/_Project/Tests/EditMode/MilestoneIntegrationTests.cs | 96 | None |
| Assets/_Project/Tests/EditMode/NightResolverTests.cs | 112 | None |
| Assets/_Project/Tests/EditMode/PlayerAgencyTests.cs | 107 | None |
| Assets/_Project/Tests/EditMode/PlaytestPresentationTests.cs | 50 | None |
| Assets/_Project/Tests/EditMode/PresentationEffectsTests.cs | 232 | None |
| Assets/_Project/Tests/EditMode/RepositoryAuditTests.cs | 151 | None |
| Assets/_Project/Tests/EditMode/ScenarioStrategy.cs | 22 | None |
| Assets/_Project/Tests/EditMode/SettingsTests.cs | 87 | None |
| Assets/_Project/Tests/EditMode/SocialEvidenceTests.cs | 153 | None |
| Assets/_Project/Tests/EditMode/SocialStrategyTests.cs | 106 | None |
| Assets/_Project/Tests/EditMode/TeamSessionTests.cs | 48 | None |
| Assets/_Project/Tests/EditMode/TestContent.cs | 42 | None |
| Assets/_Project/Tests/EditMode/TextCatalogTests.cs | 67 | None |
| Assets/_Project/Tests/PlayMode/CopyFixture.cs | 17 | None |
| Assets/_Project/Tests/PlayMode/DisplayOptionsTests.cs | 44 | UnityEngine, UnityEngine.UI |
| Assets/_Project/Tests/PlayMode/InteractionViewTests.cs | 52 | UnityEngine, UnityEngine.TestTools, UnityEngine.UI |
| Assets/_Project/Tests/PlayMode/PresenterDisplayTests.cs | 50 | None |
| Assets/_Project/Tests/PlayMode/TextEffectsAdapterTests.cs | 61 | UnityEngine |
| Assets/_Project/Tests/PlayMode/UnityWorkflowTests.cs | 56 | UnityEngine, UnityEngine.EventSystems, UnityEngine.InputSystem, UnityEngine.InputSystem.LowLevel, UnityEngine.InputSystem.UI |
