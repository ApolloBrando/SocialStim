using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SocialStim.Rules.Roles;

namespace SocialStim.Tests.EditMode
{
    /// <summary>Loads actual committed role assets without depending on Unity's asset database.</summary>
    internal static class MissionTestContent
    {
        internal static IReadOnlyList<RoleSpec> Roles()
        {
#if UNITY_EDITOR
            string root = Path.Combine(UnityEngine.Application.dataPath, "_Project/ScriptableObjects/Roles/Mission");
#else
            string root = Path.Combine(TestContextDirectory(), "MissionRoles");
#endif
            return Directory.GetFiles(root, "*.asset").OrderBy(p => p, StringComparer.Ordinal).Select(path => {
                string text = File.ReadAllText(path);
                string field(string name) => Regex.Match(text, @"^  _" + name + @": (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
                int number(string name) => int.Parse(field(name), System.Globalization.CultureInfo.InvariantCulture);
                return new RoleSpec(new RoleId(field("id")), field("displayName"), (Faction)number("faction"),
                    (RoleCategory)number("category"), (AbilityKind)number("ability"), number("attack"), number("defense"),
                    number("protection"), number("charges"), (Faction)number("appearance"), number("unique") != 0,
                    number("canClaimTruthfully") != 0, number("canBluff") != 0, (RoleFlags)number("flags"), (RoleObjective)number("objective"));
            }).ToList().AsReadOnly();
        }
#if !UNITY_EDITOR
        private static string TestContextDirectory() => NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#endif
        internal static RoleSpec Role(string id) => Roles().Single(r => r.Id.Value == id);
    }
}
