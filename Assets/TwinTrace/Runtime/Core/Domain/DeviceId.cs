using System;

namespace TwinTrace.Domain
{
    public readonly struct DeviceId : IEquatable<DeviceId>
    {
        public DeviceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Device ID cannot be empty.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(DeviceId other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is DeviceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(DeviceId left, DeviceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DeviceId left, DeviceId right)
        {
            return !left.Equals(right);
        }
    }
}

