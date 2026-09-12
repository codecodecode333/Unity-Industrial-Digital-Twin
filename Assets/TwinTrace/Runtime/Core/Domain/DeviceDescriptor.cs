using System;

namespace TwinTrace.Domain
{
    public sealed class DeviceDescriptor
    {
        public DeviceDescriptor(DeviceId id, DeviceKind kind, string displayName)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Device descriptor requires a valid ID.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(DeviceKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            Id = id;
            Kind = kind;
            DisplayName = displayName.Trim();
        }

        public DeviceId Id { get; }
        public DeviceKind Kind { get; }
        public string DisplayName { get; }
    }
}
