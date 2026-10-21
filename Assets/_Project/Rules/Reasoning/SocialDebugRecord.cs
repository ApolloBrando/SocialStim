using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Reasoning
{
    /// <summary>Detached privileged opinion edge; never public evidence or a strategy input.</summary>
    public sealed class SocialDebugRecord
    {
        internal SocialDebugRecord(CharacterId observer, string observerName, bool observerAlive,
            CharacterId subject, string subjectName, bool subjectAlive, int suspicion, int trust, FactionEstimate estimate)
        { Observer = observer; ObserverName = observerName; ObserverAlive = observerAlive;
            Subject = subject; SubjectName = subjectName; SubjectAlive = subjectAlive;
            Suspicion = suspicion; Trust = trust; Estimate = estimate; }
        /// <summary>NPC whose opinion is shown.</summary>
        public CharacterId Observer { get; }
        /// <summary>Public observer name.</summary>
        public string ObserverName { get; }
        /// <summary>Whether this observer is alive.</summary>
        public bool ObserverAlive { get; }
        /// <summary>Participant being assessed, including the player.</summary>
        public CharacterId Subject { get; }
        /// <summary>Public subject name.</summary>
        public string SubjectName { get; }
        /// <summary>Whether this subject is alive.</summary>
        public bool SubjectAlive { get; }
        /// <summary>Observer-local suspicion from zero to one hundred.</summary>
        public int Suspicion { get; }
        /// <summary>Observer-local credibility from zero to one hundred.</summary>
        public int Trust { get; }
        /// <summary>Normalized heuristic alignment estimates, never true role assignments.</summary>
        public FactionEstimate Estimate { get; }
    }
}
