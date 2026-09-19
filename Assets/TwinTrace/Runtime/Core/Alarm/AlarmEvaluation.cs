using System;

namespace TwinTrace.Alarms
{
    public readonly struct AlarmEvaluation
    {
        public AlarmEvaluation(AlarmCode code, AlarmSeverity severity)
        {
            if ((code == AlarmCode.None) != (severity == AlarmSeverity.None))
            {
                throw new ArgumentException(
                    "Alarm code and severity must both be None or both be active.");
            }

            Code = code;
            Severity = severity;
        }

        public AlarmCode Code { get; }
        public AlarmSeverity Severity { get; }

        public static AlarmEvaluation None =>
            new AlarmEvaluation(AlarmCode.None, AlarmSeverity.None);
    }
}
