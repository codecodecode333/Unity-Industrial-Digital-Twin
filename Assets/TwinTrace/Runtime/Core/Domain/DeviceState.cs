using System;

namespace TwinTrace.Domain
{
    public sealed class DeviceState
    {
        private const float RunningRpmThreshold = 1f;

        public DeviceState(DeviceDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Descriptor = descriptor;
            OperationalState = DeviceOperationalState.Offline;
        }

        public event Action<DeviceState> Changed;

        public DeviceDescriptor Descriptor { get; }
        public DeviceId Id => Descriptor.Id;
        public bool HasTelemetry { get; private set; }
        public long LastSequence { get; private set; }
        public DateTimeOffset LastTelemetryAtUtc { get; private set; }
        public float TemperatureCelsius { get; private set; }
        public float Rpm { get; private set; }
        public float LoadPercent { get; private set; }
        public DeviceOperationalState OperationalState { get; private set; }

        public TelemetryApplyResult Apply(TelemetryFrame frame)
        {
            if (frame.DeviceId != Id)
            {
                throw new InvalidOperationException(
                    $"Cannot apply telemetry for '{frame.DeviceId}' to device '{Id}'.");
            }

            if (HasTelemetry && frame.Sequence <= LastSequence)
            {
                return TelemetryApplyResult.Stale;
            }

            LastSequence = frame.Sequence;
            LastTelemetryAtUtc = frame.CapturedAtUtc;
            TemperatureCelsius = frame.TemperatureCelsius;
            Rpm = frame.Rpm;
            LoadPercent = frame.LoadPercent;
            OperationalState = frame.Rpm > RunningRpmThreshold
                ? DeviceOperationalState.Running
                : DeviceOperationalState.Idle;
            HasTelemetry = true;

            Changed?.Invoke(this);
            return TelemetryApplyResult.Applied;
        }
    }
}
