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
        private static readonly DeviceId PresentedDeviceId = new DeviceId("MOTOR-001");

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

            SimulationDeviceProfile[] simulationProfiles = CreateSimulationProfiles();
            _registry = new DeviceRegistry();
            DeviceState presentedDevice = null;

            foreach (SimulationDeviceProfile profile in simulationProfiles)
            {
                var device = new DeviceState(profile.Descriptor);
                _registry.Register(device);

                if (device.Id == PresentedDeviceId)
                {
                    presentedDevice = device;
                }
            }

            if (telemetrySource is SimulationTelemetrySource simulation)
            {
                simulation.Configure(simulationProfiles);
            }

            if (presentedDevice == null)
            {
                throw new InvalidOperationException(
                    $"Presented device '{PresentedDeviceId}' is not registered.");
            }

            devicePresenter.Bind(presentedDevice);
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
            TelemetryApplyResult result = _registry.Apply(frame);
            if (result == TelemetryApplyResult.UnknownDevice)
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

        private static SimulationDeviceProfile[] CreateSimulationProfiles()
        {
            return new[]
            {
                new SimulationDeviceProfile(
                    new DeviceDescriptor(
                        new DeviceId("MOTOR-001"),
                        DeviceKind.Motor,
                        "Cooling Motor A"),
                    55f,
                    1450f,
                    60f,
                    0f),
                new SimulationDeviceProfile(
                    new DeviceDescriptor(
                        new DeviceId("MOTOR-002"),
                        DeviceKind.Motor,
                        "Cooling Motor B"),
                    48f,
                    1000f,
                    40f,
                    1.8f),
                new SimulationDeviceProfile(
                    new DeviceDescriptor(
                        new DeviceId("CONVEYOR-001"),
                        DeviceKind.Conveyor,
                        "Main Conveyor"),
                    42f,
                    500f,
                    50f,
                    3.4f)
            };
        }
    }
}
