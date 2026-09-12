using System;
using NUnit.Framework;
using TwinTrace.Domain;

namespace TwinTrace.Tests
{
    public sealed class DeviceRegistryTests
    {
        private static readonly DateTimeOffset CapturedAt =
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void Register_StoresMultipleDevices()
        {
            DeviceState motorOne = CreateDevice("MOTOR-001", DeviceKind.Motor);
            DeviceState motorTwo = CreateDevice("MOTOR-002", DeviceKind.Motor);
            DeviceState conveyor = CreateDevice("CONVEYOR-001", DeviceKind.Conveyor);
            DeviceRegistry registry = new DeviceRegistry();

            registry.Register(motorOne);
            registry.Register(motorTwo);
            registry.Register(conveyor);

            Assert.That(registry.TryGet(motorOne.Id, out DeviceState foundMotorOne), Is.True);
            Assert.That(registry.TryGet(motorTwo.Id, out DeviceState foundMotorTwo), Is.True);
            Assert.That(registry.TryGet(conveyor.Id, out DeviceState foundConveyor), Is.True);
            Assert.That(foundMotorOne, Is.SameAs(motorOne));
            Assert.That(foundMotorTwo, Is.SameAs(motorTwo));
            Assert.That(foundConveyor, Is.SameAs(conveyor));
        }

        [Test]
        public void Apply_RoutesTelemetryOnlyToMatchingDevice()
        {
            DeviceState motorOne = CreateDevice("MOTOR-001", DeviceKind.Motor);
            DeviceState motorTwo = CreateDevice("MOTOR-002", DeviceKind.Motor);
            DeviceState conveyor = CreateDevice("CONVEYOR-001", DeviceKind.Conveyor);
            DeviceRegistry registry = new DeviceRegistry();
            registry.Register(motorOne);
            registry.Register(motorTwo);
            registry.Register(conveyor);

            TelemetryApplyResult result = registry.Apply(
                CreateFrame(motorTwo.Id, 1, 58f, 1500f, 70f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.Applied));
            Assert.That(motorOne.HasTelemetry, Is.False);
            Assert.That(motorTwo.HasTelemetry, Is.True);
            Assert.That(motorTwo.TemperatureCelsius, Is.EqualTo(58f));
            Assert.That(conveyor.HasTelemetry, Is.False);
        }

        [Test]
        public void Apply_ReturnsUnknownDeviceForUnregisteredId()
        {
            DeviceRegistry registry = new DeviceRegistry();
            registry.Register(CreateDevice("MOTOR-001", DeviceKind.Motor));

            TelemetryApplyResult result = registry.Apply(
                CreateFrame(new DeviceId("MOTOR-999"), 1, 55f, 1450f, 60f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.UnknownDevice));
        }

        [Test]
        public void Apply_ReturnsStaleFromMatchingDevice()
        {
            DeviceState motor = CreateDevice("MOTOR-001", DeviceKind.Motor);
            DeviceRegistry registry = new DeviceRegistry();
            registry.Register(motor);
            registry.Apply(CreateFrame(motor.Id, 10, 55f, 1450f, 60f));

            TelemetryApplyResult result = registry.Apply(
                CreateFrame(motor.Id, 9, 70f, 0f, 20f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.Stale));
        }

        private static DeviceState CreateDevice(string id, DeviceKind kind)
        {
            return new DeviceState(new DeviceDescriptor(
                new DeviceId(id),
                kind,
                id));
        }

        private static TelemetryFrame CreateFrame(
            DeviceId id,
            long sequence,
            float temperature,
            float rpm,
            float load)
        {
            return new TelemetryFrame(id, sequence, CapturedAt, temperature, rpm, load);
        }
    }
}
