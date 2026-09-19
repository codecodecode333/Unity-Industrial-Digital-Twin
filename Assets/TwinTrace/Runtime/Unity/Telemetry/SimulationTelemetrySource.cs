using System;
using System.Collections.Generic;
using TwinTrace.Domain;
using UnityEngine;

namespace TwinTrace.Telemetry
{
    [DisallowMultipleComponent]
    public sealed class SimulationTelemetrySource : TelemetrySourceBehaviour
    {
        private const float OverheatDegreesPerSecond = 6f;
        private const float MaximumOverheatIncrease = 35f;
        private const float ConveyorJamDurationSeconds = 4f;
        private const float ConveyorJammedLoadPercent = 95f;

        [SerializeField, Min(0.05f)] private float intervalSeconds = 0.5f;

        private IReadOnlyList<SimulationDeviceProfile> _profiles =
            Array.Empty<SimulationDeviceProfile>();
        private SimulationDeviceRuntime[] _runtimeDevices =
            Array.Empty<SimulationDeviceRuntime>();
        private bool _isRunning;
        private float _elapsedSeconds;
        private float _simulationSeconds;

        public IReadOnlyList<SimulationDeviceProfile> Profiles => _profiles;

        public FaultInjectionResult InjectFault(
            DeviceId deviceId,
            SimulatedFaultType fault)
        {
            SimulationDeviceRuntime runtime = FindRuntime(deviceId);
            if (runtime == null)
            {
                return FaultInjectionResult.UnknownDevice;
            }

            if (fault == SimulatedFaultType.None)
            {
                return FaultInjectionResult.InvalidFault;
            }

            if (!IsCompatible(runtime.Profile.Descriptor.Kind, fault))
            {
                return FaultInjectionResult.IncompatibleDeviceKind;
            }

            runtime.ActiveFault = fault;
            runtime.FaultElapsedSeconds = 0f;
            return FaultInjectionResult.Injected;
        }

        public bool ClearFault(DeviceId deviceId)
        {
            SimulationDeviceRuntime runtime = FindRuntime(deviceId);
            if (runtime == null)
            {
                return false;
            }

            runtime.ActiveFault = SimulatedFaultType.None;
            runtime.FaultElapsedSeconds = 0f;
            return true;
        }

        public bool TryGetActiveFault(
            DeviceId deviceId,
            out SimulatedFaultType fault)
        {
            SimulationDeviceRuntime runtime = FindRuntime(deviceId);
            if (runtime == null)
            {
                fault = SimulatedFaultType.None;
                return false;
            }

            fault = runtime.ActiveFault;
            return true;
        }

        public void Configure(IReadOnlyList<SimulationDeviceProfile> profiles)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Stop the simulation before configuring it.");
            }

            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            if (profiles.Count == 0)
            {
                throw new ArgumentException("At least one simulation profile is required.", nameof(profiles));
            }

            var deviceIds = new HashSet<DeviceId>();
            var configuredProfiles = new SimulationDeviceProfile[profiles.Count];
            var runtimeDevices = new SimulationDeviceRuntime[profiles.Count];

            for (int index = 0; index < profiles.Count; index++)
            {
                SimulationDeviceProfile profile = profiles[index]
                    ?? throw new ArgumentException("Simulation profiles cannot contain null.", nameof(profiles));

                if (!deviceIds.Add(profile.Descriptor.Id))
                {
                    throw new ArgumentException(
                        $"Duplicate simulation device '{profile.Descriptor.Id}'.",
                        nameof(profiles));
                }

                configuredProfiles[index] = profile;
                runtimeDevices[index] = new SimulationDeviceRuntime(profile);
            }

            _profiles = Array.AsReadOnly(configuredProfiles);
            _runtimeDevices = runtimeDevices;
            _elapsedSeconds = 0f;
            _simulationSeconds = 0f;
        }

        public override void Begin()
        {
            if (_isRunning)
            {
                return;
            }

            if (_runtimeDevices.Length == 0)
            {
                throw new InvalidOperationException("Configure the simulation before starting it.");
            }

            _isRunning = true;
            _elapsedSeconds = 0f;
            PublishFrames();
        }

        public override void End()
        {
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            _elapsedSeconds += Time.unscaledDeltaTime;
            if (_elapsedSeconds < intervalSeconds)
            {
                return;
            }

            _elapsedSeconds %= intervalSeconds;
            PublishFrames();
        }

        private void PublishFrames()
        {
            DateTimeOffset capturedAtUtc = DateTimeOffset.UtcNow;

            foreach (SimulationDeviceRuntime runtime in _runtimeDevices)
            {
                SimulationDeviceProfile profile = runtime.Profile;
                float phase = _simulationSeconds + profile.PhaseOffset;
                float temperature =
                    profile.BaseTemperatureCelsius + Mathf.Sin(phase * 0.7f) * 6f;
                float rpm = profile.BaseRpm + Mathf.Sin(phase * 1.1f) * 120f;
                float load = Mathf.Clamp(
                    profile.BaseLoadPercent + Mathf.Sin(phase * 0.5f) * 18f,
                    0f,
                    100f);

                ApplyFault(runtime, ref temperature, ref rpm, ref load);

                runtime.Sequence++;
                Publish(new TelemetryFrame(
                    profile.Descriptor.Id,
                    runtime.Sequence,
                    capturedAtUtc,
                    temperature,
                    Mathf.Max(0f, rpm),
                    load));
            }

            _simulationSeconds += intervalSeconds;
        }

        private void ApplyFault(
            SimulationDeviceRuntime runtime,
            ref float temperature,
            ref float rpm,
            ref float load)
        {
            if (runtime.ActiveFault == SimulatedFaultType.None)
            {
                return;
            }

            runtime.FaultElapsedSeconds += intervalSeconds;

            switch (runtime.ActiveFault)
            {
                case SimulatedFaultType.MotorOverheat:
                    temperature += Mathf.Min(
                        runtime.FaultElapsedSeconds * OverheatDegreesPerSecond,
                        MaximumOverheatIncrease);
                    break;

                case SimulatedFaultType.ConveyorJam:
                    float progress = Mathf.Clamp01(
                        runtime.FaultElapsedSeconds / ConveyorJamDurationSeconds);
                    rpm = Mathf.Lerp(rpm, 0f, progress);
                    load = Mathf.Lerp(load, ConveyorJammedLoadPercent, progress);
                    break;
            }
        }

        private SimulationDeviceRuntime FindRuntime(DeviceId deviceId)
        {
            foreach (SimulationDeviceRuntime runtime in _runtimeDevices)
            {
                if (runtime.Profile.Descriptor.Id == deviceId)
                {
                    return runtime;
                }
            }

            return null;
        }

        private static bool IsCompatible(DeviceKind kind, SimulatedFaultType fault)
        {
            return (kind, fault) switch
            {
                (DeviceKind.Motor, SimulatedFaultType.MotorOverheat) => true,
                (DeviceKind.Conveyor, SimulatedFaultType.ConveyorJam) => true,
                _ => false
            };
        }

        private void OnValidate()
        {
            intervalSeconds = Mathf.Max(0.05f, intervalSeconds);
        }

        private sealed class SimulationDeviceRuntime
        {
            public SimulationDeviceRuntime(SimulationDeviceProfile profile)
            {
                Profile = profile;
            }

            public SimulationDeviceProfile Profile { get; }
            public long Sequence { get; set; }
            public SimulatedFaultType ActiveFault { get; set; }
            public float FaultElapsedSeconds { get; set; }
        }
    }
}
