using System.Collections;
using NUnit.Framework;
using TwinTrace.Domain;
using TwinTrace.Interaction;
using TwinTrace.Presentation;
using TwinTrace.Telemetry;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TwinTrace.Tests
{
    public sealed class FaultInjectionTests
    {
        private static readonly DeviceId MotorOneId = new DeviceId("MOTOR-001");
        private static readonly DeviceId MotorTwoId = new DeviceId("MOTOR-002");
        private static readonly DeviceId ConveyorId = new DeviceId("CONVEYOR-001");
        private static readonly DeviceId UnknownId = new DeviceId("UNKNOWN-001");

        [UnityTest]
        public IEnumerator MotorOverheat_CanBeInjected()
        {
            SimulationTelemetrySource source = CreateSource(CreateMotorProfile(MotorOneId));

            FaultInjectionResult result = source.InjectFault(
                MotorOneId,
                SimulatedFaultType.MotorOverheat);

            Assert.That(result, Is.EqualTo(FaultInjectionResult.Injected));
            AssertActiveFault(source, MotorOneId, SimulatedFaultType.MotorOverheat);

            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConveyorJam_CanBeInjected()
        {
            SimulationTelemetrySource source = CreateSource(CreateConveyorProfile());

            FaultInjectionResult result = source.InjectFault(
                ConveyorId,
                SimulatedFaultType.ConveyorJam);

            Assert.That(result, Is.EqualTo(FaultInjectionResult.Injected));
            AssertActiveFault(source, ConveyorId, SimulatedFaultType.ConveyorJam);

            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConveyorJam_IsRejectedForMotor()
        {
            SimulationTelemetrySource source = CreateSource(CreateMotorProfile(MotorOneId));

            FaultInjectionResult result = source.InjectFault(
                MotorOneId,
                SimulatedFaultType.ConveyorJam);

            Assert.That(
                result,
                Is.EqualTo(FaultInjectionResult.IncompatibleDeviceKind));
            AssertActiveFault(source, MotorOneId, SimulatedFaultType.None);

            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MotorOverheat_IsRejectedForConveyor()
        {
            SimulationTelemetrySource source = CreateSource(CreateConveyorProfile());

            FaultInjectionResult result = source.InjectFault(
                ConveyorId,
                SimulatedFaultType.MotorOverheat);

            Assert.That(
                result,
                Is.EqualTo(FaultInjectionResult.IncompatibleDeviceKind));
            AssertActiveFault(source, ConveyorId, SimulatedFaultType.None);

            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnknownDevice_IsRejected()
        {
            SimulationTelemetrySource source = CreateSource(CreateMotorProfile(MotorOneId));

            FaultInjectionResult result = source.InjectFault(
                UnknownId,
                SimulatedFaultType.MotorOverheat);

            Assert.That(result, Is.EqualTo(FaultInjectionResult.UnknownDevice));
            Assert.That(source.ClearFault(UnknownId), Is.False);
            Assert.That(source.TryGetActiveFault(UnknownId, out _), Is.False);

            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MotorOverheat_RaisesOnlyTargetTemperatureAndKeepsSequence()
        {
            SimulationTelemetrySource source = CreateSource(
                CreateMotorProfile(MotorOneId),
                CreateMotorProfile(MotorTwoId));
            TelemetryFrame motorOneFrame = default;
            TelemetryFrame motorTwoFrame = default;
            source.FrameReceived += frame =>
            {
                if (frame.DeviceId == MotorOneId)
                {
                    motorOneFrame = frame;
                }
                else if (frame.DeviceId == MotorTwoId)
                {
                    motorTwoFrame = frame;
                }
            };
            source.Begin();
            long sequenceBeforeFault = motorOneFrame.Sequence;
            source.InjectFault(MotorOneId, SimulatedFaultType.MotorOverheat);

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(motorOneFrame.Sequence, Is.GreaterThan(sequenceBeforeFault));
            Assert.That(motorOneFrame.Sequence, Is.EqualTo(motorTwoFrame.Sequence));
            Assert.That(
                motorOneFrame.TemperatureCelsius,
                Is.GreaterThan(motorTwoFrame.TemperatureCelsius + 2.5f));

            source.End();
            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConveyorJam_ReducesRpmAndRaisesLoadOverTime()
        {
            SimulationTelemetrySource source = CreateSource(CreateConveyorProfile());
            TelemetryFrame latestFrame = default;
            source.FrameReceived += frame => latestFrame = frame;
            source.Begin();
            TelemetryFrame normalFrame = latestFrame;
            source.InjectFault(ConveyorId, SimulatedFaultType.ConveyorJam);

            yield return new WaitForSecondsRealtime(1.1f);

            Assert.That(latestFrame.Sequence, Is.GreaterThan(normalFrame.Sequence));
            Assert.That(latestFrame.Rpm, Is.LessThan(normalFrame.Rpm));
            Assert.That(latestFrame.LoadPercent, Is.GreaterThan(normalFrame.LoadPercent));

            source.End();
            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearFault_RestoresNormalTelemetryWithoutResettingSequence()
        {
            SimulationTelemetrySource source = CreateSource(
                CreateMotorProfile(MotorOneId),
                CreateMotorProfile(MotorTwoId));
            TelemetryFrame motorOneFrame = default;
            TelemetryFrame motorTwoFrame = default;
            source.FrameReceived += frame =>
            {
                if (frame.DeviceId == MotorOneId)
                {
                    motorOneFrame = frame;
                }
                else if (frame.DeviceId == MotorTwoId)
                {
                    motorTwoFrame = frame;
                }
            };
            source.Begin();
            source.InjectFault(MotorOneId, SimulatedFaultType.MotorOverheat);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(
                motorOneFrame.TemperatureCelsius,
                Is.GreaterThan(motorTwoFrame.TemperatureCelsius));

            long sequenceBeforeClear = motorOneFrame.Sequence;
            Assert.That(source.ClearFault(MotorOneId), Is.True);
            yield return new WaitForSecondsRealtime(0.6f);

            AssertActiveFault(source, MotorOneId, SimulatedFaultType.None);
            Assert.That(motorOneFrame.Sequence, Is.GreaterThan(sequenceBeforeClear));
            Assert.That(
                motorOneFrame.TemperatureCelsius,
                Is.EqualTo(motorTwoFrame.TemperatureCelsius).Within(0.001f));

            source.End();
            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FaultFrame_IsAppliedByDeviceRegistry()
        {
            SimulationDeviceProfile profile = CreateMotorProfile(MotorOneId);
            SimulationTelemetrySource source = CreateSource(profile);
            var state = new DeviceState(profile.Descriptor);
            var registry = new DeviceRegistry();
            registry.Register(state);
            TelemetryApplyResult lastResult = default;
            source.FrameReceived += frame => lastResult = registry.Apply(frame);
            source.Begin();
            source.InjectFault(MotorOneId, SimulatedFaultType.MotorOverheat);

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(lastResult, Is.EqualTo(TelemetryApplyResult.Applied));
            Assert.That(state.LastSequence, Is.GreaterThan(1));
            Assert.That(state.TemperatureCelsius, Is.GreaterThan(55f));

            source.End();
            UnityEngine.Object.Destroy(source.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Controller_InjectsDefaultFaultForSelectedDevice()
        {
            SimulationTelemetrySource source = CreateSource(
                CreateMotorProfile(MotorOneId),
                CreateConveyorProfile());
            var system = new GameObject("FaultController-Test");
            DeviceSelectionController selection =
                system.AddComponent<DeviceSelectionController>();
            FaultInjectionController controller =
                system.AddComponent<FaultInjectionController>();
            controller.Configure(selection, source);
            DeviceBinding motor = CreateBoundDevice(
                MotorOneId,
                DeviceKind.Motor,
                "Cooling Motor A");
            selection.Select(motor);

            FaultInjectionResult result =
                controller.InjectDefaultFaultForSelectedDevice();

            Assert.That(result, Is.EqualTo(FaultInjectionResult.Injected));
            AssertActiveFault(source, MotorOneId, SimulatedFaultType.MotorOverheat);

            UnityEngine.Object.Destroy(source.gameObject);
            UnityEngine.Object.Destroy(system);
            UnityEngine.Object.Destroy(motor.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase002Scene_PanelControlsFaultAndBlocksWorldSelection()
        {
            const string scenePath = "Assets/TwinTrace/Scenes/Phase002.unity";
            yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            yield return null;

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            DeviceBinding motor = FindSceneComponent<DeviceBinding>(scene, "MOTOR-001");
            DeviceSelectionController selection =
                FindSceneComponent<DeviceSelectionController>(scene, "TwinTraceSystem");
            SimulationTelemetrySource source =
                FindSceneComponent<SimulationTelemetrySource>(scene, "TwinTraceSystem");
            DeviceDetailsPanel panel = FindSceneComponent<DeviceDetailsPanel>(scene, "UI");
            selection.Select(motor);

            FaultInjectionResult result = panel.InjectFaultForSelection();
            Assert.That(result, Is.EqualTo(FaultInjectionResult.Injected));
            AssertActiveFault(source, MotorOneId, SimulatedFaultType.MotorOverheat);

            Vector2 panelClickPosition = new Vector2(
                Screen.width - 50f,
                Screen.height - 50f);
            bool worldSelected = selection.HandlePointerClick(panelClickPosition);

            Assert.That(worldSelected, Is.False);
            Assert.That(selection.Selected, Is.SameAs(motor));
            Assert.That(panel.ClearFaultForSelection(), Is.True);
            AssertActiveFault(source, MotorOneId, SimulatedFaultType.None);

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static SimulationTelemetrySource CreateSource(
            params SimulationDeviceProfile[] profiles)
        {
            var root = new GameObject("FaultSimulation-Test");
            SimulationTelemetrySource source =
                root.AddComponent<SimulationTelemetrySource>();
            source.Configure(profiles);
            return source;
        }

        private static SimulationDeviceProfile CreateMotorProfile(DeviceId id)
        {
            return new SimulationDeviceProfile(
                new DeviceDescriptor(id, DeviceKind.Motor, id.ToString()),
                55f,
                1450f,
                60f,
                0f);
        }

        private static SimulationDeviceProfile CreateConveyorProfile()
        {
            return new SimulationDeviceProfile(
                new DeviceDescriptor(ConveyorId, DeviceKind.Conveyor, "Main Conveyor"),
                42f,
                500f,
                50f,
                0f);
        }

        private static DeviceBinding CreateBoundDevice(
            DeviceId id,
            DeviceKind kind,
            string displayName)
        {
            var root = new GameObject(id + "-Fault-Test");
            DevicePresenter presenter = root.AddComponent<DevicePresenter>();
            DeviceBinding binding = root.AddComponent<DeviceBinding>();
            binding.Configure(id, presenter);
            binding.Bind(new DeviceState(new DeviceDescriptor(id, kind, displayName)));
            return binding;
        }

        private static void AssertActiveFault(
            SimulationTelemetrySource source,
            DeviceId id,
            SimulatedFaultType expected)
        {
            Assert.That(source.TryGetActiveFault(id, out SimulatedFaultType actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static T FindSceneComponent<T>(Scene scene, string gameObjectName)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == gameObjectName && root.TryGetComponent(out T component))
                {
                    return component;
                }
            }

            Assert.Fail(
                $"Scene '{scene.path}' does not contain '{gameObjectName}' with {typeof(T).Name}.");
            return null;
        }
    }
}
