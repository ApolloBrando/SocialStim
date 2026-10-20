using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Council
{
    /// <summary>Headless all-AI runner through the same validated public player port.</summary>
    public static class CouncilSimulation
    {
        /// <summary>Runs independent seeds without UI clocks or hidden-role access during decisions.</summary>
        public static CouncilSimulationMetrics Run(CouncilOptions options, SetupConfiguration setup, IReadOnlyList<RoleSpec> roles,
            IReadOnlyList<string> names, IReadOnlyList<DialogueLine> dialogue, int games, uint firstSeed = 1, int stepLimit = 1000)
        {
            if (games < 1 || stepLimit < 1) throw new ArgumentOutOfRangeException(nameof(games));
            var metrics = new CouncilSimulationMetrics();
            var strategy = new CouncilNpcStrategy();
            var clock = Stopwatch.StartNew();
            for (int game = 0; game < games; game++)
            {
                uint seed = unchecked(firstSeed + (uint)game); metrics.Games++;
                try
                {
                    var run = CouncilFactory.Create(options.WithSeed(seed), setup, roles, names, dialogue, strategy);
                    var random = new SeededRandom(seed ^ 0x68E31DA4u);
                    CouncilSnapshot view = run.View();
                    int steps = 0;
                    while (view.Phase != CouncilPhase.Ended && steps++ < stepLimit)
                    {
                        CouncilAction action = Decision(view, strategy, random);
                        if (!run.TrySubmit(action, out string error))
                        {
                            var fallback = view.Phase == CouncilPhase.Discussion
                                ? new CouncilAction(CouncilActionKind.Pass) : view.Phase == CouncilPhase.Night
                                ? new CouncilAction(CouncilActionKind.Night) : new CouncilAction(CouncilActionKind.Continue);
                            if (!run.TrySubmit(fallback, out error)) throw new InvalidOperationException(error);
                        }
                        metrics.StepTotal++; view = run.View();
                    }
                    if (view.Phase != CouncilPhase.Ended) { metrics.Fail(seed, "Step limit."); continue; }
                    metrics.Complete(view.Outcome); metrics.DayTotal += view.Day;
                    bool infinitum = view.FinalRoles.Values.Any(id => roles.Single(r => r.Id == id).Objective == RoleObjective.StealParity);
                    if (infinitum) metrics.InfinitumRuns++;
                    foreach (var entry in view.FinalRoles)
                    {
                        var role = roles.Single(r => r.Id == entry.Value);
                        bool alive = view.Knowledge.Characters.Single(c => c.Id == entry.Key).Alive;
                        bool votedOut = view.AfterAction.Any(e => e.Kind == CouncilEventKind.Elimination && e.Target == entry.Key);
                        metrics.Role(role.Id, new MissionWinCondition().Won(role, alive, votedOut, view.Outcome));
                    }
                    foreach (var vote in view.AfterAction.Where(e => e.Kind == CouncilEventKind.Vote && e.Actor.HasValue && e.Target.HasValue))
                    {
                        var voter = roles.Single(r => r.Id == view.FinalRoles[vote.Actor.Value]);
                        var target = roles.Single(r => r.Id == view.FinalRoles[vote.Target.Value]);
                        if (voter.Faction != Faction.Town) continue;
                        metrics.TownVotes++;
                        if (target.Faction == Faction.Antagonist || target.Objective == RoleObjective.LastSurvivor) metrics.CorrectVotes++;
                    }
                }
                catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
                { metrics.Fail(seed, exception.GetType().Name + " " + exception.Message); }
            }
            clock.Stop(); metrics.Milliseconds = clock.Elapsed.TotalMilliseconds;
            return metrics;
        }
        private static CouncilAction Decision(CouncilSnapshot view, ICouncilStrategy strategy, IRandomSource random)
        {
            var k = view.Knowledge;
            if (!k.Characters.Single(c => c.Id == k.Self).Alive) return new CouncilAction(CouncilActionKind.Continue);
            switch (view.Phase)
            {
                case CouncilPhase.Discussion:
                    if (view.Respond) return new CouncilAction(CouncilActionKind.Defend, k.Self, ReasonType.Logic);
                    return strategy.Speak(k, random);
                case CouncilPhase.Vote:
                    var scoped = new CouncilKnowledge(k.Self, k.OwnRole, k.Traits, k.Day, k.Round, k.Charges, k.SelfHealUsed,
                        k.Characters, k.Events, k.Results, k.Catalog, k.Counts, k.RoleCounts, view.VoteTargets, k.Stats, k.Policy, k.Seed, k.Inference, k.SelfHealAllowed);
                    var vote = strategy.Vote(scoped, random);
                    if (!vote.HasValue && !view.CanAbstain) vote = view.VoteTargets.Select(id => (CharacterId?)id).FirstOrDefault();
                    return new CouncilAction(CouncilActionKind.Vote, vote);
                case CouncilPhase.Night:
                    var target = strategy.NightTarget(k, random);
                    return new CouncilAction(CouncilActionKind.Night, target.HasValue && view.NightTargets.Contains(target.Value) ? target : null);
                default: return new CouncilAction(CouncilActionKind.Continue);
            }
        }
    }
}
