namespace SocialStim.Rules.Commands
{
    /// <summary>Participant command families understood by the phase state machine.</summary>
    public enum CommandKind { Speak, Nominate, Judge, ChooseNightTarget, Continue, ReportLastAction, Vote }
}
