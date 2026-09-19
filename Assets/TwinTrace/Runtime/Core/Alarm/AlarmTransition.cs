using System;
using TwinTrace.Domain;

namespace TwinTrace.Alarms
{
    public readonly struct AlarmTransition
    {
        public AlarmTransition(
            DeviceId deviceId,
            AlarmCode alarmCode,
            AlarmSeverity previousSeverity,
            AlarmSeverity currentSeverity,
            AlarmTransitionKind transitionKind,
            DateTimeOffset occurredAtUtc)
        {
            DeviceId = deviceId;
            AlarmCode = alarmCode;
            PreviousSeverity = previousSeverity;
            CurrentSeverity = currentSeverity;
            TransitionKind = transitionKind;
            OccurredAtUtc = occurredAtUtc;
        }

        public DeviceId DeviceId { get; }
        public AlarmCode AlarmCode { get; }
        public AlarmSeverity PreviousSeverity { get; }
        public AlarmSeverity CurrentSeverity { get; }
        public AlarmTransitionKind TransitionKind { get; }
        public DateTimeOffset OccurredAtUtc { get; }
    }
}
