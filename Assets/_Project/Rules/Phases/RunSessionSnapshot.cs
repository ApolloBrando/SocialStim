using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Persistence;
using SocialStim.Rules.Randomness;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;
using SocialStim.Rules.Victory;

namespace SocialStim.Rules.Phases
{
    /// <summary>Explicit versioned complete state, detached from scene objects and delegates.</summary>
    public sealed partial class RunSession
    {
        private const int SaveMagic = 0x5353544D;
        private const int SaveVersion = 1;

        /// <summary>Captures an input-boundary snapshot, including all random streams and observer state.</summary>
        public byte[] Save()
        {
            if (!(_strategy is SocialNpcStrategy) && !(_strategy is BasicNpcStrategy))
                throw new NotSupportedException("Saving custom NPC strategies requires an explicit persistence implementation.");
            if (!(_winCondition is StarterWinCondition) && !(_winCondition is CapabilityWinCondition))
                throw new NotSupportedException("Saving custom outcome policies requires an explicit persistence implementation.");
            using (var payload = new MemoryStream())
            {
                using (var w = new BinaryWriter(payload, Encoding.UTF8, true)) WriteState(w);
                if (payload.Length > SnapshotIO.MaximumBytes) throw new InvalidDataException("Snapshot exceeds the size limit.");
                byte[] bytes = payload.ToArray();
                using (var output = new MemoryStream())
                using (var w = new BinaryWriter(output))
                {
                    w.Write(SaveMagic); w.Write(SaveVersion); SnapshotIO.Text(w, ContentHash());
                    using (var sha = SHA256.Create()) w.Write(sha.ComputeHash(bytes));
                    w.Write(bytes.Length); w.Write(bytes);
                    return output.ToArray();
                }
            }
        }

        /// <summary>Builds a new isolated session; failed validation never mutates an existing run.</summary>
        public static RunSession Load(byte[] snapshot, IEnumerable<RoleSpec> roles, IEnumerable<DialogueLine> dialogue,
            ReasoningSettings reasoning = null)
        {
            if (snapshot == null || snapshot.Length > SnapshotIO.MaximumBytes + 1024 || snapshot.Length < 48)
                throw new InvalidDataException("Missing, oversized, or truncated snapshot.");
            try
            {
                using (var input = new MemoryStream(snapshot, false))
                using (var reader = new BinaryReader(input))
                {
                    if (reader.ReadInt32() != SaveMagic || reader.ReadInt32() != SaveVersion)
                        throw new InvalidDataException("Unsupported save format or schema version.");
                    string contentHash = SnapshotIO.Text(reader, 128);
                    byte[] checksum = reader.ReadBytes(32);
                    int length = SnapshotIO.Int(reader, 0, SnapshotIO.MaximumBytes);
                    if (length != input.Length - input.Position) throw new InvalidDataException("Truncated or trailing snapshot data.");
                    byte[] payload = reader.ReadBytes(length);
                    using (var sha = SHA256.Create())
                        if (!sha.ComputeHash(payload).SequenceEqual(checksum)) throw new InvalidDataException("Snapshot checksum failed.");
                    using (var stream = new MemoryStream(payload, false))
                    using (var r = new BinaryReader(stream))
                    {
                        RunSession session = ReadState(r, roles, dialogue, reasoning ?? new ReasoningSettings());
                        if (stream.Position != stream.Length) throw new InvalidDataException("Unexpected trailing state.");
                        if (!string.Equals(contentHash, session.ContentHash(), StringComparison.Ordinal))
                            throw new InvalidDataException("Save content/rules version does not match this catalog, dialogue, or reasoning.");
                        session.ValidateLoadedState();
                        return session;
                    }
                }
            }
            catch (InvalidDataException) { throw; }
            catch (Exception e) when (e is EndOfStreamException || e is ArgumentException || e is OverflowException || e is InvalidOperationException)
            { throw new InvalidDataException("Invalid snapshot state.", e); }
        }

        private string ContentHash()
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                SnapshotIO.Text(w, "SocialStim rules 03.1 / snapshot 1");
                w.Write(_catalog.Count);
                foreach (RoleSpec role in _catalog.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
                {
                    SnapshotIO.Text(w, role.Id.Value); SnapshotIO.Text(w, role.Name); w.Write((int)role.Faction);
                    w.Write((int)role.Category); w.Write((int)role.Ability); w.Write(role.Attack); w.Write(role.Defense);
                    w.Write(role.Protection); w.Write(role.Charges); w.Write((int)role.Appearance); w.Write(role.Unique);
                }
                SnapshotRecords.Settings(w, _reasoning); _dialogue.WriteContent(w); w.Flush();
                using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        }

        private void WriteState(BinaryWriter w)
        {
            w.Write(_options.Seed); SnapshotRecords.Role(w, _options.PlayerRole);
            w.Write(_options.TrialsPerDay); w.Write(_options.DiscussionRounds); w.Write(_options.RevealOnDeath); w.Write(_options.MaximumDays);
            w.Write(_strategy is SocialNpcStrategy ? 0 : 1);
            if (_strategy is SocialNpcStrategy social) SnapshotRecords.Settings(w, social.Settings);
            w.Write(_winCondition is CapabilityWinCondition ? 1 : 0);
            w.Write(_cast.Count);
            foreach (Participant actor in _cast)
            {
                SnapshotIO.Text(w, actor.Name); SnapshotRecords.Role(w, actor.Role.Id);
                w.Write((int)actor.Personality); w.Write(actor.Alive); w.Write(actor.Charges);
            }
            w.Write((int)_phases.Phase); w.Write(_phases.Day); w.Write(_discussionRounds); w.Write(_trials); w.Write(_revision);
            SnapshotRecords.Id(w, _defendant); w.Write((int)_outcome); w.Write(_opening); w.Write(_decisions.State);
            w.Write(_lastTeamPlan != null);
            if (_lastTeamPlan != null) { w.Write(_lastTeamPlan.Killer.Value); SnapshotRecords.Id(w, _lastTeamPlan.Target); }
            w.Write(_executionObjectives.Count); foreach (var id in _executionObjectives.OrderBy(x => x.Value)) w.Write(id.Value);
            w.Write(_lastActions.Count);
            foreach (var pair in _lastActions.OrderBy(p => p.Key.Value)) { w.Write(pair.Key.Value); SnapshotRecords.Id(w, pair.Value.Target); }
            w.Write(_revealed.Count);
            foreach (var pair in _revealed.OrderBy(p => p.Key.Value)) { w.Write(pair.Key.Value); SnapshotRecords.Role(w, pair.Value.Id); }
            foreach (Participant actor in _cast)
            {
                w.Write(_results[actor.Id].Count); foreach (NightReport report in _results[actor.Id]) SnapshotRecords.Report(w, report);
                _beliefs[actor.Id].WriteState(w);
            }
            _dialogue.WriteState(w);
            w.Write(_transcript.Count);
            foreach (GameObservation o in _transcript)
            {
                w.Write(o.Sequence); w.Write(o.Day); w.Write((int)o.Cue); SnapshotIO.Text(w, o.Text); SnapshotRecords.Id(w, o.Speaker);
                w.Write(o.Metadata != null);
                if (o.Metadata != null) { SnapshotIO.Text(w, o.Metadata.Emotion); SnapshotIO.Text(w, o.Metadata.Portrait); SnapshotIO.Text(w, o.Metadata.Audio); }
                SnapshotRecords.Id(w, o.Target); SnapshotIO.OptionalEnum(w, o.Intent); SnapshotRecords.Role(w, o.ClaimedRole);
            }
        }

        private static RunSession ReadState(BinaryReader r, IEnumerable<RoleSpec> roles, IEnumerable<DialogueLine> dialogue, ReasoningSettings reasoning)
        {
            var catalog = roles.OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToList().AsReadOnly();
            uint seed = r.ReadUInt32(); RoleId? chosen = SnapshotRecords.Role(r, catalog);
            var options = new RunOptions(seed, chosen, SnapshotIO.Int(r, 1, 10), SnapshotIO.Int(r, 1, 10),
                SnapshotIO.Bool(r), SnapshotIO.Int(r, 1, int.MaxValue));
            int strategyKind = SnapshotIO.Int(r, 0, 1);
            INpcStrategy strategy = strategyKind == 0 ? (INpcStrategy)new SocialNpcStrategy(SnapshotRecords.Settings(r)) : new BasicNpcStrategy();
            IWinCondition outcomePolicy = SnapshotIO.Int(r, 0, 1) == 0 ? (IWinCondition)new StarterWinCondition() : new CapabilityWinCondition();
            int count = SnapshotIO.Int(r, 3, 20);
            var members = new List<CastMemberSpec>(); var alive = new List<bool>(); var charges = new List<int>();
            for (int i = 0; i < count; i++)
            {
                string name = SnapshotIO.Text(r, 256);
                RoleId? role = SnapshotRecords.Role(r, catalog);
                if (!role.HasValue) throw new InvalidDataException("Missing cast role.");
                var personality = (Personality)SnapshotIO.Int(r, 0, 31);
                members.Add(new CastMemberSpec(name, role.Value, personality));
                alive.Add(SnapshotIO.Bool(r));
                RoleSpec definition = catalog.Single(x => x.Id == role.Value);
                int resource = SnapshotIO.Int(r, -1, Math.Max(0, definition.Charges));
                if ((definition.Charges == -1) != (resource == -1)) throw new InvalidDataException("Invalid charge policy.");
                charges.Add(resource);
            }
            RunSession session = RunFactory.CreateScenario(options, members, catalog, dialogue, strategy, outcomePolicy, reasoning);
            for (int i = 0; i < count; i++) { session._cast[i].Alive = alive[i]; session._cast[i].Charges = charges[i]; }
            var phase = SnapshotIO.EnumValue<GamePhase>(r); int day = SnapshotIO.Int(r, 1, int.MaxValue - 1);
            session._phases.Restore(phase, day);
            session._discussionRounds = SnapshotIO.Int(r, 0, options.DiscussionRounds);
            session._trials = SnapshotIO.Int(r, 0, options.TrialsPerDay); session._revision = SnapshotIO.Int(r, 0, int.MaxValue - 1);
            session._defendant = SnapshotRecords.Id(r, count); session._outcome = SnapshotIO.EnumValue<OutcomeKind>(r);
            session._opening = SnapshotIO.Bool(r); ((SeededRandom)session._decisions).Restore(r.ReadUInt32());
            if (SnapshotIO.Bool(r)) session._lastTeamPlan = new TeamNightPlan(SnapshotRecords.RequiredId(r, count), SnapshotRecords.Id(r, count));
            int length = SnapshotIO.Int(r, 0, count);
            for (int i = 0; i < length; i++)
                if (!session._executionObjectives.Add(SnapshotRecords.RequiredId(r, count))) throw new InvalidDataException("Duplicate objective.");
            length = SnapshotIO.Int(r, 0, count);
            for (int i = 0; i < length; i++)
            {
                CharacterId id = SnapshotRecords.RequiredId(r, count);
                if (session._lastActions.ContainsKey(id)) throw new InvalidDataException("Duplicate night action.");
                session._lastActions.Add(id, new NightOrder(id, SnapshotRecords.Id(r, count)));
            }
            length = SnapshotIO.Int(r, 0, count);
            for (int i = 0; i < length; i++)
            {
                CharacterId id = SnapshotRecords.RequiredId(r, count); RoleId? role = SnapshotRecords.Role(r, catalog);
                if (!role.HasValue || session._revealed.ContainsKey(id)) throw new InvalidDataException("Invalid revealed role.");
                session._revealed.Add(id, catalog.Single(x => x.Id == role.Value));
            }
            foreach (Participant actor in session._cast)
            {
                length = SnapshotIO.Int(r, 0, 200000);
                for (int i = 0; i < length; i++) session._results[actor.Id].Add(SnapshotRecords.Report(r, count, day, actor.Id));
                session._beliefs[actor.Id].ReadState(r, count, day, catalog);
            }
            session._dialogue.ReadState(r, count);
            session._transcript.Clear(); length = SnapshotIO.Int(r, 0, 200000);
            for (int i = 0; i < length; i++)
            {
                if (SnapshotIO.Int(r, 0, 200000) != i) throw new InvalidDataException("Nonsequential transcript.");
                int observedDay = SnapshotIO.Int(r, 1, day); var cue = SnapshotIO.EnumValue<GameCue>(r);
                string text = SnapshotIO.Text(r); CharacterId? speaker = SnapshotRecords.Id(r, count);
                DialogueMetadata metadata = SnapshotIO.Bool(r) ? new DialogueMetadata(SnapshotIO.Text(r), SnapshotIO.Text(r), SnapshotIO.Text(r)) : null;
                CharacterId? target = SnapshotRecords.Id(r, count); var intent = SnapshotIO.OptionalEnum<SpeechIntent>(r);
                RoleId? claim = SnapshotRecords.Role(r, catalog);
                session._transcript.Add(new GameObservation(i, observedDay, cue, text, speaker, metadata, target, intent, claim));
            }
            return session;
        }

        private void ValidateLoadedState()
        {
            bool trial = _phases.Phase == GamePhase.Defense || _phases.Phase == GamePhase.Judgement || _phases.Phase == GamePhase.LastWords;
            if (trial && (!_defendant.HasValue || !Actor(_defendant.Value).Alive || _trials == 0))
                throw new InvalidDataException("Trial snapshot has no eligible defendant.");
            if ((_phases.Phase == GamePhase.Ended) != (_outcome != OutcomeKind.Ongoing))
                throw new InvalidDataException("Terminal phase/outcome mismatch.");
            if (_opening && _phases.Phase != GamePhase.Discussion) throw new InvalidDataException("Opening outside discussion.");
            if (_revealed.Any(p => Actor(p.Key).Alive || Actor(p.Key).Role.Id != p.Value.Id || !_options.RevealOnDeath))
                throw new InvalidDataException("Invalid role disclosure.");
            if (_executionObjectives.Any(id => Actor(id).Alive || Actor(id).Role.Category != RoleCategory.NeutralEvil))
                throw new InvalidDataException("Invalid execution objective.");
            if (_lastTeamPlan != null && (Actor(_lastTeamPlan.Killer).Role.Faction != Faction.Antagonist
                || Actor(_lastTeamPlan.Killer).Role.Ability != AbilityKind.Attack))
                throw new InvalidDataException("Invalid team plan actor.");
            if (_winCondition is StarterWinCondition && _cast.Any(a => a.Role.Ability == AbilityKind.Attack && a.Role.Faction != Faction.Antagonist))
                throw new InvalidDataException("Unsupported starter outcome policy.");
            foreach (Participant actor in _cast) _ = Knowledge(actor);
        }
    }
}
