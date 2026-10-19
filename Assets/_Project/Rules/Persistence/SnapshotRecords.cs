using System.Collections.Generic;
using System.IO;
using System.Linq;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Commands;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Night;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Rules.Persistence
{
    internal static class SnapshotRecords
    {
        internal static void Id(BinaryWriter w, CharacterId? id) => w.Write(id?.Value ?? -1);
        internal static CharacterId? Id(BinaryReader r, int count) { int id = SnapshotIO.Int(r, -1, count - 1); return id < 0 ? (CharacterId?)null : new CharacterId(id); }
        internal static CharacterId RequiredId(BinaryReader r, int count) => new CharacterId(SnapshotIO.Int(r, 0, count - 1));
        internal static void Role(BinaryWriter w, RoleId? role) => SnapshotIO.Text(w, role?.Value ?? "");
        internal static RoleId? Role(BinaryReader r, IReadOnlyList<RoleSpec> catalog)
        {
            string id = SnapshotIO.Text(r, 256);
            if (id.Length == 0) return null;
            RoleSpec role = catalog.FirstOrDefault(x => x.Id.Value == id);
            if (role == null) throw new InvalidDataException("Snapshot references missing role content.");
            return role.Id;
        }
        internal static void Report(BinaryWriter w, NightReport report)
        {
            w.Write(report.Recipient.Value); w.Write((int)report.Kind); Id(w, report.Target);
            SnapshotIO.OptionalEnum(w, report.Alignment); w.Write(report.Night); Id(w, report.Subject);
        }
        internal static NightReport Report(BinaryReader r, int count, int day, CharacterId recipient)
        {
            CharacterId owner = RequiredId(r, count);
            var kind = SnapshotIO.EnumValue<ReportKind>(r); CharacterId? target = Id(r, count);
            var alignment = SnapshotIO.OptionalEnum<Faction>(r); int night = SnapshotIO.Int(r, 1, day);
            CharacterId? subject = Id(r, count);
            if (owner != recipient || (kind == ReportKind.Investigation && (!target.HasValue || !alignment.HasValue))
                || (kind == ReportKind.VisitorSeen && (!target.HasValue || !subject.HasValue))
                || ((kind == ReportKind.NoVisitors || kind == ReportKind.ProtectionApplied || kind == ReportKind.ForgeryApplied || kind == ReportKind.AttackFailed) && !target.HasValue)
                || (kind != ReportKind.Investigation && alignment.HasValue))
                throw new InvalidDataException("Invalid scoped night report.");
            return new NightReport(owner, kind, target, alignment, night, subject);
        }
        internal static void Evidence(BinaryWriter w, EvidenceRecord e)
        {
            w.Write(e.Sequence); w.Write(e.Day); w.Write((int)e.Kind); w.Write(e.Actor.Value); Id(w, e.Target);
            SnapshotIO.OptionalEnum(w, e.Intent); Role(w, e.Role); SnapshotIO.OptionalEnum(w, e.Alignment);
            SnapshotIO.OptionalEnum(w, e.Verdict); w.Write(e.Night);
        }
        internal static EvidenceRecord Evidence(BinaryReader r, int index, int count, int day, CharacterId self, IReadOnlyList<RoleSpec> catalog)
        {
            if (SnapshotIO.Int(r, 0, 200000) != index) throw new InvalidDataException("Nonsequential evidence.");
            int observedDay = SnapshotIO.Int(r, 1, day + 1);
            var kind = SnapshotIO.EnumValue<EvidenceKind>(r); CharacterId actor = RequiredId(r, count);
            CharacterId? target = Id(r, count); var intent = SnapshotIO.OptionalEnum<SpeechIntent>(r);
            RoleId? role = Role(r, catalog); var alignment = SnapshotIO.OptionalEnum<Faction>(r);
            var verdict = SnapshotIO.OptionalEnum<Verdict>(r); int night = SnapshotIO.Int(r, 0, day);
            if (observedDay > day && kind != EvidenceKind.PrivateInvestigation) throw new InvalidDataException("Future public evidence.");
            if (kind == EvidenceKind.PrivateInvestigation && (actor != self || !target.HasValue || !alignment.HasValue))
                throw new InvalidDataException("Foreign or incomplete private evidence.");
            return new EvidenceRecord(index, observedDay, kind, actor, target, intent, role, alignment, verdict, night);
        }
        internal static void Settings(BinaryWriter w, ReasoningSettings s)
        {
            w.Write(s.AssertionWeight); w.Write(s.ContradictionPenalty); w.Write(s.AggressiveBonus); w.Write(s.CautiousThreshold);
            w.Write(s.GullibleBonus); w.Write(s.LoyaltyBonus); w.Write(s.DeceptionBonus); w.Write(s.ExposureThreshold);
        }
        internal static ReasoningSettings Settings(BinaryReader r) => new ReasoningSettings(SnapshotIO.Int(r, 0, 100), SnapshotIO.Int(r, 0, 100),
            SnapshotIO.Int(r, 0, 100), SnapshotIO.Int(r, 0, 100), SnapshotIO.Int(r, 0, 100), SnapshotIO.Int(r, 0, 100),
            SnapshotIO.Int(r, 0, 100), SnapshotIO.Int(r, 0, 100));
    }
}
