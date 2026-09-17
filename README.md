# TwinTrace

TwinTrace is a Unity 6 portfolio project for an industrial Digital Twin Incident Replay System.

The repository currently contains **Phase 002-C: 3D Digital Twin Presentation**:

- a Unity-independent device domain model;
- immutable device descriptors for motors and conveyors;
- ID-based routing for multiple registered device states;
- sequence-based rejection of stale telemetry;
- a replaceable telemetry source boundary;
- simulated telemetry for two motors and one conveyor;
- independent sequence and telemetry variation per device;
- Device ID bindings between Domain state and Scene objects;
- status colors and RPM-driven motion for three primitive-based 3D devices.

Device selection, details UI, MQTT, fault injection, alarms, recording, replay, and timeline features are intentionally out of scope for this phase.

## Requirements

- Unity `6000.3.9f1`

## Run the Phase 002 demo

1. Open the project in Unity.
2. Open `Assets/TwinTrace/Scenes/Phase002.unity`.
3. Enter Play Mode.
4. Observe the three status indicators and RPM-driven rotors/rollers.

If the demo scene needs to be recreated, use **TwinTrace > Create Phase 002 Demo Scene**.

## Runtime flow

```text
SimulationTelemetrySource
  -> MOTOR-001 / MOTOR-002 / CONVEYOR-001 frames
  -> TelemetryFrame event for each device
  -> TwinTraceBootstrap
  -> DeviceRegistry.Apply
  -> DeviceState.Apply
  -> DeviceState.Changed
  -> DevicePresenter
  -> MaterialPropertyBlock status color and RPM motion
```

Pure C# code lives in `Assets/TwinTrace/Runtime/Core`. Its assembly has `noEngineReferences` enabled so the domain cannot accidentally depend on `UnityEngine`. Unity-facing source, presentation, and composition code lives in `Assets/TwinTrace/Runtime/Unity`.

After the first frame is applied, a device accepts only frames whose sequence is greater than its last applied sequence. Duplicate and older frames return `TelemetryApplyResult.Stale` without changing state or raising the `Changed` event.
