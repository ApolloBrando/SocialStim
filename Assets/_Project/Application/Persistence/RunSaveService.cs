using System;
using System.Collections.Generic;
using System.IO;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Phases;
using SocialStim.Rules.Reasoning;
using SocialStim.Rules.Roles;

namespace SocialStim.Application.Persistence
{
    /// <summary>Validation precedes session replacement; recovery never overwrites files implicitly.</summary>
    public static class RunSaveService
    {
        public static RunSession Load(FileSaveStore store, IEnumerable<RoleSpec> roles, IEnumerable<DialogueLine> dialogue,
            ReasoningSettings reasoning, out bool recovered)
        {
            recovered = false;
            try { return RunSession.Load(store.Read(SaveSlot.Run), roles, dialogue, reasoning); }
            catch (Exception e) when ((e is IOException || e is ArgumentException) && store.HasBackup(SaveSlot.Run))
            {
                RunSession session = RunSession.Load(store.Read(SaveSlot.Run, backup: true), roles, dialogue, reasoning);
                recovered = true;
                return session;
            }
        }

        public static void Save(FileSaveStore store, RunSession session, IEnumerable<RoleSpec> roles,
            IEnumerable<DialogueLine> dialogue, ReasoningSettings reasoning)
        {
            byte[] snapshot = session.Save();
            bool preserveBackup = false;
            if (store.Exists(SaveSlot.Run))
            {
                try { _ = RunSession.Load(store.Read(SaveSlot.Run), roles, dialogue, reasoning); }
                catch (Exception e) when (e is IOException || e is ArgumentException) { preserveBackup = true; }
            }
            store.Write(SaveSlot.Run, snapshot, preserveBackup);
        }
    }
}
