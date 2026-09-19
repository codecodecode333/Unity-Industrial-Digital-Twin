using System;
using System.Collections.Generic;
using TwinTrace.Alarms;
using TwinTrace.Domain;
using TwinTrace.Presentation;
using TwinTrace.Telemetry;
using UnityEngine;

namespace TwinTrace.Composition
{
    [DisallowMultipleComponent]
    public sealed class TwinTraceBootstrap : MonoBehaviour
    {
        [SerializeField] private TelemetrySourceBehaviour telemetrySource;

        private DeviceRegistry _registry;
        private AlarmMonitor _alarmMonitor;
        private ITelemetrySource _activeSource;

        private void Awake()
        {
            telemetrySource ??= GetComponent<TelemetrySourceBehaviour>();
            telemetrySource ??= gameObject.AddComponent<SimulationTelemetrySource>();

            if (telemetrySource == null)
            {
                throw new InvalidOperationException("A telemetry source component is required.");
            }

            DeviceDescriptor[] descriptors = CreateDeviceDescriptors();
            _registry = new DeviceRegistry();
            _alarmMonitor = new AlarmMonitor(new AlarmEvaluator());

            foreach (DeviceDescriptor descriptor in descriptors)
            {
                var state = new DeviceState(descriptor);
                _registry.Register(state);
                _alarmMonitor.Register(state);
            }

            if (telemetrySource is SimulationTelemetrySource simulation)
            {
                simulation.Configure(CreateSimulationProfiles(descriptors));
            }

            BindSceneDevices();
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

        private void OnDestroy()
        {
            _alarmMonitor?.Dispose();
            _alarmMonitor = null;
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
        }

        private void BindSceneDevices()
        {
            DeviceBinding[] bindings = FindObjectsByType<DeviceBinding>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var boundIds = new HashSet<DeviceId>();

            foreach (DeviceBinding binding in bindings)
            {
                if (!binding.TryGetId(out DeviceId id))
                {
                    Debug.LogWarning(
                        $"Device binding on '{binding.gameObject.name}' has an invalid Device ID.",
                        binding);
                    continue;
                }

                if (!_registry.TryGet(id, out DeviceState state))
                {
                    Debug.LogWarning(
                        $"No registered device matches binding '{id}' on '{binding.gameObject.name}'.",
                        binding);
                    continue;
                }

                if (!_alarmMonitor.TryGet(id, out DeviceAlarmState alarmState))
                {
                    Debug.LogWarning(
                        $"No alarm state matches binding '{id}' on '{binding.gameObject.name}'.",
                        binding);
                    continue;
                }

                if (!boundIds.Add(id))
                {
                    Debug.LogWarning(
                        $"Multiple scene bindings reference device '{id}'.",
                        binding);
                }

                binding.Bind(state, alarmState);
            }
        }

        private static DeviceDescriptor[] CreateDeviceDescriptors()
        {
            return new[]
            {
                new DeviceDescriptor(
                    new DeviceId("MOTOR-001"),
                    DeviceKind.Motor,
                    "Cooling Motor A"),
                new DeviceDescriptor(
                    new DeviceId("MOTOR-002"),
                    DeviceKind.Motor,
                    "Cooling Motor B"),
                new DeviceDescriptor(
                    new DeviceId("CONVEYOR-001"),
                    DeviceKind.Conveyor,
                    "Main Conveyor")
            };
        }

        private static SimulationDeviceProfile[] CreateSimulationProfiles(
            IReadOnlyList<DeviceDescriptor> descriptors)
        {
            return new[]
            {
                new SimulationDeviceProfile(
                    descriptors[0],
                    55f,
                    1450f,
                    60f,
                    0f),
                new SimulationDeviceProfile(
                    descriptors[1],
                    48f,
                    1000f,
                    40f,
                    1.8f),
                new SimulationDeviceProfile(
                    descriptors[2],
                    42f,
                    500f,
                    50f,
                    3.4f)
            };
        }
    }
}
