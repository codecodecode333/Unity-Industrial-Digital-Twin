using System;
using System.Collections.Generic;
using TwinTrace.Domain;

namespace TwinTrace.Alarms
{
    public sealed class AlarmMonitor : IDisposable
    {
        private readonly AlarmEvaluator _evaluator;
        private readonly Dictionary<DeviceId, Registration> _registrations =
            new Dictionary<DeviceId, Registration>();
        private bool _isDisposed;

        public AlarmMonitor(AlarmEvaluator evaluator)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public event Action<AlarmTransition> Transitioned;

        public DeviceAlarmState Register(DeviceState device)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AlarmMonitor));
            }

            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            var alarmState = new DeviceAlarmState(device.Id);
            var registration = new Registration(device, alarmState);
            _registrations.Add(device.Id, registration);
            device.Changed += HandleDeviceChanged;
            alarmState.Transitioned += HandleTransitioned;

            if (device.HasTelemetry)
            {
                Evaluate(registration);
            }

            return alarmState;
        }

        public bool TryGet(DeviceId id, out DeviceAlarmState alarmState)
        {
            if (_registrations.TryGetValue(id, out Registration registration))
            {
                alarmState = registration.AlarmState;
                return true;
            }

            alarmState = null;
            return false;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (Registration registration in _registrations.Values)
            {
                registration.Device.Changed -= HandleDeviceChanged;
                registration.AlarmState.Transitioned -= HandleTransitioned;
            }

            _registrations.Clear();
            _isDisposed = true;
        }

        private void HandleDeviceChanged(DeviceState device)
        {
            if (_registrations.TryGetValue(device.Id, out Registration registration))
            {
                Evaluate(registration);
            }
        }

        private void Evaluate(Registration registration)
        {
            AlarmEvaluation evaluation = _evaluator.Evaluate(
                registration.Device,
                registration.AlarmState);
            registration.AlarmState.Apply(
                evaluation,
                registration.Device.LastTelemetryAtUtc);
        }

        private void HandleTransitioned(AlarmTransition transition)
        {
            Transitioned?.Invoke(transition);
        }

        private sealed class Registration
        {
            public Registration(DeviceState device, DeviceAlarmState alarmState)
            {
                Device = device;
                AlarmState = alarmState;
            }

            public DeviceState Device { get; }
            public DeviceAlarmState AlarmState { get; }
        }
    }
}
