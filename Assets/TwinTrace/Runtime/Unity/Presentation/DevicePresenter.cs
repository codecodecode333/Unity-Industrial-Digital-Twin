using TwinTrace.Domain;
using UnityEngine;

namespace TwinTrace.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DevicePresenter : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [Header("3D Visualization")]
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Transform[] rotatingParts = new Transform[0];
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField, Min(0f)] private float rpmVisualizationScale = 0.05f;
        [SerializeField] private Color offlineColor = new Color(0.35f, 0.35f, 0.35f);
        [SerializeField] private Color idleColor = new Color(0.1f, 0.4f, 1f);
        [SerializeField] private Color runningColor = new Color(0.1f, 0.8f, 0.2f);

        [Header("Runtime State (Read Only)")]
        [SerializeField] private string deviceId = string.Empty;
        [SerializeField] private string operationalState = DeviceOperationalState.Offline.ToString();
        [SerializeField] private float temperatureCelsius;
        [SerializeField] private float rpm;
        [SerializeField] private float loadPercent;
        [SerializeField] private long sequence;
        [SerializeField] private string capturedAtUtc = string.Empty;

        private DeviceState _state;
        private MaterialPropertyBlock _propertyBlock;

        public string DisplayedDeviceId => deviceId;
        public DeviceOperationalState DisplayedOperationalState =>
            _state == null ? DeviceOperationalState.Offline : _state.OperationalState;
        public long DisplayedSequence => sequence;
        public bool IsBound => _state != null;

        public void ConfigureVisuals(
            Renderer deviceStatusRenderer,
            Transform[] deviceRotatingParts,
            Vector3 deviceRotationAxis,
            float visualizationScale)
        {
            statusRenderer = deviceStatusRenderer;
            rotatingParts = deviceRotatingParts ?? new Transform[0];
            rotationAxis = deviceRotationAxis;
            rpmVisualizationScale = Mathf.Max(0f, visualizationScale);
            ApplyStatusColor(_state?.OperationalState ?? DeviceOperationalState.Offline);
        }

        public void Bind(DeviceState state)
        {
            Unbind();
            _state = state;
            _state.Changed += HandleStateChanged;
            Refresh(_state);
        }

        public void Unbind()
        {
            if (_state != null)
            {
                _state.Changed -= HandleStateChanged;
                _state = null;
            }

            deviceId = string.Empty;
            operationalState = DeviceOperationalState.Offline.ToString();
            temperatureCelsius = 0f;
            rpm = 0f;
            loadPercent = 0f;
            sequence = 0;
            capturedAtUtc = "No telemetry";
            ApplyStatusColor(DeviceOperationalState.Offline);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void HandleStateChanged(DeviceState state)
        {
            Refresh(state);
        }

        private void Refresh(DeviceState state)
        {
            deviceId = state.Id.ToString();
            operationalState = state.OperationalState.ToString();
            temperatureCelsius = state.TemperatureCelsius;
            rpm = state.Rpm;
            loadPercent = state.LoadPercent;
            sequence = state.LastSequence;
            capturedAtUtc = state.LastTelemetryAtUtc == default
                ? "No telemetry"
                : state.LastTelemetryAtUtc.ToString("HH:mm:ss.fff 'UTC'");
            ApplyStatusColor(state.OperationalState);
        }

        private void Update()
        {
            if (_state == null || _state.Rpm <= 0f)
            {
                return;
            }

            float rotationDegrees = _state.Rpm * rpmVisualizationScale * Time.deltaTime;
            foreach (Transform rotatingPart in rotatingParts)
            {
                if (rotatingPart != null)
                {
                    rotatingPart.Rotate(rotationAxis, rotationDegrees, Space.Self);
                }
            }
        }

        private void ApplyStatusColor(DeviceOperationalState state)
        {
            if (statusRenderer == null || statusRenderer.sharedMaterial == null)
            {
                return;
            }

            int propertyId;
            if (statusRenderer.sharedMaterial.HasProperty(BaseColorProperty))
            {
                propertyId = BaseColorProperty;
            }
            else if (statusRenderer.sharedMaterial.HasProperty(ColorProperty))
            {
                propertyId = ColorProperty;
            }
            else
            {
                return;
            }

            Color color = state switch
            {
                DeviceOperationalState.Idle => idleColor,
                DeviceOperationalState.Running => runningColor,
                _ => offlineColor
            };

            _propertyBlock ??= new MaterialPropertyBlock();
            statusRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(propertyId, color);
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
