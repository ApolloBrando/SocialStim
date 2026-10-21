using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Night;

namespace SocialStim.Rules.Commands
{
    /// <summary>Immutable command; the session validates phase and actor legality.</summary>
    public sealed class GameCommand
    {
        private GameCommand(CommandKind kind, CharacterId? target = null, SpeechIntent intent = SpeechIntent.StaySilent,
            RoleId? role = null, Faction? faction = null, Verdict verdict = Verdict.Abstain, NightActionClaim nightReport = null)
        {
            Kind = kind; Target = target; Intent = intent; ClaimedRole = role;
            ReportedFaction = faction; Verdict = verdict;
            NightReport = nightReport;
        }
        /// <summary>Requested command family.</summary>
        public CommandKind Kind { get; }
        /// <summary>Optional target; null means no target/abstain.</summary>
        public CharacterId? Target { get; }
        /// <summary>Speech intent, when speaking.</summary>
        public SpeechIntent Intent { get; }
        /// <summary>Role being asserted, not necessarily the speaker's role.</summary>
        public RoleId? ClaimedRole { get; }
        /// <summary>Alignment being asserted in a report.</summary>
        public Faction? ReportedFaction { get; }
        /// <summary>Judgement stance.</summary>
        public Verdict Verdict { get; }
        /// <summary>Public attributed account; not a true-role or private-result lookup.</summary>
        public NightActionClaim NightReport { get; }
        /// <summary>Builds an attributed structured night report for session-owned publication.</summary>
        internal static GameCommand PublishReport(NightActionClaim report) => new GameCommand(CommandKind.Speak, report.Target, SpeechIntent.ReportAction, nightReport: report);
        /// <summary>Builds a truthful or deceptive attributed statement.</summary>
        public static GameCommand Speak(SpeechIntent intent, CharacterId? target = null, RoleId? role = null, Faction? faction = null)
            => new GameCommand(CommandKind.Speak, target, intent, role, faction);
        /// <summary>Publicly reports the observer's recorded selection and legitimate results from last night.</summary>
        public static GameCommand ReportLastAction(Faction? claimedResult = null) => new GameCommand(CommandKind.ReportLastAction, faction: claimedResult);
        /// <summary>Publishes a player-authored cover story, subject to role, night and once-per-night validation.</summary>
        public static GameCommand FabricateReport(NightActionClaim report) => new GameCommand(CommandKind.ReportLastAction, nightReport: report);
        /// <summary>Nominates a participant, or abstains.</summary>
        public static GameCommand Nominate(CharacterId? target) => new GameCommand(CommandKind.Nominate, target);
        /// <summary>Casts a daily plurality ballot; null is an abstention when enabled.</summary>
        public static GameCommand Vote(CharacterId? target) => new GameCommand(CommandKind.Vote, target);
        /// <summary>Submits a judgement stance.</summary>
        public static GameCommand Judge(Verdict verdict) => new GameCommand(CommandKind.Judge, verdict: verdict);
        /// <summary>Chooses a night target, or passes.</summary>
        public static GameCommand Night(CharacterId? target) => new GameCommand(CommandKind.ChooseNightTarget, target);
        /// <summary>Chooses one living Town participant for an executed Jester's revenge.</summary>
        public static GameCommand Revenge(CharacterId target) => new GameCommand(CommandKind.Revenge, target);
        /// <summary>Acknowledges a defense, last words, dawn, or spectator turn.</summary>
        public static GameCommand Continue() => new GameCommand(CommandKind.Continue);
    }
}
