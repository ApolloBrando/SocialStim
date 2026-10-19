using System;

namespace SocialStim.Rules.Randomness
{
    /// <summary>Version 1 xorshift32 stream, independent of Unity and framework RNGs.</summary>
    public sealed class SeededRandom : IRandomSource
    {
        private uint _state;

        /// <summary>Creates a stream from a seed or previously captured state.</summary>
        public SeededRandom(uint state)
        {
            _state = state == 0 ? 0x6D2B79F5u : state;
        }

        internal void Restore(uint state)
        {
            if (state == 0) throw new ArgumentException("A random stream state cannot be zero.");
            _state = state;
        }

        /// <inheritdoc />
        public uint State => _state;

        /// <inheritdoc />
        public int Next(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            }

            // Rejection removes modulo bias; all arithmetic is explicitly wrapping.
            uint bound = (uint)exclusiveMaximum;
            uint value;
            do
            {
                unchecked
                {
                    _state ^= _state << 13;
                    _state ^= _state >> 17;
                    _state ^= _state << 5;
                }
                value = _state - 1u;
            }
            while (value >= uint.MaxValue - uint.MaxValue % bound);

            // Xorshift's nonzero states form a domain of 2^32 - 1 values.
            return (int)(value % bound);
        }
    }
}
