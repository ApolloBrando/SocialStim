using System;
using System.Linq;
using SocialStim.Rules.Randomness;

namespace SocialStim.Rules.Council
{
    /// <summary>Six immutable run-local skills from one to ten.</summary>
    public sealed class CharacterStats
    {
        private readonly int[] _values;
        /// <summary>Creates a complete, bounded skill profile.</summary>
        public CharacterStats(int charisma = 5, int intuition = 5, int logic = 5, int charm = 5, int performance = 5, int stealth = 5)
        {
            _values = new[] { charisma, intuition, logic, charm, performance, stealth };
            if (_values.Any(v => v < 1 || v > 10)) throw new ArgumentOutOfRangeException(nameof(charisma));
        }
        /// <summary>Reads one skill.</summary>
        public int Get(StatKind kind) => _values[(int)kind];
        /// <summary>Point-buy total.</summary>
        public int Total => _values.Sum();
        /// <summary>Generates an equal-budget randomized profile, so luck changes strengths rather than total power.</summary>
        public static CharacterStats Generate(IRandomSource random)
        {
            var values = Enumerable.Repeat(1, 6).ToArray();
            for (int points = 0; points < 24; points++)
            {
                var eligible = Enumerable.Range(0, 6).Where(i => values[i] < 10).ToList();
                values[eligible[random.Next(eligible.Count)]]++;
            }
            return new CharacterStats(values[0], values[1], values[2], values[3], values[4], values[5]);
        }
    }
}
