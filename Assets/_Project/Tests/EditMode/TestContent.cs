using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    internal static class TestContent
    {
        internal static IReadOnlyList<RoleSpec> Roles() => new[]
        {
            Role("inspector", Faction.Town, RoleCategory.TownInvestigative, AbilityKind.Investigate),
            Role("physician", Faction.Town, RoleCategory.TownProtective, AbilityKind.Protect, protection: 1),
            Role("interrupter", Faction.Town, RoleCategory.TownSupport, AbilityKind.Block),
            Role("assassin", Faction.Antagonist, RoleCategory.AntagonistKilling, AbilityKind.Attack, attack: 1),
            Role("survivor", Faction.Neutral, RoleCategory.NeutralBenign, AbilityKind.SelfProtect, protection: 1, charges: 2),
        };

        internal static RoleSpec Role(string id, Faction faction, RoleCategory category, AbilityKind ability,
            int attack = 0, int defense = 0, int protection = 0, int charges = -1)
            => new RoleSpec(new RoleId(id), id, faction, category, ability, attack, defense, protection, charges, faction);

        internal static IReadOnlyList<DialogueLine> Lines() => Enum.GetValues(typeof(Personality)).Cast<Personality>()
            .SelectMany(trait => Enum.GetValues(typeof(SpeechIntent)).Cast<SpeechIntent>()
                .SelectMany(intent => Enumerable.Range(0, 4).Select(index => new DialogueLine($"{trait}.{intent}.{index}", intent,
                    $"PLACEHOLDER {index}: {{speaker}} {intent} {{target}} {{role}} {{result}} day {{day}}", requiredTrait: trait))))
            .ToList().AsReadOnly();

        internal static IReadOnlyList<DialogueLine> AuthoredLines()
        {
#if UNITY_EDITOR
            string directory = System.IO.Path.Combine(UnityEngine.Application.dataPath, "_Project/Data/Dialogue");
#else
            string directory = TestContextDirectory();
#endif
            return SocialStim.Data.Dialogue.DialogueDocument.ParseMany(System.IO.Directory.GetFiles(directory, "*Dialogue.json")
                .OrderBy(path => path, StringComparer.Ordinal).Select(System.IO.File.ReadAllText));
        }
#if !UNITY_EDITOR
        private static string TestContextDirectory() => NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#endif

        internal static RunSession Scenario(params string[] roles) => RunFactory.CreateScenario(new RunOptions(123, discussionRounds: 1, votingMode: VotingMode.Trial),
            roles.Select((role, index) => new CastMemberSpec($"Person{index}", new RoleId(role))), Roles(), Lines());

        internal static string[] Names() => Enumerable.Range(0, 20).Select(i => $"Person{i}").ToArray();
    }
}
