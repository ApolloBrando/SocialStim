using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.PresentationCues
{
    /// <summary>Consumes only scoped observations and public assertions; no NPC model access.</summary>
    public sealed class VisibleCueResolver
    {
        private readonly Dictionary<CharacterId, RoleId> _claims = new Dictionary<CharacterId, RoleId>();
        private readonly HashSet<CharacterId> _dead = new HashSet<CharacterId>();
        public PresentationCue Resolve(GameObservation observation, PlayerView view)
        {
            if (observation.Cue == GameCue.SpeechDelivered)
            {
                if ((observation.Intent == SpeechIntent.ClaimRole || observation.Intent == SpeechIntent.CounterClaim) && observation.Speaker.HasValue && observation.ClaimedRole.HasValue)
                {
                    bool changed = _claims.TryGetValue(observation.Speaker.Value, out RoleId prior) && prior != observation.ClaimedRole.Value;
                    _claims[observation.Speaker.Value] = observation.ClaimedRole.Value;
                    if (changed) return PresentationCue.ContradictedClaim;
                }
                switch (observation.Intent)
                {
                    case SpeechIntent.Accuse: case SpeechIntent.CallForTrial: case SpeechIntent.Deflect: return PresentationCue.Accusation;
                    case SpeechIntent.Defend: return PresentationCue.Defense;
                    default: return PresentationCue.Speech;
                }
            }
            switch (observation.Cue)
            {
                case GameCue.NightFalls: return PresentationCue.Dusk;
                case GameCue.DawnBreaks: return PresentationCue.Dawn;
                case GameCue.NominationMade: return PresentationCue.Nomination;
                case GameCue.TrialStarted: return observation.Target == view.PlayerId ? PresentationCue.NearExposure : PresentationCue.Trial;
                case GameCue.VerdictGuilty: return PresentationCue.Guilty;
                case GameCue.VerdictInnocent: return PresentationCue.Innocent;
                case GameCue.ExecutionCarriedOut: case GameCue.DeathReported:
                    // A no-casualty morning report shares the event family, not a death effect.
                    if (!observation.Target.HasValue) return PresentationCue.None;
                    _dead.Add(observation.Target.Value);
                    if (observation.Target == view.PlayerId) return PresentationCue.PlayerKilled;
                    if (view.Characters.Any(c => c.Id == observation.Target && c.IsTeammate)) return PresentationCue.TeammateLost;
                    if (view.Characters.Count - _dead.Count == 3) return PresentationCue.FinalThree;
                    return observation.Cue == GameCue.ExecutionCarriedOut ? PresentationCue.Execution : PresentationCue.Death;
                case GameCue.RoleRevealed: return PresentationCue.Reveal;
                case GameCue.PlayerTargetedAtNight: return PresentationCue.NarrowEscape;
                case GameCue.AbilityBlocked: return PresentationCue.Blocked;
                case GameCue.ProtectionApplied: return view.Charges == 0 ? PresentationCue.ResourcesDepleted : PresentationCue.None;
                case GameCue.InvestigationResultReceived: return PresentationCue.Investigation;
                case GameCue.VoteTied: return PresentationCue.Tie;
                case GameCue.VictoryAchieved: return PresentationCue.Victory;
                case GameCue.DefeatSuffered: return PresentationCue.Defeat;
                case GameCue.RunDrawn: return PresentationCue.Draw;
                default: return PresentationCue.None;
            }
        }
    }
}
