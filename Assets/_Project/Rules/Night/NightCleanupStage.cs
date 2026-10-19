namespace SocialStim.Rules.Night
{
    /// <summary>Notifications and previously scheduled penalties follow all simultaneous abilities.</summary>
    public sealed class NightCleanupStage : INightStage
    {
        public int Priority => 60;
        public void Resolve(NightContext context)
        {
            if (!context.Policy.Mission) return;
            foreach (var patient in context.Patients)
                context.Reports.Add(new NightReport(patient.Key, context.Attacked.Contains(patient.Value)
                    ? ReportKind.PatientAttacked : ReportKind.PatientSafe, patient.Value));
            foreach (var actor in context.Actors)
                if (actor.BacklashDue) context.Deaths.Add(actor.Id);
            foreach (var actor in context.BacklashScheduled)
                context.Reports.Add(new NightReport(actor, ReportKind.BacklashPending));
        }
    }
}
