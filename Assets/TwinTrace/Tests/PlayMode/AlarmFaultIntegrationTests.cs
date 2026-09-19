using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TwinTrace.Alarms;
using TwinTrace.Domain;
using TwinTrace.Telemetry;
using UnityEngine;
using UnityEngine.TestTools;

namespace TwinTrace.Tests
{
    public sealed class AlarmFaultIntegrationTests
    {
        private static readonly DeviceId MotorId = new DeviceId("MOTOR-001");
        private static readonly DeviceId ConveyorId = new DeviceId("CONVEYOR-001");

        [UnityTest]
        public IEnumerator SimulatedFaults_ReachCriticalAndRecoverThroughTelemetry()
        {
            var root = new GameObject("AlarmFaultIntegration-Test");
            SimulationTelemetrySource source = root.AddComponent<SimulationTelemetrySource>();
            DeviceState motor = CreateState(MotorId, DeviceKind.Motor);
            DeviceState conveyor = CreateState(ConveyorId, DeviceKind.Conveyor);
            var registry = new DeviceRegistry();
            registry.Register(motor);
            registry.Register(conveyor);
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceAlarmState motorAlarm = monitor.Register(motor);
            DeviceAlarmState conveyorAlarm = monitor.Register(conveyor);
            var transitions = new List<AlarmTransition>();
            monitor.Transitioned += transitions.Add;
            source.Configure(new[]
            {
                new SimulationDeviceProfile(
                    motor.Descriptor,
                    55f,
                    1450f,
                    60f,
                    0f),
                new SimulationDeviceProfile(
                    conveyor.Descriptor,
                    42f,
                    500f,
                    50f,
                    0f)
            });
            source.FrameReceived += frame => registry.Apply(frame);
            source.Begin();

            Assert.That(
                source.InjectFault(MotorId, SimulatedFaultType.MotorOverheat),
                Is.EqualTo(FaultInjectionResult.Injected));
            Assert.That(
                source.InjectFault(ConveyorId, SimulatedFaultType.ConveyorJam),
                Is.EqualTo(FaultInjectionResult.Injected));

            float criticalDeadline = Time.realtimeSinceStartup + 8f;
            while ((motorAlarm.Severity != AlarmSeverity.Critical ||
                    conveyorAlarm.Severity != AlarmSeverity.Critical) &&
                   Time.realtimeSinceStartup < criticalDeadline)
            {
                yield return null;
            }

            Assert.That(motorAlarm.Severity, Is.EqualTo(AlarmSeverity.Critical));
            Assert.That(conveyorAlarm.Severity, Is.EqualTo(AlarmSeverity.Critical));
            AssertReachedWarningAndCritical(transitions, MotorId);
            AssertReachedWarningAndCritical(transitions, ConveyorId);

            Assert.That(source.ClearFault(MotorId), Is.True);
            Assert.That(source.ClearFault(ConveyorId), Is.True);

            float recoveryDeadline = Time.realtimeSinceStartup + 2f;
            while ((motorAlarm.Severity != AlarmSeverity.None ||
                    conveyorAlarm.Severity != AlarmSeverity.None) &&
                   Time.realtimeSinceStartup < recoveryDeadline)
            {
                yield return null;
            }

            Assert.That(motorAlarm.Severity, Is.EqualTo(AlarmSeverity.None));
            Assert.That(conveyorAlarm.Severity, Is.EqualTo(AlarmSeverity.None));
            AssertCleared(transitions, MotorId);
            AssertCleared(transitions, ConveyorId);

            source.End();
            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        private static DeviceState CreateState(DeviceId id, DeviceKind kind)
        {
            return new DeviceState(new DeviceDescriptor(id, kind, id.ToString()));
        }

        private static void AssertReachedWarningAndCritical(
            IEnumerable<AlarmTransition> transitions,
            DeviceId id)
        {
            bool reachedWarning = false;
            bool reachedCritical = false;
            foreach (AlarmTransition transition in transitions)
            {
                if (transition.DeviceId != id)
                {
                    continue;
                }

                reachedWarning |= transition.CurrentSeverity == AlarmSeverity.Warning;
                reachedCritical |= transition.CurrentSeverity == AlarmSeverity.Critical;
            }

            Assert.That(reachedWarning, Is.True, $"{id} did not reach Warning.");
            Assert.That(reachedCritical, Is.True, $"{id} did not reach Critical.");
        }

        private static void AssertCleared(
            IEnumerable<AlarmTransition> transitions,
            DeviceId id)
        {
            foreach (AlarmTransition transition in transitions)
            {
                if (transition.DeviceId == id &&
                    transition.TransitionKind == AlarmTransitionKind.Cleared)
                {
                    return;
                }
            }

            Assert.Fail($"{id} did not raise a Cleared transition.");
        }
    }
}
