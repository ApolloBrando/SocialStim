using System;
using System.Collections.Generic;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>One observer's evidence-weight model; consumes assertions and permitted results.</summary>
    internal sealed class BeliefState
    {
        private readonly Dictionary<CharacterId, int> _suspicion = new Dictionary<CharacterId, int>();
        private readonly Dictionary<CharacterId, RoleId> _claims = new Dictionary<CharacterId, RoleId>();
        private readonly Dictionary<string, Faction> _reports = new Dictionary<string, Faction>();
        private readonly List<KeyValuePair<CharacterId, CharacterId>> _accusations = new List<KeyValuePair<CharacterId, CharacterId>>();
        private readonly Personality _personality;

        internal BeliefState(IEnumerable<CharacterId> characters, Personality personality, IRandomSource random)
        {
            _personality = personality;
            foreach (CharacterId character in characters) _suspicion.Add(character, 12 + random.Next(13));
        }

        internal int Suspicion(CharacterId target) => _suspicion[target];
        internal RoleId? Claim(CharacterId target) => _claims.TryGetValue(target, out RoleId claim) ? claim : (RoleId?)null;

        internal void Observe(CharacterId speaker, GameCommand speech, int day)
        {
            int credibility = _suspicion[speaker] > 65 ? 3 : 9;
            if (_personality.HasFlag(Personality.Gullible)) credibility += 4;
            if (speech.Intent == SpeechIntent.Accuse || speech.Intent == SpeechIntent.CallForTrial || speech.Intent == SpeechIntent.Deflect)
            {
                if (speech.Target.HasValue)
                {
                    Change(speech.Target.Value, credibility);
                    _accusations.Add(new KeyValuePair<CharacterId, CharacterId>(speaker, speech.Target.Value));
                }
                if (speech.Intent == SpeechIntent.Deflect) Change(speaker, 4);
            }
            if (speech.Intent == SpeechIntent.Vouch && speech.Target.HasValue) Change(speech.Target.Value, -credibility);
            if (speech.Intent == SpeechIntent.Defend) Change(speaker, -3);
            if (speech.ClaimedRole.HasValue)
            {
                if (_claims.TryGetValue(speaker, out RoleId prior) && prior != speech.ClaimedRole.Value) Change(speaker, 24);
                _claims[speaker] = speech.ClaimedRole.Value;
            }
            if (speech.Intent == SpeechIntent.ReportInvestigation && speech.Target.HasValue && speech.ReportedFaction.HasValue)
            {
                string key = $"{speaker.Value}:{speech.Target.Value.Value}:{day - 1}";
                if (_reports.TryGetValue(key, out Faction prior) && prior != speech.ReportedFaction.Value) Change(speaker, 30);
                _reports[key] = speech.ReportedFaction.Value;
                Change(speech.Target.Value, speech.ReportedFaction == Faction.Antagonist ? credibility + 6 : -credibility);
            }
        }

        internal void Receive(NightReport report)
        {
            if (report.Kind == ReportKind.Investigation && report.Target.HasValue)
                Change(report.Target.Value, report.Alignment == Faction.Antagonist ? 65 : -45);
        }

        internal void Reveal(CharacterId target, RoleSpec role)
        {
            if (_claims.TryGetValue(target, out RoleId claim) && claim != role.Id) Change(target, 35);
            foreach (KeyValuePair<CharacterId, CharacterId> accusation in _accusations)
                if (accusation.Value == target) Change(accusation.Key, role.Faction == Faction.Antagonist ? -12 : 8);
        }

        internal void ObserveVote(CharacterId voter, CharacterId defendant, Verdict verdict)
        {
            if (_suspicion[defendant] >= 60 && verdict == Verdict.Innocent) Change(voter, 6);
            if (_suspicion[defendant] < 20 && verdict == Verdict.Guilty) Change(voter, 4);
        }

        private void Change(CharacterId target, int amount) => _suspicion[target] = Math.Max(0, Math.Min(100, _suspicion[target] + amount));
    }
}
