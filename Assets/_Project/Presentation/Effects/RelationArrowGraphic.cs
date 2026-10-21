using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Effects
{
    /// <summary>Static directed edges rebuilt only when the selected viewpoint or layout changes.</summary>
    public sealed class RelationArrowGraphic : MaskableGraphic
    {
        private IReadOnlyList<Vector2> _targets;
        private IReadOnlyList<Color> _colors;
        /// <summary>Configures normalized target offsets from the central observer.</summary>
        public void Configure(IReadOnlyList<Vector2> targets, IReadOnlyList<Color> colors)
        { _targets = targets; _colors = colors; raycastTarget = false; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if (_targets == null) return;
            for (int i = 0; i < _targets.Count; i++)
            {
                Vector2 target = Vector2.Scale(_targets[i], rectTransform.rect.size);
                Vector2 direction = target.normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                Vector2 start = direction * 65;
                Vector2 end = target - direction * 70;
                int vertex = mesh.currentVertCount;
                mesh.AddVert(start - normal, _colors[i], Vector2.zero);
                mesh.AddVert(start + normal, _colors[i], Vector2.zero);
                mesh.AddVert(end + normal, _colors[i], Vector2.zero);
                mesh.AddVert(end - normal, _colors[i], Vector2.zero);
                mesh.AddTriangle(vertex, vertex + 1, vertex + 2); mesh.AddTriangle(vertex, vertex + 2, vertex + 3);
                vertex = mesh.currentVertCount;
                mesh.AddVert(end + direction * 9, _colors[i], Vector2.zero);
                mesh.AddVert(end - direction * 5 + normal * 5, _colors[i], Vector2.zero);
                mesh.AddVert(end - direction * 5 - normal * 5, _colors[i], Vector2.zero);
                mesh.AddTriangle(vertex, vertex + 1, vertex + 2);
            }
        }
    }
}
