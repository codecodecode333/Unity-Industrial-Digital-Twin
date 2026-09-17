using System;
using UnityEngine;

namespace TwinTrace.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DeviceSelectionVisual : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [SerializeField] private GameObject marker;
        [SerializeField] private Color selectionColor = new Color(1f, 0.72f, 0.08f);

        private MaterialPropertyBlock _propertyBlock;

        public bool IsSelected { get; private set; }

        public void Configure(GameObject selectionMarker)
        {
            marker = selectionMarker != null
                ? selectionMarker
                : throw new ArgumentNullException(nameof(selectionMarker));
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (marker == null)
            {
                return;
            }

            if (selected)
            {
                ApplySelectionColor();
            }

            marker.SetActive(selected);
        }

        private void Awake()
        {
            SetSelected(false);
        }

        private void ApplySelectionColor()
        {
            Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer markerRenderer in renderers)
            {
                Material material = markerRenderer.sharedMaterial;
                if (material == null)
                {
                    continue;
                }

                int propertyId;
                if (material.HasProperty(BaseColorProperty))
                {
                    propertyId = BaseColorProperty;
                }
                else if (material.HasProperty(ColorProperty))
                {
                    propertyId = ColorProperty;
                }
                else
                {
                    continue;
                }

                _propertyBlock ??= new MaterialPropertyBlock();
                markerRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(propertyId, selectionColor);
                markerRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
