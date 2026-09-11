using System;
using TwinTrace.Domain;
using UnityEngine;

namespace TwinTrace.Telemetry
{
    [DisallowMultipleComponent]
    public sealed class SimulationTelemetrySource : TelemetrySourceBehaviour
    {
        [SerializeField, Min(0.05f)] private float intervalSeconds = 0.5f;
        [SerializeField] private float baseTemperatureCelsius = 55f;
        [SerializeField, Min(0f)] private float baseRpm = 1450f;
        [SerializeField, Range(0f, 100f)] private float baseLoadPercent = 62f;

        private DeviceId _deviceId;
        private bool _isConfigured;
        private bool _isRunning;
        private float _elapsedSeconds;
        private float _simulationSeconds;
        private long _sequence;

        public void Configure(DeviceId deviceId)
        {
            if (!deviceId.IsValid)
            {
                throw new ArgumentException("Simulation requires a valid device ID.", nameof(deviceId));
            }

            _deviceId = deviceId;
            _isConfigured = true;
        }

        public override void Begin()
        {
            if (_isRunning)
            {
                return;
            }

            if (!_isConfigured)
            {
                throw new InvalidOperationException("Configure the simulation before starting it.");
            }

            _isRunning = true;
            _elapsedSeconds = 0f;
            PublishFrame();
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
            PublishFrame();
        }

        private void PublishFrame()
        {
            float temperature = baseTemperatureCelsius + Mathf.Sin(_simulationSeconds * 0.7f) * 6f;
            float rpm = baseRpm + Mathf.Sin(_simulationSeconds * 1.1f) * 120f;
            float load = Mathf.Clamp(
                baseLoadPercent + Mathf.Sin(_simulationSeconds * 0.5f) * 18f,
                0f,
                100f);

            _sequence++;
            Publish(new TelemetryFrame(
                _deviceId,
                _sequence,
                DateTimeOffset.UtcNow,
                temperature,
                Mathf.Max(0f, rpm),
                load));

            _simulationSeconds += intervalSeconds;
        }

        private void OnValidate()
        {
            intervalSeconds = Mathf.Max(0.05f, intervalSeconds);
            baseRpm = Mathf.Max(0f, baseRpm);
            baseLoadPercent = Mathf.Clamp(baseLoadPercent, 0f, 100f);
        }
    }
}

