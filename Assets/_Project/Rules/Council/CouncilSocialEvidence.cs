using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Council
{
    /// <summary>Rebuilds attributed evidence after reveals; persuasion is a product of reason, skills, credibility and affinity.</summary>
    internal sealed class CouncilSocialEvidence
    {
        private readonly CouncilKnowledge _knowledge;
        private readonly Dictionary<CharacterId, double> _credibility = new Dictionary<CharacterId, double>();
        private readonly Dictionary<CharacterId, double> _affinity = new Dictionary<CharacterId, double>();
        private readonly Dictionary<CharacterId, double> _pressure = new Dictionary<CharacterId, double>();
        private readonly Dictionary<CharacterId, double> _conduct = new Dictionary<CharacterId, double>();
        internal CouncilSocialEvidence(CouncilKnowledge knowledge)
        {
            _knowledge = knowledge;
            foreach (var actor in knowledge.Characters)
            { _credibility[actor.Id] = 0; _affinity[actor.Id] = 0; _pressure[actor.Id] = 0; _conduct[actor.Id] = 0; }
            foreach (var actor in knowledge.Characters)
            {
                if (CouncilReasoning.HasReason(knowledge, actor.Id, ReasonType.Contradiction)) _credibility[actor.Id] -= 2.3;
                if (CouncilReasoning.HasReason(knowledge, actor.Id, ReasonType.ClaimConflict)) _credibility[actor.Id] -= 0.4;
            }
            foreach (var e in knowledge.Events)
            {
                if (!e.Actor.HasValue || !e.Target.HasValue) continue;
                var speaker = e.Actor.Value; var target = e.Target.Value;
                bool defending = e.Intent == SpeechIntent.Defend || e.Intent == SpeechIntent.Vouch;
                bool accusing = e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.Deflect;
                var revealed = knowledge.Characters.First(c => c.Id == target).RevealedFaction;
                if (revealed.HasValue && speaker != target)
                {
                    if (e.Kind == CouncilEventKind.Vote || accusing)
                    { double success = revealed == Faction.Antagonist ? 0.24 : revealed == Faction.Town ? -0.09 : 0.02; _credibility[speaker] += success; _conduct[speaker] -= success; }
                    if (defending)
                    { double mistake = revealed == Faction.Antagonist ? 0.32 : -0.08; _credibility[speaker] -= mistake; _conduct[speaker] += mistake; }
                }
                if (target == knowledge.Self && speaker != knowledge.Self)
                {
                    double charm = 0.5 + knowledge.Skill(speaker).Get(StatKind.Charm) / 10.0;
                    _affinity[speaker] += (defending ? 0.10 : accusing ? -0.13 : e.Intent == SpeechIntent.Agree ? 0.07 : 0) * charm;
                }

            }
            foreach (var e in knowledge.Events.Where(e => e.Actor.HasValue && e.Target.HasValue))
            {
                bool defending = e.Intent == SpeechIntent.Defend || e.Intent == SpeechIntent.Vouch;
                bool accusing = e.Intent == SpeechIntent.Accuse || e.Intent == SpeechIntent.Deflect;
                if (!defending && !accusing) continue;
                var speaker = e.Actor.Value; var target = e.Target.Value;
                var stats = knowledge.Skill(speaker); var listener = knowledge.Skill(knowledge.Self);
                double reason = Strength(knowledge, target, e.Reason);
                double presentation = 0.35 + stats.Get(StatKind.Charisma) / 20.0
                    + stats.Get(e.Reason == ReasonType.Logic || e.Reason == ReasonType.Evidence ? StatKind.Logic : StatKind.Performance) / 25.0;
                double receptivity = knowledge.Traits.HasFlag(Personality.Gullible) ? 1.3 : knowledge.Traits.HasFlag(Personality.Cautious) ? 0.75 : 1;
                double critical = 1.2 - listener.Get(StatKind.Logic) / 25.0;
                double influence = reason * presentation * Credibility(speaker) * (1 + Affinity(speaker) * 0.35) * receptivity * critical;
                // Repeated identical assertions saturate instead of providing unlimited additive persuasion.
                double repetition = knowledge.Events.Count(p => p.Sequence <= e.Sequence && p.Actor == speaker && p.Target == target && p.Intent == e.Intent && p.Reason == e.Reason);
                _pressure[target] += (accusing ? 1 : -1) * influence / Math.Pow(repetition, 1.4);
            }
        }
        internal double Credibility(CharacterId actor) => 0.08 + 0.84 * Sigmoid(_credibility[actor]);
        internal double Affinity(CharacterId actor) => Math.Tanh(_affinity[actor]);
        internal double Exposure => Sigmoid(_pressure[_knowledge.Self] * 2 - 1.4);
        internal double Tension => Math.Min(1, _knowledge.Characters.Count(c => !c.Alive) / (double)_knowledge.Characters.Count + (_knowledge.Day - 1) * 0.04);
        internal double LogLikelihood(CharacterId actor, RoleSpec role)
        {
            bool hostile = role.Faction == Faction.Antagonist || role.Objective == RoleObjective.LastSurvivor;
            double score = Math.Tanh(_pressure[actor]) * (hostile ? 1 : -1) + _conduct[actor] * (hostile ? 1 : -0.5);
            var claims = _knowledge.Events.Where(e => e.Kind == CouncilEventKind.Claim && e.Actor == actor).ToList();
            foreach (var claim in claims.GroupBy(e => e.Claim).Select(g => g.First()))
                score += claim.Claim == role.Id ? Math.Log(2.6) : role.CanBluff ? Math.Log(0.8) : Math.Log(0.035);
            if (CouncilReasoning.HasReason(_knowledge, actor, ReasonType.Contradiction))
                score += role.CanBluff ? 0.7 : -1.8;
            if (_knowledge.Events.Any(e => e.Kind == CouncilEventKind.Death && e.Target == actor)
                && role.Flags.HasFlag(RoleFlags.NightImmune) && !role.Flags.HasFlag(RoleFlags.Backlash)) return double.NegativeInfinity;
            int statements = _knowledge.Events.Count(e => e.Day == _knowledge.Day && e.Actor == actor && e.Intent.HasValue);
            if (statements == 0 && _knowledge.Round > 2 && hostile)
                score += 0.08 * _knowledge.Skill(actor).Get(StatKind.Stealth) / 5.0;
            return score;
        }
        internal static double Strength(CouncilKnowledge k, CharacterId target, ReasonType reason)
        {
            if (!CouncilReasoning.HasReason(k, target, reason)) return 0.04;
            switch (reason)
            {
                case ReasonType.Contradiction: return 0.95;
                case ReasonType.ResultMismatch: return 0.85;
                case ReasonType.ClaimConflict: return 0.75;
                case ReasonType.Evidence: return 0.70;
                case ReasonType.VoteHistory: case ReasonType.DefendedEvil: return 0.60;
                case ReasonType.Logic: case ReasonType.Alibi: return 0.40;
                case ReasonType.ClaimTiming: case ReasonType.PowerRoleKilled: return 0.32;
                case ReasonType.Silence: case ReasonType.OverEagerness: case ReasonType.NightPattern: return 0.22;
                case ReasonType.Emotional: return 0.18;
                default: return 0.12;
            }
        }
        private static double Sigmoid(double logOdds) => 1 / (1 + Math.Exp(-Math.Max(-20, Math.Min(20, logOdds))));
    }
}
