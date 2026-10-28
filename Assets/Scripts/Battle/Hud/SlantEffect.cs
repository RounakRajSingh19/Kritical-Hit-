using UnityEngine;
using UnityEngine.UI;

namespace MysticKombat
{
    // Shears a UI graphic sideways around its own vertical centre, turning a
    // rectangle into a parallelogram (slanted short edges). Add it AFTER an Outline
    // so the outline is slanted too.
    public class SlantEffect : BaseMeshEffect
    {
        public float amount = 0.28f;      // x shift per unit of height; + leans the top right, - leans it left

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            float cy = graphic.rectTransform.rect.center.y;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                var p = v.position;
                p.x += (p.y - cy) * amount;
                v.position = p;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
