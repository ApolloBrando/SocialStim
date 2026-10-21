using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.Sessions
{
    /// <summary>Reversible authoring state. Only selecting a final result submits a game action.</summary>
    internal sealed class ReportComposer
    {
        internal sealed class Choice
        {
            internal string Label;
            internal CharacterId? Target;
            internal Action Navigate;
            internal NightActionClaim Report;
        }
        private readonly PlayerView _view;
        private readonly RoleSpec _role;
        private readonly GameText _text;
        private readonly Action _cancel;
        private readonly HashSet<CharacterId> _visitors = new HashSet<CharacterId>();
        private CharacterId? _target;
        private bool _choosingVisitors;
        internal int Revision => _view.Revision;
        internal ReportComposer(PlayerView view, RoleSpec role, GameText text, Action cancel)
        { _view = view; _role = role; _text = text; _cancel = cancel; }
        internal string Prompt => _text.Format(!_target.HasValue ? "fabricate.pickTarget" : _choosingVisitors ? "fabricate.pickVisitors" : "fabricate.pickResult",
            ("role", _role.Name), ("night", _view.LastNightAction.Night.ToString()),
            ("target", _target.HasValue ? _view.Characters.First(c => c.Id == _target).Name : ""))
            + (!_choosingVisitors ? "" : "\n" + _text.Format("fabricate.selected", ("names", _visitors.Count == 0
                ? _text.Format("fabricate.none") : string.Join(", ", _view.Characters.Where(c => _visitors.Contains(c.Id)).Select(c => c.Name)))));
        internal IEnumerable<Choice> Choices()
        {
            yield return new Choice { Label = _text.Format("fabricate.back"), Navigate = () =>
                { if (_choosingVisitors) { _choosingVisitors = false; _visitors.Clear(); }
                    else if (_target.HasValue) _target = null; else _cancel(); } };
            if (!_target.HasValue)
            {
                yield return Result("fabricate.pass");
                foreach (var participant in _view.Characters.Where(c => c.Id != _view.PlayerId && _view.ReportParticipants.Contains(c.Id)))
                {
                    CharacterId id = participant.Id;
                    yield return new Choice { Label = participant.Name, Target = id, Navigate = () => _target = id };
                }
                yield break;
            }
            if (_choosingVisitors)
            {
                yield return new Choice { Label = _text.Format("fabricate.publishVisitors", ("count", _visitors.Count.ToString())),
                    Report = Account(_visitors.Count == 0 ? new[] { new ReportedNightResult(ReportKind.NoVisitors, _target, subject: _target) }
                        : _visitors.OrderBy(id => id.Value).Select(id => new ReportedNightResult(ReportKind.VisitorSeen, id, subject: _target))) };
                foreach (var visitor in _view.Characters.Where(c => c.Id != _view.PlayerId && _view.ReportParticipants.Contains(c.Id)))
                {
                    CharacterId id = visitor.Id;
                    yield return new Choice { Label = _text.Format(_visitors.Contains(id) ? "fabricate.removeVisitor" : "fabricate.addVisitor", ("name", visitor.Name)),
                        Target = id, Navigate = () => { if (!_visitors.Add(id)) _visitors.Remove(id); } };
                }
                yield break;
            }
            yield return Result("fabricate.blocked", new ReportedNightResult(ReportKind.Blocked));
            switch (_role.Ability)
            {
                case AbilityKind.Investigate:
                    foreach (Faction faction in Enum.GetValues(typeof(Faction)))
                        yield return new Choice { Label = _text.Format("fabricate.alignment", ("result", _text.Format("faction." + faction))),
                            Report = Account(new[] { new ReportedNightResult(ReportKind.Investigation, _target, faction) }) };
                    break;
                case AbilityKind.Protect: yield return Result("fabricate.protected", new ReportedNightResult(ReportKind.ProtectionApplied, _target)); break;
                case AbilityKind.Attack:
                    yield return Result("fabricate.attack", Array.Empty<ReportedNightResult>());
                    yield return Result("fabricate.failed", new ReportedNightResult(ReportKind.AttackFailed, _target)); break;
                case AbilityKind.Watch:
                    yield return Result("fabricate.noVisitors", new ReportedNightResult(ReportKind.NoVisitors, _target, subject: _target));
                    yield return new Choice { Label = _text.Format("fabricate.visitors"), Navigate = () => _choosingVisitors = true }; break;
            }
        }
        private Choice Result(string key, params ReportedNightResult[] results) => new Choice { Label = _text.Format(key), Report = Account(results) };
        private NightActionClaim Account(IEnumerable<ReportedNightResult> results)
            => new NightActionClaim(_view.LastNightAction.Night, _role.Id, _role.Ability, _target, results);
    }
}
