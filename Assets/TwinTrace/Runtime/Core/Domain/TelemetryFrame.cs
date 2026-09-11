using System;

namespace TwinTrace.Domain
{
    public readonly struct TelemetryFrame
    {
        public TelemetryFrame(
            DeviceId deviceId,
            long sequence,
            DateTimeOffset capturedAtUtc,
            float temperatureCelsius,
            float rpm,
            float loadPercent)
        {
            if (!deviceId.IsValid)
            {
                throw new ArgumentException("Telemetry requires a valid device ID.", nameof(deviceId));
            }

            if (sequence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            EnsureFinite(temperatureCelsius, nameof(temperatureCelsius));
            EnsureFinite(rpm, nameof(rpm));
            EnsureFinite(loadPercent, nameof(loadPercent));

            if (rpm < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rpm));
            }

            if (loadPercent < 0f || loadPercent > 100f)
            {
                throw new ArgumentOutOfRangeException(nameof(loadPercent));
            }

            DeviceId = deviceId;
            Sequence = sequence;
            CapturedAtUtc = capturedAtUtc;
            TemperatureCelsius = temperatureCelsius;
            Rpm = rpm;
            LoadPercent = loadPercent;
        }

        public DeviceId DeviceId { get; }
        public long Sequence { get; }
        public DateTimeOffset CapturedAtUtc { get; }
        public float TemperatureCelsius { get; }
        public float Rpm { get; }
        public float LoadPercent { get; }

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Telemetry values must be finite.");
            }
        }
    }
}

