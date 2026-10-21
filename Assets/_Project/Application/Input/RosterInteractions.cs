using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;

namespace SocialStim.Application.Input
{
    /// <summary>Portable roster projections derived only from legal choices and scoped public history.</summary>
    public static class RosterInteractions
    {
        /// <summary>Public roster as of the dialogue being shown, without queued claims or deaths leaking early.</summary>
        public static Rules.Reasoning.KnownCharacter VisibleParticipant(PlayerView view, CharacterId id, int visibleObservations)
        {
            var participant = view.Characters.First(c => c.Id == id);
            var shown = view.Transcript.Take(System.Math.Max(0, visibleObservations)).ToList();
            var claim = shown.LastOrDefault(o => o.Speaker == id && o.ClaimedRole.HasValue
                && (o.Intent == SpeechIntent.ClaimRole || o.Intent == SpeechIntent.CounterClaim));
            bool alive = visibleObservations >= view.Transcript.Count ? participant.Alive
                : !shown.Any(o => o.Target == id && (o.Cue == GameCue.DeathReported || o.Cue == GameCue.ExecutionCarriedOut));
            return new Rules.Reasoning.KnownCharacter(participant.Id, participant.Name, alive, participant.Suspicion,
                participant.IsTeammate, claim: claim?.ClaimedRole, trust: participant.Trust,
                suspicionValue: participant.SuspicionValue, trustValue: participant.TrustValue, teamRole: participant.TeamRole);
        }
        public static IReadOnlyList<PlayerOption> ForTarget(PlayerInputRequest request, CharacterId target, string category = null)
            => request.Options.Where(o => o.Target == target && (category == null || o.Category == category)).ToList().AsReadOnly();
        /// <summary>One click submits only an unambiguous legal target action. Discussion requires choosing Accuse/Defend first.</summary>
        public static PlayerOption DirectChoice(PlayerInputRequest request, CharacterId target, string category)
        {
            var actions = ForTarget(request, target, category == "Targets" ? null : category);
            if (category == null) actions = actions.Where(o => o.Kind != Rules.Commands.CommandKind.Speak).ToList().AsReadOnly();
            return actions.Count == 1 ? actions[0] : null;
        }
        public static GameObservation ReplyStatement(IEnumerable<GameObservation> transcript, CharacterId speaker, int day)
            => transcript.LastOrDefault(o => o.Day == day && o.Speaker == speaker && o.Cue == GameCue.SpeechDelivered
                && (o.Intent == SpeechIntent.Accuse || o.Intent == SpeechIntent.Deflect || o.Intent == SpeechIntent.Defend || o.Intent == SpeechIntent.Vouch));
        public static string Label(PlayerOption option, GameText text, string name) => option.Kind == Rules.Commands.CommandKind.Speak
            && option.Intent == SpeechIntent.Accuse ? text.Format("ui.accuseTarget", ("name", name))
            : option.Kind == Rules.Commands.CommandKind.Speak && option.Intent == SpeechIntent.Vouch
                ? text.Format("ui.defendTarget", ("name", name)) : option.Label;
        public static IReadOnlyList<GameObservation> PublicRecord(PlayerView view, CharacterId participant)
            => view.Transcript.Where(o => o.Cue == GameCue.SpeechDelivered || o.Cue == GameCue.BallotRevealed
                || o.Cue == GameCue.NominationMade || o.Cue == GameCue.RoleRevealed || o.Cue == GameCue.DeathReported || o.Cue == GameCue.ExecutionCarriedOut)
                .Where(o => o.Speaker == participant || o.Target == participant || o.NightReport != null
                    && o.NightReport.Results.Any(r => r.Target == participant || r.Subject == participant)).ToList().AsReadOnly();
    }
}
