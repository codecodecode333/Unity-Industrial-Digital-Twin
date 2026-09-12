using System;
using NUnit.Framework;
using TwinTrace.Domain;

namespace TwinTrace.Tests
{
    public sealed class DeviceStateTests
    {
        private static readonly DateTimeOffset FirstCapturedAt =
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static readonly DateTimeOffset SecondCapturedAt =
            FirstCapturedAt.AddSeconds(1);

        [Test]
        public void NewDevice_UsesDescriptorAndHasNoTelemetry()
        {
            DeviceDescriptor descriptor = CreateDescriptor();
            DeviceState device = new DeviceState(descriptor);

            Assert.That(device.Descriptor, Is.SameAs(descriptor));
            Assert.That(device.Id, Is.EqualTo(descriptor.Id));
            Assert.That(device.HasTelemetry, Is.False);
            Assert.That(device.OperationalState, Is.EqualTo(DeviceOperationalState.Offline));
        }

        [TestCase(0f, DeviceOperationalState.Idle)]
        [TestCase(1450f, DeviceOperationalState.Running)]
        public void Apply_UpdatesValuesAndOperationalState(
            float rpm,
            DeviceOperationalState expectedState)
        {
            DeviceState device = CreateState();

            TelemetryApplyResult result = device.Apply(
                CreateFrame(7, FirstCapturedAt, 58.5f, rpm, 72f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.Applied));
            Assert.That(device.HasTelemetry, Is.True);
            Assert.That(device.TemperatureCelsius, Is.EqualTo(58.5f));
            Assert.That(device.Rpm, Is.EqualTo(rpm));
            Assert.That(device.LoadPercent, Is.EqualTo(72f));
            Assert.That(device.LastSequence, Is.EqualTo(7));
            Assert.That(device.OperationalState, Is.EqualTo(expectedState));
        }

        [Test]
        public void Apply_AcceptsSequenceZeroAsFirstFrame()
        {
            DeviceState device = CreateState();

            TelemetryApplyResult result = device.Apply(
                CreateFrame(0, FirstCapturedAt, 55f, 1450f, 60f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.Applied));
            Assert.That(device.HasTelemetry, Is.True);
            Assert.That(device.LastSequence, Is.EqualTo(0));
        }

        [Test]
        public void Apply_AcceptsIncreasingSequence()
        {
            DeviceState device = CreateState();
            device.Apply(CreateFrame(10, FirstCapturedAt, 55f, 1400f, 60f));

            TelemetryApplyResult result = device.Apply(
                CreateFrame(11, SecondCapturedAt, 56f, 1450f, 61f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.Applied));
            Assert.That(device.LastSequence, Is.EqualTo(11));
        }

        [TestCase(9)]
        [TestCase(10)]
        public void Apply_RejectsOlderOrDuplicateSequence(long staleSequence)
        {
            DeviceState device = CreateState();
            device.Apply(CreateFrame(10, FirstCapturedAt, 55f, 1400f, 60f));

            TelemetryApplyResult result = device.Apply(
                CreateFrame(staleSequence, SecondCapturedAt, 70f, 0f, 20f));

            Assert.That(result, Is.EqualTo(TelemetryApplyResult.Stale));
        }

        [Test]
        public void StaleFrame_DoesNotChangeCurrentState()
        {
            DeviceState device = CreateState();
            device.Apply(CreateFrame(10, FirstCapturedAt, 55f, 1400f, 60f));

            device.Apply(CreateFrame(9, SecondCapturedAt, 70f, 0f, 20f));

            Assert.That(device.TemperatureCelsius, Is.EqualTo(55f));
            Assert.That(device.Rpm, Is.EqualTo(1400f));
            Assert.That(device.LoadPercent, Is.EqualTo(60f));
            Assert.That(device.OperationalState, Is.EqualTo(DeviceOperationalState.Running));
            Assert.That(device.LastSequence, Is.EqualTo(10));
            Assert.That(device.LastTelemetryAtUtc, Is.EqualTo(FirstCapturedAt));
        }

        [Test]
        public void StaleFrame_DoesNotRaiseChangedEvent()
        {
            DeviceState device = CreateState();
            int changedCount = 0;
            device.Changed += _ => changedCount++;
            device.Apply(CreateFrame(10, FirstCapturedAt, 55f, 1400f, 60f));
            changedCount = 0;

            device.Apply(CreateFrame(9, SecondCapturedAt, 70f, 0f, 20f));

            Assert.That(changedCount, Is.Zero);
        }

        private static DeviceState CreateState()
        {
            return new DeviceState(CreateDescriptor());
        }

        private static DeviceDescriptor CreateDescriptor()
        {
            return new DeviceDescriptor(
                new DeviceId("MOTOR-001"),
                DeviceKind.Motor,
                "Cooling Motor A");
        }

        private static TelemetryFrame CreateFrame(
            long sequence,
            DateTimeOffset capturedAt,
            float temperature,
            float rpm,
            float load)
        {
            return new TelemetryFrame(
                new DeviceId("MOTOR-001"),
                sequence,
                capturedAt,
                temperature,
                rpm,
                load);
        }
    }
}
