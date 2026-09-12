using System;
using NUnit.Framework;
using TwinTrace.Domain;

namespace TwinTrace.Tests
{
    public sealed class DeviceDescriptorTests
    {
        [Test]
        public void DeviceId_TrimsAndComparesOrdinally()
        {
            Assert.That(new DeviceId(" MOTOR-001 "), Is.EqualTo(new DeviceId("MOTOR-001")));
            Assert.That(new DeviceId("motor-001"), Is.Not.EqualTo(new DeviceId("MOTOR-001")));
        }

        [Test]
        public void Constructor_CreatesImmutableDescriptor()
        {
            DeviceDescriptor descriptor = new DeviceDescriptor(
                new DeviceId("MOTOR-001"),
                DeviceKind.Motor,
                " Cooling Motor A ");

            Assert.That(descriptor.Id, Is.EqualTo(new DeviceId("MOTOR-001")));
            Assert.That(descriptor.Kind, Is.EqualTo(DeviceKind.Motor));
            Assert.That(descriptor.DisplayName, Is.EqualTo("Cooling Motor A"));
        }

        [Test]
        public void Constructor_RejectsInvalidDeviceId()
        {
            Assert.Throws<ArgumentException>(() =>
                new DeviceDescriptor(default, DeviceKind.Motor, "Cooling Motor A"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsEmptyDisplayName(string displayName)
        {
            Assert.Throws<ArgumentException>(() =>
                new DeviceDescriptor(new DeviceId("MOTOR-001"), DeviceKind.Motor, displayName));
        }
    }
}
