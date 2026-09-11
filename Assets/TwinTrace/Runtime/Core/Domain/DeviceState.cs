using System;

namespace TwinTrace.Domain
{
    public sealed class DeviceState
    {
        private const float RunningRpmThreshold = 1f;

        public DeviceState(DeviceId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Device state requires a valid ID.", nameof(id));
            }

            Id = id;
            OperationalState = DeviceOperationalState.Offline;
        }

        public event Action<DeviceState> Changed;

        public DeviceId Id { get; }
        public long LastSequence { get; private set; }
        public DateTimeOffset LastTelemetryAtUtc { get; private set; }
        public float TemperatureCelsius { get; private set; }
        public float Rpm { get; private set; }
        public float LoadPercent { get; private set; }
        public DeviceOperationalState OperationalState { get; private set; }

        public void Apply(TelemetryFrame frame)
        {
            if (frame.DeviceId != Id)
            {
                throw new InvalidOperationException(
                    $"Cannot apply telemetry for '{frame.DeviceId}' to device '{Id}'.");
            }

            LastSequence = frame.Sequence;
            LastTelemetryAtUtc = frame.CapturedAtUtc;
            TemperatureCelsius = frame.TemperatureCelsius;
            Rpm = frame.Rpm;
            LoadPercent = frame.LoadPercent;
            OperationalState = frame.Rpm > RunningRpmThreshold
                ? DeviceOperationalState.Running
                : DeviceOperationalState.Idle;

            Changed?.Invoke(this);
        }
    }
}

