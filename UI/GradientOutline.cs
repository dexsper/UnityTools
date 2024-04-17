using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UI
{
    [AddComponentMenu("UI/Effects/GradientOutline", 81)]
    public class GradientOutline : BaseMeshEffect
    {
        public enum GradientDirection
        {
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft
        }

        private const float MaxEffectDistance = 600f;

        [SerializeField] private Gradient _effectGradient;
        [SerializeField] private Vector2 _effectDistance;
        [SerializeField] private bool _useGraphicAlpha;
        [SerializeField] private GradientDirection _direction;

        protected GradientOutline()
        {
        }

        protected override void OnValidate()
        {
            EffectDistance = _effectDistance;
            base.OnValidate();
        }

        public Gradient EffectGradient
        {
            get => _effectGradient;
            set
            {
                _effectGradient = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public Vector2 EffectDistance
        {
            get => _effectDistance;
            set
            {
                _effectDistance = new Vector2(
                    Mathf.Clamp(value.x, -MaxEffectDistance, MaxEffectDistance),
                    Mathf.Clamp(value.y, -MaxEffectDistance, MaxEffectDistance)
                );

                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public bool UseGraphicAlpha
        {
            get => _useGraphicAlpha;
            set
            {
                _useGraphicAlpha = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public GradientDirection Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        protected void ApplyShadowZeroAlloc(List<UIVertex> verts, int start, int end, float x, float y)
        {
            for (int i = start; i < end; ++i)
            {
                var vt = verts[i];
                verts.Add(vt);

                Vector3 v = vt.position;
                v.x += x;
                v.y += y;
                vt.position = v;

                float t = 0f;
                switch (_direction)
                {
                    case GradientDirection.TopToBottom:
                        t = Mathf.InverseLerp(0, _effectDistance.y, -v.y);
                        break;
                    case GradientDirection.BottomToTop:
                        t = Mathf.InverseLerp(0, _effectDistance.y, v.y);
                        break;
                    case GradientDirection.LeftToRight:
                        t = Mathf.InverseLerp(0, _effectDistance.x, -v.x);
                        break;
                    case GradientDirection.RightToLeft:
                        t = Mathf.InverseLerp(0, _effectDistance.x, v.x);
                        break;
                }

                Color32 newColor = _effectGradient.Evaluate(t);
                
                if (_useGraphicAlpha)
                    newColor.a = (byte)((newColor.a * verts[i].color.a) / 255);
                
                vt.color = newColor;
                verts[i] = vt;
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            var verts = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(verts);

            int initialVertexCount = verts.Count;

            int start = 0;
            int end = initialVertexCount;

            ApplyShadowZeroAlloc(verts, start, end, _effectDistance.x, _effectDistance.y);
            start = end;

            end = verts.Count;
            ApplyShadowZeroAlloc(verts, start, end, _effectDistance.x, -_effectDistance.y);
            start = end;

            end = verts.Count;
            ApplyShadowZeroAlloc(verts, start, end, -_effectDistance.x, _effectDistance.y);
            start = end;

            end = verts.Count;
            ApplyShadowZeroAlloc(verts, start, end, -_effectDistance.x, -_effectDistance.y);

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
            ListPool<UIVertex>.Release(verts);
        }
    }
}
