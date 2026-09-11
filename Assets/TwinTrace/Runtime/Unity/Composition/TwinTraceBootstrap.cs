using System;
using TwinTrace.Domain;
using TwinTrace.Presentation;
using TwinTrace.Telemetry;
using UnityEngine;

namespace TwinTrace.Composition
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DevicePresenter))]
    public sealed class TwinTraceBootstrap : MonoBehaviour
    {
        [SerializeField] private string motorDeviceId = "MOTOR-001";
        [SerializeField] private TelemetrySourceBehaviour telemetrySource;
        [SerializeField] private DevicePresenter devicePresenter;

        private DeviceRegistry _registry;
        private ITelemetrySource _activeSource;

        private void Awake()
        {
            telemetrySource ??= GetComponent<TelemetrySourceBehaviour>();
            telemetrySource ??= gameObject.AddComponent<SimulationTelemetrySource>();
            devicePresenter ??= GetComponent<DevicePresenter>();

            if (telemetrySource == null)
            {
                throw new InvalidOperationException("A telemetry source component is required.");
            }

            if (devicePresenter == null)
            {
                throw new InvalidOperationException("A device presenter component is required.");
            }

            DeviceState motor = new DeviceState(new DeviceId(motorDeviceId));
            _registry = new DeviceRegistry();
            _registry.Register(motor);

            if (telemetrySource is SimulationTelemetrySource simulation)
            {
                simulation.Configure(motor.Id);
            }

            devicePresenter.Bind(motor);
            _activeSource = telemetrySource;
        }

        private void OnEnable()
        {
            if (_activeSource == null)
            {
                return;
            }

            _activeSource.FrameReceived += HandleFrameReceived;
            _activeSource.Begin();
        }

        private void OnDisable()
        {
            if (_activeSource == null)
            {
                return;
            }

            _activeSource.End();
            _activeSource.FrameReceived -= HandleFrameReceived;
        }

        private void HandleFrameReceived(TelemetryFrame frame)
        {
            if (!_registry.TryApply(frame))
            {
                Debug.LogWarning(
                    $"Ignoring telemetry for unregistered device '{frame.DeviceId}'.",
                    this);
            }
        }

        private void Reset()
        {
            telemetrySource = GetComponent<TelemetrySourceBehaviour>();
            telemetrySource ??= gameObject.AddComponent<SimulationTelemetrySource>();
            devicePresenter = GetComponent<DevicePresenter>();
        }
    }
}
