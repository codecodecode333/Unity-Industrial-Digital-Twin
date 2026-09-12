using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TwinTrace.Composition;
using TwinTrace.Domain;
using TwinTrace.Telemetry;
using UnityEngine;
using UnityEngine.TestTools;

namespace TwinTrace.Tests
{
    public sealed class MultiDeviceSimulationTests
    {
        private static readonly DeviceId MotorOneId = new DeviceId("MOTOR-001");
        private static readonly DeviceId MotorTwoId = new DeviceId("MOTOR-002");
        private static readonly DeviceId ConveyorId = new DeviceId("CONVEYOR-001");

        [UnityTest]
        public IEnumerator Bootstrap_ConfiguresExpectedSimulationDevices()
        {
            GameObject root = new GameObject("TwinTrace-MultiDevice-Test");
            root.AddComponent<TwinTraceBootstrap>();
            yield return null;

            SimulationTelemetrySource source = root.GetComponent<SimulationTelemetrySource>();
            Assert.That(source.Profiles, Has.Count.EqualTo(3));
            AssertProfile(source.Profiles, MotorOneId, DeviceKind.Motor, "Cooling Motor A");
            AssertProfile(source.Profiles, MotorTwoId, DeviceKind.Motor, "Cooling Motor B");
            AssertProfile(source.Profiles, ConveyorId, DeviceKind.Conveyor, "Main Conveyor");

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Source_PublishesIndependentFramesToRegisteredDevices()
        {
            GameObject root = new GameObject("Simulation-Source-Test");
            SimulationTelemetrySource source = root.AddComponent<SimulationTelemetrySource>();
            SimulationDeviceProfile[] profiles = CreateProfiles();
            Dictionary<DeviceId, DeviceState> devices = CreateDevices(profiles);
            DeviceRegistry registry = CreateRegistry(devices);
            var receivedFrames = new List<TelemetryFrame>();

            source.Configure(profiles);
            source.FrameReceived += frame =>
            {
                receivedFrames.Add(frame);
                Assert.That(registry.Apply(frame), Is.EqualTo(TelemetryApplyResult.Applied));
            };

            source.Begin();

            Assert.That(receivedFrames, Has.Count.EqualTo(3));
            Assert.That(devices[MotorOneId].LastSequence, Is.EqualTo(1));
            Assert.That(devices[MotorTwoId].LastSequence, Is.EqualTo(1));
            Assert.That(devices[ConveyorId].LastSequence, Is.EqualTo(1));
            Assert.That(devices[MotorOneId].HasTelemetry, Is.True);
            Assert.That(devices[MotorTwoId].HasTelemetry, Is.True);
            Assert.That(devices[ConveyorId].HasTelemetry, Is.True);
            Assert.That(
                devices[MotorOneId].TemperatureCelsius,
                Is.Not.EqualTo(devices[MotorTwoId].TemperatureCelsius));
            Assert.That(
                devices[MotorTwoId].Rpm,
                Is.Not.EqualTo(devices[ConveyorId].Rpm));

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(devices[MotorOneId].LastSequence, Is.GreaterThan(1));
            Assert.That(devices[MotorTwoId].LastSequence, Is.EqualTo(devices[MotorOneId].LastSequence));
            Assert.That(devices[ConveyorId].LastSequence, Is.EqualTo(devices[MotorOneId].LastSequence));

            source.End();
            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MotorOneFrame_DoesNotChangeOtherDevices()
        {
            GameObject root = new GameObject("Simulation-Routing-Test");
            SimulationTelemetrySource source = root.AddComponent<SimulationTelemetrySource>();
            SimulationDeviceProfile[] profiles = CreateProfiles();
            Dictionary<DeviceId, DeviceState> devices = CreateDevices(profiles);
            DeviceRegistry registry = CreateRegistry(devices);
            TelemetryFrame? motorOneFrame = null;

            source.Configure(profiles);
            source.FrameReceived += frame =>
            {
                if (frame.DeviceId == MotorOneId)
                {
                    motorOneFrame = frame;
                }
            };
            source.Begin();

            Assert.That(motorOneFrame.HasValue, Is.True);
            Assert.That(
                registry.Apply(motorOneFrame.Value),
                Is.EqualTo(TelemetryApplyResult.Applied));
            Assert.That(devices[MotorOneId].HasTelemetry, Is.True);
            Assert.That(devices[MotorTwoId].HasTelemetry, Is.False);
            Assert.That(devices[ConveyorId].HasTelemetry, Is.False);

            source.End();
            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator End_StopsPublishingTelemetry()
        {
            GameObject root = new GameObject("Simulation-Stop-Test");
            SimulationTelemetrySource source = root.AddComponent<SimulationTelemetrySource>();
            int receivedCount = 0;

            source.Configure(CreateProfiles());
            source.FrameReceived += _ => receivedCount++;
            source.Begin();
            yield return new WaitForSecondsRealtime(0.6f);

            source.End();
            int countAfterEnd = receivedCount;
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(receivedCount, Is.EqualTo(countAfterEnd));

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        private static SimulationDeviceProfile[] CreateProfiles()
        {
            return new[]
            {
                CreateProfile(MotorOneId, DeviceKind.Motor, "Cooling Motor A", 55f, 1450f, 60f, 0f),
                CreateProfile(MotorTwoId, DeviceKind.Motor, "Cooling Motor B", 48f, 1000f, 40f, 1.8f),
                CreateProfile(ConveyorId, DeviceKind.Conveyor, "Main Conveyor", 42f, 500f, 50f, 3.4f)
            };
        }

        private static SimulationDeviceProfile CreateProfile(
            DeviceId id,
            DeviceKind kind,
            string displayName,
            float temperature,
            float rpm,
            float load,
            float phaseOffset)
        {
            return new SimulationDeviceProfile(
                new DeviceDescriptor(id, kind, displayName),
                temperature,
                rpm,
                load,
                phaseOffset);
        }

        private static Dictionary<DeviceId, DeviceState> CreateDevices(
            IReadOnlyList<SimulationDeviceProfile> profiles)
        {
            var devices = new Dictionary<DeviceId, DeviceState>();
            foreach (SimulationDeviceProfile profile in profiles)
            {
                devices.Add(profile.Descriptor.Id, new DeviceState(profile.Descriptor));
            }

            return devices;
        }

        private static DeviceRegistry CreateRegistry(
            IReadOnlyDictionary<DeviceId, DeviceState> devices)
        {
            var registry = new DeviceRegistry();
            foreach (DeviceState device in devices.Values)
            {
                registry.Register(device);
            }

            return registry;
        }

        private static void AssertProfile(
            IReadOnlyList<SimulationDeviceProfile> profiles,
            DeviceId id,
            DeviceKind kind,
            string displayName)
        {
            SimulationDeviceProfile found = null;
            foreach (SimulationDeviceProfile profile in profiles)
            {
                if (profile.Descriptor.Id == id)
                {
                    found = profile;
                    break;
                }
            }

            Assert.That(found, Is.Not.Null);
            Assert.That(found.Descriptor.Kind, Is.EqualTo(kind));
            Assert.That(found.Descriptor.DisplayName, Is.EqualTo(displayName));
        }
    }
}
