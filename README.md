# TwinTrace

TwinTrace is a Unity 6 portfolio project for an industrial Digital Twin Incident Replay System.

The repository currently contains **Phase 001: Foundation** only:

- a Unity-independent device domain model;
- a replaceable telemetry source boundary;
- a deterministic simulated telemetry source;
- one motor device whose runtime state is shown in the Inspector and a small debug panel.

MQTT, fault injection, alarms, recording, replay, and timeline features are intentionally out of scope for this phase.

## Requirements

- Unity `6000.3.9f1`

## Run the Phase 001 demo

1. Open the project in Unity.
2. Open `Assets/TwinTrace/Scenes/Phase001.unity`.
3. Enter Play Mode.
4. Select the `MOTOR-001` GameObject to inspect its live values, or view the debug panel in the Game view.

If the demo scene needs to be recreated, use **TwinTrace > Create Phase 001 Demo Scene**.

## Runtime flow

```text
SimulationTelemetrySource
  -> TelemetryFrame event
  -> TwinTraceBootstrap
  -> DeviceRegistry
  -> DeviceState.Apply
  -> DevicePresenter
```

Pure C# code lives in `Assets/TwinTrace/Runtime/Core`. Its assembly has `noEngineReferences` enabled so the domain cannot accidentally depend on `UnityEngine`. Unity-facing source, presentation, and composition code lives in `Assets/TwinTrace/Runtime/Unity`.

