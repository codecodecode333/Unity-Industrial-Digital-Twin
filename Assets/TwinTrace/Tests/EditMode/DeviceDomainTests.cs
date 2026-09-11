using System;
using NUnit.Framework;
using TwinTrace.Domain;

namespace TwinTrace.Tests
{
    public sealed class DeviceDomainTests
    {
        private static readonly DateTimeOffset CapturedAt =
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void DeviceId_TrimsAndComparesOrdinally()
        {
            Assert.That(new DeviceId(" MOTOR-001 "), Is.EqualTo(new DeviceId("MOTOR-001")));
            Assert.That(new DeviceId("motor-001"), Is.Not.EqualTo(new DeviceId("MOTOR-001")));
        }

        [Test]
        public void NewDevice_IsOffline()
        {
            DeviceState device = new DeviceState(new DeviceId("MOTOR-001"));

            Assert.That(device.OperationalState, Is.EqualTo(DeviceOperationalState.Offline));
        }

        [TestCase(0f, DeviceOperationalState.Idle)]
        [TestCase(1450f, DeviceOperationalState.Running)]
        public void ApplyingTelemetry_UpdatesValuesAndOperationalState(
            float rpm,
            DeviceOperationalState expectedState)
        {
            DeviceId id = new DeviceId("MOTOR-001");
            DeviceState device = new DeviceState(id);
            TelemetryFrame frame = new TelemetryFrame(id, 7, CapturedAt, 58.5f, rpm, 72f);

            device.Apply(frame);

            Assert.That(device.TemperatureCelsius, Is.EqualTo(58.5f));
            Assert.That(device.Rpm, Is.EqualTo(rpm));
            Assert.That(device.LoadPercent, Is.EqualTo(72f));
            Assert.That(device.LastSequence, Is.EqualTo(7));
            Assert.That(device.OperationalState, Is.EqualTo(expectedState));
        }

        [Test]
        public void Registry_RoutesFrameByDeviceId()
        {
            DeviceId id = new DeviceId("MOTOR-001");
            DeviceState device = new DeviceState(id);
            DeviceRegistry registry = new DeviceRegistry();
            registry.Register(device);

            bool applied = registry.TryApply(
                new TelemetryFrame(id, 1, CapturedAt, 55f, 1450f, 60f));

            Assert.That(applied, Is.True);
            Assert.That(device.LastSequence, Is.EqualTo(1));
        }

        [Test]
        public void Registry_RejectsUnknownDeviceWithoutChangingRegisteredDevice()
        {
            DeviceState registered = new DeviceState(new DeviceId("MOTOR-001"));
            DeviceRegistry registry = new DeviceRegistry();
            registry.Register(registered);

            bool applied = registry.TryApply(new TelemetryFrame(
                new DeviceId("MOTOR-999"),
                1,
                CapturedAt,
                55f,
                1450f,
                60f));

            Assert.That(applied, Is.False);
            Assert.That(registered.OperationalState, Is.EqualTo(DeviceOperationalState.Offline));
        }
    }
}

