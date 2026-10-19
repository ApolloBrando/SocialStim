using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;

namespace SocialStim.Rules.Council
{
    /// <summary>Bounded round exchanges and priority interruptions, independent of rendering time.</summary>
    internal sealed class CouncilDayDirector
    {
        private readonly CouncilWorld _world;
        private readonly ICouncilStrategy _strategy;
        private readonly CouncilSpeechService _speech;
        private readonly Queue<CharacterId> _pending = new Queue<CharacterId>();
        internal CouncilDayDirector(CouncilWorld world, ICouncilStrategy strategy, CouncilSpeechService speech)
        { _world = world; _strategy = strategy; _speech = speech; }
        internal int WindowsUsed { get; private set; }
        internal bool Respond { get; private set; }
        internal CharacterId? RespondTo { get; private set; }
        private int _followups;
        internal void Begin()
        {
            _world.Phase = CouncilPhase.Discussion; _world.Round = 1; WindowsUsed = 0; Respond = false; RespondTo = null; _followups = 0; _pending.Clear();
            QueueSpeakers(1); RunPending();
        }
        internal void PlayerSpoke(bool wasResponse, CouncilAction action)
        {
            Respond = false; RespondTo = null;
            if (wasResponse && action.Kind == CouncilActionKind.Accuse && action.Target.HasValue && _followups++ < 2)
                _pending.Enqueue(action.Target.Value);
            if (!wasResponse)
            {
                WindowsUsed++;
                QueueSpeakers(System.Math.Min(4, 2 + _world.Living.Count() / 5));
            }
            RunPending();
        }
        private void QueueSpeakers(int count)
        {
            var available = _world.Living.Where(a => a.Id != _world.Player.Id).Select(a => a.Id).ToList();
            var last = _world.Events.LastOrDefault(e => e.Intent.HasValue);
            if (last != null && last.Target.HasValue && last.Target != _world.Player.Id && available.Contains(last.Target.Value))
            { _pending.Enqueue(last.Target.Value); available.Remove(last.Target.Value); count--; }
            while (count-- > 0 && available.Count > 0)
            {
                int index = _world.Decisions.Next(available.Count);
                _pending.Enqueue(available[index]); available.RemoveAt(index);
            }
        }
        private void RunPending()
        {
            while (true)
            {
                while (_pending.Count > 0)
                {
                    var actor = _world.Actor(_pending.Dequeue());
                    if (!actor.Alive) continue;
                    var action = _strategy.Speak(_world.Knowledge(actor), _world.Decisions);
                    if (CouncilActionValidator.Error(_world, actor, action, false) != null)
                        action = new CouncilAction(CouncilActionKind.Chat, intent: SpeechIntent.Banter);
                    if (action.Kind != CouncilActionKind.Pass) _speech.Say(actor, action);
                    bool directlyAddressed = action.Target == _world.Player.Id
                        && (action.Kind == CouncilActionKind.Accuse || action.Intent == SpeechIntent.Question);
                    bool counterClaim = action.Kind == CouncilActionKind.Claim && action.Claim.HasValue
                        && _world.Catalog.Any(r => r.Id == action.Claim.Value && r.Unique)
                        && _world.Claim(_world.Player.Id) == action.Claim;
                    if (_world.Player.Alive && (directlyAddressed || counterClaim)) { Respond = true; RespondTo = actor.Id; return; }
                }
                if (WindowsUsed < _world.Options.PlayerWindows) return;
                if (_world.Round >= _world.Options.Rounds)
                { _world.Phase = CouncilPhase.Vote; _world.Emit(CouncilEventKind.Phase, "Cast your vote."); return; }
                _world.Round++; WindowsUsed = 0; _followups = 0; QueueSpeakers(1);
            }
        }
    }
}
