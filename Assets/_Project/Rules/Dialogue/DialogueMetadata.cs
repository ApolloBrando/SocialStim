namespace SocialStim.Rules.Dialogue
{
    /// <summary>Optional authored presentation hints ignored by plain-text rendering.</summary>
    public sealed class DialogueMetadata
    {
        /// <summary>Creates metadata without rendering dependencies.</summary>
        public DialogueMetadata(string emotion = "", string portrait = "", string audio = "")
        { Emotion = emotion ?? ""; Portrait = portrait ?? ""; Audio = audio ?? ""; }
        /// <summary>Expression/emotion content key.</summary>
        public string Emotion { get; }
        /// <summary>Future portrait content key.</summary>
        public string Portrait { get; }
        /// <summary>Future audio content key.</summary>
        public string Audio { get; }
    }
}
