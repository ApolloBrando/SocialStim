using System;
using System.Linq;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;

namespace SocialStim.Rules.Council
{
    /// <summary>Shared player/NPC payload, target, reason and permission validation.</summary>
    internal static class CouncilActionValidator
    {
        internal static string Error(CouncilWorld world, CouncilActor actor, CouncilAction action, bool phaseSpecific)
        {
            if (action == null || !Enum.IsDefined(typeof(CouncilActionKind), action.Kind)
                || !Enum.IsDefined(typeof(ReasonType), action.Reason)
                || action.Readout.HasValue && !Enum.IsDefined(typeof(Readout), action.Readout.Value)
                || action.Intent.HasValue && !Enum.IsDefined(typeof(SpeechIntent), action.Intent.Value)) return "Invalid action.";
            if (!actor.Alive) return action.Kind == CouncilActionKind.Continue ? null
                : world.Phase == CouncilPhase.Elimination && world.Eliminated == actor.Id
                && world.Options.LastWords && action.Kind == CouncilActionKind.Chat && action.Intent == SpeechIntent.LastWords
                && !action.Target.HasValue && !action.Claim.HasValue && !action.Readout.HasValue && !action.PatientAttacked.HasValue ? null : "You can only watch.";
            if (action.Target.HasValue && !world.Actors.Any(a => a.Id == action.Target.Value)) return "Unknown target.";
            if (phaseSpecific)
            {
                switch (world.Phase)
                {
                    case CouncilPhase.Ended: return "Run complete.";
                    case CouncilPhase.Vote:
                        return action.Kind == CouncilActionKind.Vote
                            && (action.Target.HasValue ? action.Target != actor.Id && world.Actor(action.Target.Value).Alive : world.Options.Abstain)
                            ? null : "Choose a living vote target.";
                    case CouncilPhase.Night:
                        return action.Kind == CouncilActionKind.Night && NightLegality.IsLegal(actor.Night(),
                            new NightOrder(actor.Id, action.Target), world.Living.Select(a => a.Night()).ToList(),
                            new NightPolicy(true, world.Options.SelfHeal, world.Options.Backlash, world.Options.NotifyLull))
                            ? null : "Invalid night target.";
                    case CouncilPhase.Morning: case CouncilPhase.Elimination:
                        return action.Kind == CouncilActionKind.Continue ? null : "Continue to the next stage.";
                    case CouncilPhase.FreeTime:
                        return action.Kind == CouncilActionKind.FreeTime && action.Target.HasValue && action.Target != actor.Id
                            && world.Actor(action.Target.Value).Alive || action.Kind == CouncilActionKind.Pass ? null : "Choose someone to visit.";
                }
            }
            if (action.Kind == CouncilActionKind.Pass) return action.Target.HasValue || action.Claim.HasValue
                || action.Readout.HasValue || action.PatientAttacked.HasValue ? "Invalid pass." : null;
            if (action.Intent.HasValue && action.Kind == CouncilActionKind.Chat)
            {
                SpeechIntent intent = action.Intent.Value;
                if (intent == SpeechIntent.ClaimRole || intent == SpeechIntent.CounterClaim || intent == SpeechIntent.ReportAction
                    || intent == SpeechIntent.ReportInvestigation || intent == SpeechIntent.LastWords
                    || intent == SpeechIntent.Accuse || intent == SpeechIntent.Defend || intent == SpeechIntent.Vouch)
                    return "Intent does not match chat.";
                if (SpeechContract.RequiresTarget(intent) && (!action.Target.HasValue || action.Target == actor.Id))
                    return "Choose someone else.";
                if (intent == SpeechIntent.ReactToDeath && !world.Events.Any(e => e.Public && e.Kind == CouncilEventKind.Death))
                    return "No death has been reported.";
                if (intent == SpeechIntent.ReactToRoleReveal && !world.Events.Any(e => e.Kind == CouncilEventKind.RoleReveal))
                    return "No role has been revealed.";
            }
            else if (action.Intent.HasValue) return "Intent override is only available for chat.";
            if (action.Kind != CouncilActionKind.Chat && action.Kind != CouncilActionKind.Accuse
                && action.Kind != CouncilActionKind.Defend && action.Kind != CouncilActionKind.Claim && action.Kind != CouncilActionKind.Report)
                return "It is discussion time.";
            if (action.Kind == CouncilActionKind.Accuse && (!action.Target.HasValue || action.Target == actor.Id)) return "Choose someone else.";
            if (action.Kind == CouncilActionKind.Defend && !action.Target.HasValue) return "Choose who to defend.";
            if (action.Target.HasValue && action.Kind != CouncilActionKind.Report && action.Kind != CouncilActionKind.Claim
                && !world.Actor(action.Target.Value).Alive) return "Choose a living participant.";
            if (action.Intent == SpeechIntent.Question && (!action.Target.HasValue || action.Target == actor.Id)) return "Choose someone to question.";
            if (action.DeclaredLie && !actor.Role.CanBluff) return "This role cannot bluff.";
            if ((action.Kind == CouncilActionKind.Accuse || action.Kind == CouncilActionKind.Defend)
                && action.Target.HasValue && !action.DeclaredLie && !CouncilReasoning.HasReason(world.Knowledge(actor), action.Target.Value, action.Reason))
                return "That reason has no supporting evidence.";
            if (action.Kind == CouncilActionKind.Claim)
            {
                var role = action.Claim.HasValue ? world.Catalog.FirstOrDefault(r => r.Id == action.Claim.Value) : null;
                if (!ClaimPermissions.CanClaim(actor.Role, role)) return "This role cannot make that claim.";
            }
            else if (action.Claim.HasValue) return "Claim payload does not match the action.";
            if (action.Readout.HasValue || action.PatientAttacked.HasValue)
            {
                if (action.Kind != CouncilActionKind.Claim && action.Kind != CouncilActionKind.Report
                    || !action.Target.HasValue || action.Readout.HasValue && action.PatientAttacked.HasValue) return "Invalid report.";
                bool real = actor.Results.Any(r => r.Night == world.Day - 1 && r.Target == action.Target
                    && (action.Readout.HasValue ? r.Readout == action.Readout
                        : action.PatientAttacked.Value ? r.Kind == ReportKind.PatientAttacked : r.Kind == ReportKind.PatientSafe));
                if (!ClaimPermissions.CanReport(actor.Role, real)) return "Choose a result you actually received.";
                if (world.Day <= 1) return "There is no previous night.";
            }
            else if (action.Kind == CouncilActionKind.Report) return "Choose a result to report.";
            return null;
        }
    }
}
