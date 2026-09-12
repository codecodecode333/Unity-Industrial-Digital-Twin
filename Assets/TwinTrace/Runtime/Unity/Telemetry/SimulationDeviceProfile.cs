using System;
using TwinTrace.Domain;

namespace TwinTrace.Telemetry
{
    public sealed class SimulationDeviceProfile
    {
        public SimulationDeviceProfile(
            DeviceDescriptor descriptor,
            float baseTemperatureCelsius,
            float baseRpm,
            float baseLoadPercent,
            float phaseOffset)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));

            EnsureFinite(baseTemperatureCelsius, nameof(baseTemperatureCelsius));
            EnsureFinite(baseRpm, nameof(baseRpm));
            EnsureFinite(baseLoadPercent, nameof(baseLoadPercent));
            EnsureFinite(phaseOffset, nameof(phaseOffset));

            if (baseRpm < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRpm));
            }

            if (baseLoadPercent < 0f || baseLoadPercent > 100f)
            {
                throw new ArgumentOutOfRangeException(nameof(baseLoadPercent));
            }

            BaseTemperatureCelsius = baseTemperatureCelsius;
            BaseRpm = baseRpm;
            BaseLoadPercent = baseLoadPercent;
            PhaseOffset = phaseOffset;
        }

        public DeviceDescriptor Descriptor { get; }
        public float BaseTemperatureCelsius { get; }
        public float BaseRpm { get; }
        public float BaseLoadPercent { get; }
        public float PhaseOffset { get; }

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Simulation values must be finite.");
            }
        }
    }
}
