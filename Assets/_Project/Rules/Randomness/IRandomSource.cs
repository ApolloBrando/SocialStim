namespace SocialStim.Rules.Randomness
{
    /// <summary>Supplies deterministic bounded random values and resumable state.</summary>
    public interface IRandomSource
    {
        /// <summary>Current algorithm state; zero is never emitted.</summary>
        uint State { get; }

        /// <summary>Returns a value in [0, exclusiveMaximum).</summary>
        int Next(int exclusiveMaximum);
    }
}
