using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Randomness;

namespace SocialStim.Rules.Council
{
    /// <summary>Maps accepted semantic actions to authored text and public assertions.</summary>
    internal sealed class CouncilSpeechService
    {
        private readonly CouncilWorld _world;
        private readonly DialogueSelector _dialogue;
        internal CouncilSpeechService(CouncilWorld world, IEnumerable<DialogueLine> dialogue)
        { _world = world; _dialogue = new DialogueSelector(dialogue, new SeededRandom(world.Options.Seed ^ 0xC8013EA4u)); }
        internal SpeechIntent Intent(CouncilActor actor, CouncilAction action)
        {
            if (action.Intent.HasValue) return action.Intent.Value;
            switch (action.Kind)
            {
                case CouncilActionKind.Accuse: return SpeechIntent.Accuse;
                case CouncilActionKind.Defend: return action.Target == actor.Id ? SpeechIntent.Defend : SpeechIntent.Vouch;
                case CouncilActionKind.Claim:
                    return action.Claim.HasValue && _world.Catalog.Any(r => r.Id == action.Claim.Value && r.Unique)
                        && _world.Events.Any(e => e.Kind == CouncilEventKind.Claim && e.Claim == action.Claim && e.Actor != actor.Id)
                        ? SpeechIntent.CounterClaim : SpeechIntent.ClaimRole;
                case CouncilActionKind.Report: return action.Readout.HasValue ? SpeechIntent.ReportInvestigation : SpeechIntent.ReportAction;
                case CouncilActionKind.FreeTime: return SpeechIntent.FreeTime;
                default: return SpeechIntent.Chat;
            }
        }
        private DialogueContext Context(CouncilActor actor, CouncilAction action, string result = null)
        {
            string role = action.Claim.HasValue ? _world.Catalog.Single(r => r.Id == action.Claim.Value).Name : "";
            string target = action.Target.HasValue ? _world.Actor(action.Target.Value).Name : "";
            var phase = action.Intent == SpeechIntent.LastWords ? GamePhase.LastWords : GamePhase.Discussion;
            return new DialogueContext(actor.Id, actor.Name, target, role,
                result ?? (action.Readout.HasValue ? action.Readout.Value.ToString().ToUpperInvariant() : action.PatientAttacked.HasValue
                    ? action.PatientAttacked.Value ? "My patient was attacked." : "My patient wasn't attacked." : ""),
                _world.Day, actor.Role.Id, actor.Traits, phase, action.Target);
        }
        internal bool Eligible(CouncilActor actor, CouncilAction action) => _dialogue.CanSelect(action.LineId, Intent(actor, action), Context(actor, action));
        internal IReadOnlyList<DialogueSelection> Preview(CouncilActor actor, CouncilAction action)
        {
            uint key = unchecked(_world.Options.Seed ^ (uint)(_world.Revision * 2246822519u) ^ (uint)((action.Target?.Value ?? -1) + 2) * 3266489917u
                ^ (uint)action.Kind * 16777619u ^ (uint)action.Reason * 2654435761u);
            return _dialogue.Preview(Intent(actor, action), Context(actor, action), new SeededRandom(key));
        }
        internal void Say(CouncilActor actor, CouncilAction action)
        {
            SpeechIntent intent = Intent(actor, action);
            var line = _dialogue.Select(intent, Context(actor, action), action.LineId);
            var kind = action.Kind == CouncilActionKind.Claim ? CouncilEventKind.Claim
                : action.Kind == CouncilActionKind.Report ? CouncilEventKind.Report : CouncilEventKind.Speech;
            _world.Emit(kind, line.Text, actor.Id, action.Target, intent, action.Reason, action.Claim,
                action.Kind == CouncilActionKind.Claim ? null : action.Readout, lineId: line.Id,
                patientAttacked: action.Kind == CouncilActionKind.Claim ? null : action.PatientAttacked);
            if (action.Kind == CouncilActionKind.Claim && (action.Readout.HasValue || action.PatientAttacked.HasValue))
            {
                var report = new CouncilAction(CouncilActionKind.Report, action.Target, ReasonType.Evidence,
                    readout: action.Readout, patientAttacked: action.PatientAttacked);
                Say(actor, report);
            }
        }
    }
}
