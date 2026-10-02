using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // 스프라이트 의존 없이 원형 오디오 노드를 그리는 uGUI 그래픽.
    public sealed class ClueBoardAudioDisc : MaskableGraphic
    {
        [SerializeField, Range(12, 64)] private int _segments = 32;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            vh.AddVert(center, color, Vector2.one * 0.5f);
            for (int i = 0; i <= _segments; i++)
            {
                float angle = Mathf.PI * 2f * i / _segments;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(point, color, new Vector2((point.x - rect.xMin) / rect.width, (point.y - rect.yMin) / rect.height));
            }
            for (int i = 1; i <= _segments; i++) vh.AddTriangle(0, i, i + 1);
        }
    }
}
