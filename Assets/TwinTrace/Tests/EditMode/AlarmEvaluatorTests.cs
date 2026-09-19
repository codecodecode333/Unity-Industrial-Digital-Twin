using System;
using NUnit.Framework;
using TwinTrace.Alarms;
using TwinTrace.Domain;

namespace TwinTrace.Tests
{
    public sealed class AlarmEvaluatorTests
    {
        private static readonly DateTimeOffset CapturedAt =
            new DateTimeOffset(2026, 9, 20, 3, 0, 0, TimeSpan.Zero);

        [Test]
        public void DeviceWithoutTelemetry_HasNoAlarm()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);

            Assert.That(alarm.HasActiveAlarm, Is.False);
            Assert.That(alarm.Code, Is.EqualTo(AlarmCode.None));
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        [TestCase(69f, AlarmSeverity.None)]
        [TestCase(70f, AlarmSeverity.Warning)]
        [TestCase(80f, AlarmSeverity.Critical)]
        public void Motor_NoneStateUsesEntryThresholds(
            float temperature,
            AlarmSeverity expectedSeverity)
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);

            Apply(state, 1, temperature, 1450f, 60f);

            Assert.That(alarm.Severity, Is.EqualTo(expectedSeverity));
        }

        [Test]
        public void Motor_WarningUsesClearHysteresis()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);

            Apply(state, 1, 70f, 1450f, 60f);
            Apply(state, 2, 69f, 1450f, 60f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Warning));

            Apply(state, 3, 66f, 1450f, 60f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        [Test]
        public void Motor_CriticalUsesDowngradeHysteresis()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);

            Apply(state, 1, 80f, 1450f, 60f);
            Apply(state, 2, 78f, 1450f, 60f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Critical));

            Apply(state, 3, 76f, 1450f, 60f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Warning));

            Apply(state, 4, 60f, 1450f, 60f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        [Test]
        public void Conveyor_NormalTelemetryHasNoAlarm()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("CONVEYOR-001", DeviceKind.Conveyor);
            DeviceAlarmState alarm = monitor.Register(state);

            Apply(state, 1, 42f, 500f, 50f);

            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        [Test]
        public void Conveyor_WarningUsesCombinedThresholdAndHysteresis()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("CONVEYOR-001", DeviceKind.Conveyor);
            DeviceAlarmState alarm = monitor.Register(state);

            Apply(state, 1, 42f, 250f, 75f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Warning));

            Apply(state, 2, 42f, 280f, 72f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Warning));

            Apply(state, 3, 42f, 300f, 70f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        [Test]
        public void Conveyor_CriticalDowngradesThroughWarning()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("CONVEYOR-001", DeviceKind.Conveyor);
            DeviceAlarmState alarm = monitor.Register(state);

            Apply(state, 1, 42f, 50f, 90f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Critical));

            Apply(state, 2, 42f, 100f, 85f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Warning));

            Apply(state, 3, 42f, 500f, 50f);
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        private static DeviceState CreateState(string id, DeviceKind kind)
        {
            return new DeviceState(new DeviceDescriptor(
                new DeviceId(id),
                kind,
                id));
        }

        private static void Apply(
            DeviceState state,
            long sequence,
            float temperature,
            float rpm,
            float load)
        {
            state.Apply(new TelemetryFrame(
                state.Id,
                sequence,
                CapturedAt.AddSeconds(sequence),
                temperature,
                rpm,
                load));
        }
    }
}
