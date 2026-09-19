using System;
using TwinTrace.Domain;

namespace TwinTrace.Alarms
{
    public sealed class AlarmEvaluator
    {
        public AlarmEvaluation Evaluate(DeviceState device, DeviceAlarmState currentAlarm)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (currentAlarm == null)
            {
                throw new ArgumentNullException(nameof(currentAlarm));
            }

            if (device.Id != currentAlarm.DeviceId)
            {
                throw new InvalidOperationException(
                    $"Cannot evaluate alarm '{currentAlarm.DeviceId}' for device '{device.Id}'.");
            }

            if (!device.HasTelemetry)
            {
                return new AlarmEvaluation(currentAlarm.Code, currentAlarm.Severity);
            }

            return device.Descriptor.Kind switch
            {
                DeviceKind.Motor => EvaluateMotor(
                    device.TemperatureCelsius,
                    currentAlarm.Severity),
                DeviceKind.Conveyor => EvaluateConveyor(
                    device.Rpm,
                    device.LoadPercent,
                    currentAlarm.Severity),
                _ => AlarmEvaluation.None
            };
        }

        private static AlarmEvaluation EvaluateMotor(
            float temperatureCelsius,
            AlarmSeverity currentSeverity)
        {
            AlarmSeverity nextSeverity = currentSeverity switch
            {
                AlarmSeverity.None when temperatureCelsius >= 80f => AlarmSeverity.Critical,
                AlarmSeverity.None when temperatureCelsius >= 70f => AlarmSeverity.Warning,
                AlarmSeverity.Warning when temperatureCelsius >= 80f => AlarmSeverity.Critical,
                AlarmSeverity.Warning when temperatureCelsius < 67f => AlarmSeverity.None,
                AlarmSeverity.Critical when temperatureCelsius < 77f => AlarmSeverity.Warning,
                _ => currentSeverity
            };

            return CreateEvaluation(AlarmCode.MotorOverheat, nextSeverity);
        }

        private static AlarmEvaluation EvaluateConveyor(
            float rpm,
            float loadPercent,
            AlarmSeverity currentSeverity)
        {
            bool isCritical = rpm <= 50f && loadPercent >= 90f;
            bool isWarning = rpm <= 250f && loadPercent >= 75f;
            bool canClearWarning = rpm >= 300f && loadPercent <= 70f;
            bool canDowngradeCritical = rpm >= 100f && loadPercent <= 85f;

            AlarmSeverity nextSeverity = currentSeverity switch
            {
                AlarmSeverity.None when isCritical => AlarmSeverity.Critical,
                AlarmSeverity.None when isWarning => AlarmSeverity.Warning,
                AlarmSeverity.Warning when isCritical => AlarmSeverity.Critical,
                AlarmSeverity.Warning when canClearWarning => AlarmSeverity.None,
                AlarmSeverity.Critical when canDowngradeCritical => AlarmSeverity.Warning,
                _ => currentSeverity
            };

            return CreateEvaluation(AlarmCode.ConveyorJam, nextSeverity);
        }

        private static AlarmEvaluation CreateEvaluation(
            AlarmCode code,
            AlarmSeverity severity)
        {
            return severity == AlarmSeverity.None
                ? AlarmEvaluation.None
                : new AlarmEvaluation(code, severity);
        }
    }
}
