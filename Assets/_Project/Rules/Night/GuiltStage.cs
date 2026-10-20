namespace SocialStim.Rules.Night
{
    /// <summary>Scheduled guilt resolves after actions; role blocks, protection and innate defense cannot prevent it.</summary>
    public sealed class GuiltStage : INightStage
    {
        public int Priority => 60;
        public void Resolve(NightContext context)
        {
            foreach (NightActor actor in context.Actors)
                if (actor.GuiltNight > 0 && actor.GuiltNight <= context.Night) context.Deaths.Add(actor.Id);
        }
    }
}
