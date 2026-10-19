using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;

namespace SocialStim.Rules.Council
{
    /// <summary>Locks all ballots before publication; plurality and one tied-target revote.</summary>
    internal sealed class CouncilVoteDirector
    {
        private readonly CouncilWorld _world;
        private readonly ICouncilStrategy _strategy;
        internal CouncilVoteDirector(CouncilWorld world, ICouncilStrategy strategy)
        { _world = world; _strategy = strategy; }
        internal IReadOnlyList<CharacterId> Tied { get; private set; } = new List<CharacterId>().AsReadOnly();
        internal bool IsRevote { get; private set; }
        internal void Reset() { Tied = new List<CharacterId>().AsReadOnly(); IsRevote = false; }

        internal void Resolve(CharacterId? playerChoice)
        {
            var ballots = new List<KeyValuePair<CharacterId, CharacterId?>>();
            foreach (var actor in _world.Living)
            {
                var target = actor.Id == _world.Player.Id ? playerChoice : _strategy.Vote(_world.Knowledge(actor, _world.Living.Where(a => a.Id != actor.Id && (!IsRevote || Tied.Contains(a.Id))).Select(a => a.Id).ToList().AsReadOnly()), _world.Decisions);
                bool eligible(CharacterId id) => id != actor.Id && _world.Living.Any(a => a.Id == id)
                    && (!IsRevote || Tied.Contains(id));
                if (target.HasValue && !eligible(target.Value)) target = null;
                if (!target.HasValue && !_world.Options.Abstain)
                    target = _world.Living.Where(a => eligible(a.Id)).OrderBy(a => a.Id.Value).Select(a => (CharacterId?)a.Id).FirstOrDefault();
                ballots.Add(new KeyValuePair<CharacterId, CharacterId?>(actor.Id, target));
            }
            // Public strategy contexts cannot observe earlier votes from this same round.
            foreach (var ballot in ballots)
                _world.Emit(CouncilEventKind.Vote, _world.Actor(ballot.Key).Name + " → "
                    + (ballot.Value.HasValue ? _world.Actor(ballot.Value.Value).Name : "Abstain"), ballot.Key, ballot.Value);
            var tally = ballots.Where(b => b.Value.HasValue).GroupBy(b => b.Value.Value)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Value).ToList();
            _world.Emit(CouncilEventKind.Tally, tally.Count == 0 ? "No votes cast."
                : string.Join(" · ", tally.Select(g => _world.Actor(g.Key).Name + " " + g.Count())));
            var leaders = tally.Count == 0 ? new List<CharacterId>() : tally.Where(g => g.Count() == tally[0].Count()).Select(g => g.Key).ToList();
            if (leaders.Count != 1)
            {
                if (_world.Options.Revote && !IsRevote && leaders.Count > 1)
                {
                    Tied = leaders.AsReadOnly(); IsRevote = true;
                    _world.Emit(CouncilEventKind.Tie, "Tie. One revote."); return;
                }
                _world.Emit(CouncilEventKind.Tie, "No elimination."); _world.Eliminated = null;
            }
            else
            {
                var actor = _world.Actor(leaders[0]); actor.Alive = false; actor.VotedOut = true; _world.Eliminated = actor.Id;
                _world.Emit(CouncilEventKind.Elimination, actor.Name + " was eliminated.", target: actor.Id);
                if (_world.Options.RevealRoles)
                    _world.Emit(CouncilEventKind.RoleReveal, actor.Name + " · " + actor.Role.Name, target: actor.Id, claim: actor.Role.Id);
            }
            _world.Phase = CouncilPhase.Elimination;
        }
    }
}
