using System;
using TwinTrace.Domain;

namespace TwinTrace.Telemetry
{
    public interface ITelemetrySource
    {
        event Action<TelemetryFrame> FrameReceived;

        void Begin();
        void End();
    }
}

