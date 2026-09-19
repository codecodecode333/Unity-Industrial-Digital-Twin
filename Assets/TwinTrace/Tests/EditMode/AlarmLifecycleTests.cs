using System;
using System.Collections.Generic;
using NUnit.Framework;
using TwinTrace.Alarms;
using TwinTrace.Domain;

namespace TwinTrace.Tests
{
    public sealed class AlarmLifecycleTests
    {
        private static readonly DateTimeOffset CapturedAt =
            new DateTimeOffset(2026, 9, 20, 4, 0, 0, TimeSpan.Zero);

        [Test]
        public void WarningRaisedOnceAndRepeatedSeverityProducesNoEvents()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);
            var transitions = new List<AlarmTransition>();
            int changedCount = 0;
            monitor.Transitioned += transitions.Add;
            alarm.Changed += _ => changedCount++;

            Apply(state, 1, 70f, 1450f, 60f);
            Apply(state, 2, 72f, 1450f, 60f);
            Apply(state, 3, 74f, 1450f, 60f);

            Assert.That(changedCount, Is.EqualTo(1));
            Assert.That(transitions, Has.Count.EqualTo(1));
            AssertTransition(
                transitions[0],
                AlarmTransitionKind.Raised,
                AlarmSeverity.None,
                AlarmSeverity.Warning,
                CapturedAt.AddSeconds(1));
        }

        [Test]
        public void WarningToCriticalRaisesSeverityChanged()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);
            var transitions = new List<AlarmTransition>();
            monitor.Transitioned += transitions.Add;

            Apply(state, 1, 70f, 1450f, 60f);
            DateTimeOffset activatedAt = alarm.ActivatedAtUtc;
            Apply(state, 2, 80f, 1450f, 60f);

            Assert.That(transitions, Has.Count.EqualTo(2));
            AssertTransition(
                transitions[1],
                AlarmTransitionKind.SeverityChanged,
                AlarmSeverity.Warning,
                AlarmSeverity.Critical,
                CapturedAt.AddSeconds(2));
            Assert.That(alarm.ActivatedAtUtc, Is.EqualTo(activatedAt));
        }

        [Test]
        public void WarningToNoneRaisesClearedWithTelemetryTimestamp()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);
            var transitions = new List<AlarmTransition>();
            monitor.Transitioned += transitions.Add;

            Apply(state, 1, 70f, 1450f, 60f);
            Apply(state, 2, 66f, 1450f, 60f);

            Assert.That(transitions, Has.Count.EqualTo(2));
            AssertTransition(
                transitions[1],
                AlarmTransitionKind.Cleared,
                AlarmSeverity.Warning,
                AlarmSeverity.None,
                CapturedAt.AddSeconds(2));
            Assert.That(transitions[1].AlarmCode, Is.EqualTo(AlarmCode.MotorOverheat));
            Assert.That(alarm.Code, Is.EqualTo(AlarmCode.None));
            Assert.That(alarm.ActivatedAtUtc, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(alarm.LastChangedAtUtc, Is.EqualTo(state.LastTelemetryAtUtc));
        }

        [Test]
        public void AlarmStatesRemainIndependentPerDevice()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState motorOne = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceState motorTwo = CreateState("MOTOR-002", DeviceKind.Motor);
            DeviceState conveyor = CreateState("CONVEYOR-001", DeviceKind.Conveyor);
            DeviceAlarmState motorOneAlarm = monitor.Register(motorOne);
            DeviceAlarmState motorTwoAlarm = monitor.Register(motorTwo);
            DeviceAlarmState conveyorAlarm = monitor.Register(conveyor);

            Apply(motorOne, 1, 82f, 1450f, 60f);

            Assert.That(motorOneAlarm.Severity, Is.EqualTo(AlarmSeverity.Critical));
            Assert.That(motorTwoAlarm.Severity, Is.EqualTo(AlarmSeverity.None));
            Assert.That(conveyorAlarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        [Test]
        public void StaleTelemetryDoesNotEvaluateAlarmAgain()
        {
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);
            int changedCount = 0;
            alarm.Changed += _ => changedCount++;
            Apply(state, 10, 70f, 1450f, 60f);

            TelemetryApplyResult result = state.Apply(new TelemetryFrame(
                state.Id,
                9,
                CapturedAt.AddSeconds(20),
                50f,
                1450f,
                60f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.Stale));
            Assert.That(changedCount, Is.EqualTo(1));
            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.Warning));
        }

        [Test]
        public void DisposeStopsFurtherAlarmEvaluation()
        {
            var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceState state = CreateState("MOTOR-001", DeviceKind.Motor);
            DeviceAlarmState alarm = monitor.Register(state);
            monitor.Dispose();

            Apply(state, 1, 82f, 1450f, 60f);

            Assert.That(alarm.Severity, Is.EqualTo(AlarmSeverity.None));
        }

        private static void AssertTransition(
            AlarmTransition transition,
            AlarmTransitionKind kind,
            AlarmSeverity previous,
            AlarmSeverity current,
            DateTimeOffset occurredAt)
        {
            Assert.That(transition.TransitionKind, Is.EqualTo(kind));
            Assert.That(transition.PreviousSeverity, Is.EqualTo(previous));
            Assert.That(transition.CurrentSeverity, Is.EqualTo(current));
            Assert.That(transition.OccurredAtUtc, Is.EqualTo(occurredAt));
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
