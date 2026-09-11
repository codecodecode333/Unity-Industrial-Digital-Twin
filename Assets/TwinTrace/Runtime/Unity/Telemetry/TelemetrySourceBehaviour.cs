using System;
using TwinTrace.Domain;
using UnityEngine;

namespace TwinTrace.Telemetry
{
    public abstract class TelemetrySourceBehaviour : MonoBehaviour, ITelemetrySource
    {
        public event Action<TelemetryFrame> FrameReceived;

        public abstract void Begin();
        public abstract void End();

        protected void Publish(TelemetryFrame frame)
        {
            FrameReceived?.Invoke(frame);
        }
    }
}

