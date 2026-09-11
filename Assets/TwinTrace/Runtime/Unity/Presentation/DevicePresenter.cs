using TwinTrace.Domain;
using UnityEngine;

namespace TwinTrace.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DevicePresenter : MonoBehaviour
    {
        [Header("Debug View")]
        [SerializeField] private bool showDebugPanel = true;
        [SerializeField] private Vector2 panelPosition = new Vector2(16f, 16f);

        [Header("Runtime State (Read Only)")]
        [SerializeField] private string deviceId = string.Empty;
        [SerializeField] private string operationalState = DeviceOperationalState.Offline.ToString();
        [SerializeField] private float temperatureCelsius;
        [SerializeField] private float rpm;
        [SerializeField] private float loadPercent;
        [SerializeField] private long sequence;
        [SerializeField] private string capturedAtUtc = string.Empty;

        private DeviceState _state;

        public string DisplayedDeviceId => deviceId;
        public DeviceOperationalState DisplayedOperationalState =>
            _state == null ? DeviceOperationalState.Offline : _state.OperationalState;
        public long DisplayedSequence => sequence;

        public void Bind(DeviceState state)
        {
            Unbind();
            _state = state;
            _state.Changed += HandleStateChanged;
            Refresh(_state);
        }

        public void Unbind()
        {
            if (_state == null)
            {
                return;
            }

            _state.Changed -= HandleStateChanged;
            _state = null;
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
        }

        private void OnGUI()
        {
            if (!showDebugPanel || _state == null)
            {
                return;
            }

            GUILayout.BeginArea(
                new Rect(panelPosition.x, panelPosition.y, 300f, 150f),
                "TwinTrace - Motor Telemetry",
                GUI.skin.window);
            GUILayout.Label($"Device: {deviceId}");
            GUILayout.Label($"State: {operationalState}");
            GUILayout.Label($"Temperature: {temperatureCelsius:F1} °C");
            GUILayout.Label($"RPM: {rpm:F0}");
            GUILayout.Label($"Load: {loadPercent:F1} %");
            GUILayout.EndArea();
        }
    }
}
