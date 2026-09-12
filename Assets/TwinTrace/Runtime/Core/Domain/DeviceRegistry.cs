using System;
using System.Collections.Generic;

namespace TwinTrace.Domain
{
    public sealed class DeviceRegistry
    {
        private readonly Dictionary<DeviceId, DeviceState> _devices =
            new Dictionary<DeviceId, DeviceState>();

        public void Register(DeviceState device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            _devices.Add(device.Id, device);
        }

        public bool TryGet(DeviceId id, out DeviceState device)
        {
            return _devices.TryGetValue(id, out device);
        }

        public TelemetryApplyResult Apply(TelemetryFrame frame)
        {
            if (!_devices.TryGetValue(frame.DeviceId, out DeviceState device))
            {
                return TelemetryApplyResult.UnknownDevice;
            }

            return device.Apply(frame);
        }
    }
}
