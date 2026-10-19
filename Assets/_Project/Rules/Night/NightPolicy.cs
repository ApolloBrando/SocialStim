namespace SocialStim.Rules.Night
{
    /// <summary>Immutable mechanics options; legacy mode is retained for the optional trial rules.</summary>
    public sealed class NightPolicy
    {
        /// <summary>Creates a night policy independent of presentation and role identity.</summary>
        public NightPolicy(bool mission = false, bool selfHeal = true, bool backlash = true, bool notifyLull = true)
        { Mission = mission; SelfHeal = selfHeal; Backlash = backlash; NotifyLull = notifyLull; }
        public bool Mission { get; }
        public bool SelfHeal { get; }
        public bool Backlash { get; }
        public bool NotifyLull { get; }
    }
}
