using System;
using TwinTrace.Domain;

namespace TwinTrace.Alarms
{
    public sealed class DeviceAlarmState
    {
        public DeviceAlarmState(DeviceId deviceId)
        {
            if (!deviceId.IsValid)
            {
                throw new ArgumentException("Alarm state requires a valid device ID.", nameof(deviceId));
            }

            DeviceId = deviceId;
        }

        public event Action<DeviceAlarmState> Changed;
        public event Action<AlarmTransition> Transitioned;

        public DeviceId DeviceId { get; }
        public AlarmCode Code { get; private set; }
        public AlarmSeverity Severity { get; private set; }
        public DateTimeOffset ActivatedAtUtc { get; private set; }
        public DateTimeOffset LastChangedAtUtc { get; private set; }
        public bool HasActiveAlarm => Severity != AlarmSeverity.None;

        internal bool Apply(AlarmEvaluation evaluation, DateTimeOffset occurredAtUtc)
        {
            if (Code == evaluation.Code && Severity == evaluation.Severity)
            {
                return false;
            }

            AlarmCode previousCode = Code;
            AlarmSeverity previousSeverity = Severity;
            AlarmTransitionKind transitionKind;

            if (previousSeverity == AlarmSeverity.None)
            {
                transitionKind = AlarmTransitionKind.Raised;
                ActivatedAtUtc = occurredAtUtc;
            }
            else if (evaluation.Severity == AlarmSeverity.None)
            {
                transitionKind = AlarmTransitionKind.Cleared;
                ActivatedAtUtc = default;
            }
            else
            {
                transitionKind = AlarmTransitionKind.SeverityChanged;
            }

            Code = evaluation.Code;
            Severity = evaluation.Severity;
            LastChangedAtUtc = occurredAtUtc;

            var transition = new AlarmTransition(
                DeviceId,
                transitionKind == AlarmTransitionKind.Cleared ? previousCode : Code,
                previousSeverity,
                Severity,
                transitionKind,
                occurredAtUtc);

            Changed?.Invoke(this);
            Transitioned?.Invoke(transition);
            return true;
        }
    }
}
