using SocialStim.Data.Definitions;
using UnityEditor;

namespace SocialStim.Editor
{
    /// <summary>Inline role errors while retaining Unity's normal serialized properties and multi-object Undo.</summary>
    [CustomEditor(typeof(RoleDefinition)), CanEditMultipleObjects]
    public sealed class RoleDefinitionEditor : ValidatedDefinitionEditor
    {
        protected override void ValidateDefinition(UnityEngine.Object definition) => ((RoleDefinition)definition).ToSpec();
        protected override void DrawActions()
        {
            if (targets.Length != 1) return;
            try
            {
                var role = ((RoleDefinition)target).ToSpec();
                EditorGUILayout.HelpBox("Effective claim permissions: truthful " + role.CanClaimTruthfully + ", bluff " + role.CanBluff + ".", MessageType.Info);
            }
            catch (System.ArgumentException) { }
        }
    }
}
