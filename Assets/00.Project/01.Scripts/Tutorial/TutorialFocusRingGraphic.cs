using UnityEngine;
using UnityEngine.UI;

namespace TaskTown.Tutorial
{
    /// <summary>
    /// 별도 Texture 없이 튜토리얼 강조용 도넛 형태를 그립니다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialFocusRingGraphic : MaskableGraphic
    {
        [SerializeField, Range(8, 128)] private int segments = 64;
        [SerializeField, Range(0.05f, 0.9f)] private float thickness = 0.18f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            int safeSegments = Mathf.Clamp(segments, 8, 128);
            float innerScale = 1f - Mathf.Clamp(thickness, 0.05f, 0.9f);
            Vector2 center = rect.center;
            Vector2 outerRadius = rect.size * 0.5f;
            Vector2 innerRadius = outerRadius * innerScale;
            Color32 vertexColor = color;

            for (int index = 0; index <= safeSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / safeSegments;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));

                vertexHelper.AddVert(
                    center + Vector2.Scale(direction, outerRadius),
                    vertexColor,
                    Vector2.zero);
                vertexHelper.AddVert(
                    center + Vector2.Scale(direction, innerRadius),
                    vertexColor,
                    Vector2.zero);
            }

            for (int index = 0; index < safeSegments; index++)
            {
                int outer = index * 2;
                int inner = outer + 1;
                int nextOuter = outer + 2;
                int nextInner = outer + 3;

                vertexHelper.AddTriangle(outer, nextOuter, inner);
                vertexHelper.AddTriangle(nextOuter, nextInner, inner);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
