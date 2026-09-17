using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TwinTrace.Composition;
using TwinTrace.Domain;
using TwinTrace.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TwinTrace.Tests
{
    public sealed class DevicePresentationTests
    {
        private static readonly DeviceId MotorOneId = new DeviceId("MOTOR-001");
        private static readonly DeviceId MotorTwoId = new DeviceId("MOTOR-002");
        private static readonly DeviceId ConveyorId = new DeviceId("CONVEYOR-001");

        [UnityTest]
        public IEnumerator Binding_ConnectsMatchingDeviceState()
        {
            DeviceBinding binding = CreateBinding("MOTOR-001", out _);
            DeviceState state = CreateState(MotorOneId, DeviceKind.Motor);

            binding.Bind(state);

            Assert.That(binding.Id, Is.EqualTo(MotorOneId));
            Assert.That(binding.BoundState, Is.SameAs(state));
            Assert.That(binding.IsBound, Is.True);

            Object.Destroy(binding.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bootstrap_BindsEachSceneDeviceToMatchingState()
        {
            DeviceBinding motorOne = CreateBinding("MOTOR-001", out _);
            DeviceBinding motorTwo = CreateBinding("MOTOR-002", out _);
            DeviceBinding conveyor = CreateBinding("CONVEYOR-001", out _);
            GameObject system = CreateSystem();

            yield return null;

            Assert.That(motorOne.BoundState.Id, Is.EqualTo(MotorOneId));
            Assert.That(motorTwo.BoundState.Id, Is.EqualTo(MotorTwoId));
            Assert.That(conveyor.BoundState.Id, Is.EqualTo(ConveyorId));
            Assert.That(motorOne.BoundState, Is.Not.SameAs(motorTwo.BoundState));
            Assert.That(motorTwo.BoundState, Is.Not.SameAs(conveyor.BoundState));

            DestroyAll(system, motorOne.gameObject, motorTwo.gameObject, conveyor.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StateChange_UpdatesOnlyItsBoundPresenter()
        {
            DeviceBinding motorOne = CreateBinding("MOTOR-001", out DevicePresenter presenterOne);
            DeviceBinding motorTwo = CreateBinding("MOTOR-002", out DevicePresenter presenterTwo);
            DeviceState stateOne = CreateState(MotorOneId, DeviceKind.Motor);
            DeviceState stateTwo = CreateState(MotorTwoId, DeviceKind.Motor);
            motorOne.Bind(stateOne);
            motorTwo.Bind(stateTwo);

            stateOne.Apply(CreateFrame(MotorOneId, 1, 1450f));

            Assert.That(presenterOne.DisplayedDeviceId, Is.EqualTo("MOTOR-001"));
            Assert.That(presenterOne.DisplayedSequence, Is.EqualTo(1));
            Assert.That(
                presenterOne.DisplayedOperationalState,
                Is.EqualTo(DeviceOperationalState.Running));
            Assert.That(presenterTwo.DisplayedSequence, Is.Zero);
            Assert.That(
                presenterTwo.DisplayedOperationalState,
                Is.EqualTo(DeviceOperationalState.Offline));

            DestroyAll(motorOne.gameObject, motorTwo.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnknownBinding_DoesNotPreventValidBinding()
        {
            DeviceBinding valid = CreateBinding("MOTOR-001", out _);
            DeviceBinding unknown = CreateBinding("MOTOR-999", out _);
            LogAssert.Expect(
                LogType.Warning,
                new Regex("No registered device matches binding 'MOTOR-999'"));

            GameObject system = CreateSystem();
            yield return null;

            Assert.That(valid.IsBound, Is.True);
            Assert.That(valid.BoundState.Id, Is.EqualTo(MotorOneId));
            Assert.That(unknown.IsBound, Is.False);

            DestroyAll(system, valid.gameObject, unknown.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Presenter_UnbindStopsPreviousStateUpdates()
        {
            GameObject root = new GameObject("Presenter-Unbind-Test");
            DevicePresenter presenter = root.AddComponent<DevicePresenter>();
            DeviceState state = CreateState(MotorOneId, DeviceKind.Motor);
            presenter.Bind(state);
            state.Apply(CreateFrame(MotorOneId, 1, 1450f));

            presenter.Unbind();
            state.Apply(CreateFrame(MotorOneId, 2, 1500f));

            Assert.That(presenter.IsBound, Is.False);
            Assert.That(presenter.DisplayedSequence, Is.Zero);
            Assert.That(
                presenter.DisplayedOperationalState,
                Is.EqualTo(DeviceOperationalState.Offline));

            Object.Destroy(root);
            yield return null;
        }

        private static DeviceBinding CreateBinding(
            string id,
            out DevicePresenter presenter)
        {
            var root = new GameObject(id + "-Binding-Test");
            presenter = root.AddComponent<DevicePresenter>();
            DeviceBinding binding = root.AddComponent<DeviceBinding>();
            binding.Configure(new DeviceId(id), presenter);
            return binding;
        }

        private static GameObject CreateSystem()
        {
            var system = new GameObject("TwinTraceSystem-Test");
            system.AddComponent<TwinTraceBootstrap>();
            return system;
        }

        private static DeviceState CreateState(DeviceId id, DeviceKind kind)
        {
            return new DeviceState(new DeviceDescriptor(id, kind, id.ToString()));
        }

        private static TelemetryFrame CreateFrame(DeviceId id, long sequence, float rpm)
        {
            return new TelemetryFrame(
                id,
                sequence,
                System.DateTimeOffset.UtcNow,
                55f,
                rpm,
                60f);
        }

        private static void DestroyAll(params GameObject[] gameObjects)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                Object.Destroy(gameObject);
            }
        }
    }
}
