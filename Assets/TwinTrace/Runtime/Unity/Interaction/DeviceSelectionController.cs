using System;
using TwinTrace.Presentation;
using UnityEngine;

namespace TwinTrace.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DeviceSelectionController : MonoBehaviour
    {
        [SerializeField] private Camera selectionCamera;
        [SerializeField, Min(0.1f)] private float maxRaycastDistance = 500f;
        [SerializeField] private DeviceDetailsPanel inputBlockingPanel;

        public event Action<DeviceBinding> SelectionChanged;

        public DeviceBinding Selected { get; private set; }

        public void Configure(Camera camera)
        {
            selectionCamera = camera;
        }

        public void ConfigureInputBlocker(DeviceDetailsPanel detailsPanel)
        {
            inputBlockingPanel = detailsPanel;
        }

        public void Select(DeviceBinding binding)
        {
            if (Selected == binding)
            {
                return;
            }

            SetSelectionVisual(Selected, false);
            Selected = binding;
            SetSelectionVisual(Selected, true);
            SelectionChanged?.Invoke(Selected);
        }

        public void ClearSelection()
        {
            Select(null);
        }

        public bool SelectAtScreenPosition(Vector2 screenPosition)
        {
            selectionCamera ??= Camera.main;
            if (selectionCamera == null)
            {
                ClearSelection();
                return false;
            }

            Ray ray = selectionCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance))
            {
                ClearSelection();
                return false;
            }

            DeviceBinding binding = ResolveBinding(hit.collider);
            Select(binding);
            return binding != null;
        }

        public bool HandlePointerClick(Vector2 screenPosition)
        {
            if (inputBlockingPanel != null &&
                inputBlockingPanel.ContainsScreenPosition(screenPosition))
            {
                return false;
            }

            return SelectAtScreenPosition(screenPosition);
        }

        public static DeviceBinding ResolveBinding(Component hitComponent)
        {
            return hitComponent == null
                ? null
                : hitComponent.GetComponentInParent<DeviceBinding>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandlePointerClick(Input.mousePosition);
            }
        }

        private void OnDisable()
        {
            ClearSelection();
        }

        private void Reset()
        {
            selectionCamera = Camera.main;
        }

        private static void SetSelectionVisual(DeviceBinding binding, bool selected)
        {
            if (binding != null &&
                binding.TryGetComponent(out DeviceSelectionVisual visual))
            {
                visual.SetSelected(selected);
            }
        }
    }
}
