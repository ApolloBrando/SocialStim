using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialStim.Rules.Dialogue
{
    /// <summary>Detached editable menu wording loaded from the existing game catalog.</summary>
    public sealed class InteractionLabels
    {
        /// <summary>Fallback wording for portable clients that do not load Unity content.</summary>
        public static InteractionLabels Default { get; } = new InteractionLabels(new[] {
            "Wait", "Accuse", "Defend", "Claim Role", "Counter claim", "Refute", "Agree", "Disagree",
            "Back", "Report last night", "Defend yourself", "PLAYERS"
        });
        private readonly IReadOnlyList<string> _values;
        /// <summary>Loads the twelve catalog labels in their documented authoring order.</summary>
        public InteractionLabels(IEnumerable<string> values)
        {
            var copy = values?.ToList();
            if (copy == null || copy.Count != 12 || copy.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Interaction labels need twelve nonempty values.");
            _values = copy.AsReadOnly();
        }
        /// <summary>Silent pass label.</summary>
        public string Wait => _values[0];
        /// <summary>Accusation target-group label.</summary>
        public string Accuse => _values[1];
        /// <summary>Defense target-group label.</summary>
        public string Defend => _values[2];
        /// <summary>Legal claims group label.</summary>
        public string ClaimRole => _values[3];
        /// <summary>Immediate counterclaim prefix.</summary>
        public string CounterClaim => _values[4];
        /// <summary>Turn an accusation onto its speaker.</summary>
        public string Refute => _values[5];
        /// <summary>Public agreement label.</summary>
        public string Agree => _values[6];
        /// <summary>Public disagreement label.</summary>
        public string Disagree => _values[7];
        /// <summary>Return without submitting a command.</summary>
        public string Back => _values[8];
        /// <summary>Recorded night-report label.</summary>
        public string Report => _values[9];
        /// <summary>Self-defense response label.</summary>
        public string DefendSelf => _values[10];
        /// <summary>Public player-panel heading.</summary>
        public string Players => _values[11];
        /// <summary>Maps stable navigation groups to the authored display labels.</summary>
        public string Group(string key) => key == "Accuse" ? Accuse : key == "Defend" ? Defend : key == "Claim Role" ? ClaimRole : key;
    }
}
