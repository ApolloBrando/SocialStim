using System.Runtime.CompilerServices;

// Expose internal belief models only to Unity's behavioral test assembly.
[assembly: InternalsVisibleTo("SocialStim.Tests.EditMode")]
