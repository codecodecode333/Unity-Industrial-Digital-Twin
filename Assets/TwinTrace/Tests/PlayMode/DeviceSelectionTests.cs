using System;
using System.Collections;
using NUnit.Framework;
using TwinTrace.Domain;
using TwinTrace.Interaction;
using TwinTrace.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TwinTrace.Tests
{
    public sealed class DeviceSelectionTests
    {
        private static readonly DeviceId MotorOneId = new DeviceId("MOTOR-001");
        private static readonly DeviceId MotorTwoId = new DeviceId("MOTOR-002");

        [UnityTest]
        public IEnumerator Select_SetsSelectedDeviceAndVisual()
        {
            DeviceSelectionController controller = CreateController();
            DeviceBinding motor = CreateBinding("MOTOR-001", out DeviceSelectionVisual visual);

            controller.Select(motor);

            Assert.That(controller.Selected, Is.SameAs(motor));
            Assert.That(visual.IsSelected, Is.True);

            DestroyAll(controller.gameObject, motor.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingAnotherDevice_SwitchesSelectionVisuals()
        {
            DeviceSelectionController controller = CreateController();
            DeviceBinding motorOne = CreateBinding("MOTOR-001", out DeviceSelectionVisual visualOne);
            DeviceBinding motorTwo = CreateBinding("MOTOR-002", out DeviceSelectionVisual visualTwo);

            controller.Select(motorOne);
            controller.Select(motorTwo);

            Assert.That(controller.Selected, Is.SameAs(motorTwo));
            Assert.That(visualOne.IsSelected, Is.False);
            Assert.That(visualTwo.IsSelected, Is.True);

            DestroyAll(controller.gameObject, motorOne.gameObject, motorTwo.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearSelection_RemovesSelectionAndVisual()
        {
            DeviceSelectionController controller = CreateController();
            DeviceBinding motor = CreateBinding("MOTOR-001", out DeviceSelectionVisual visual);
            controller.Select(motor);

            controller.ClearSelection();

            Assert.That(controller.Selected, Is.Null);
            Assert.That(visual.IsSelected, Is.False);

            DestroyAll(controller.gameObject, motor.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResolveBinding_FindsBindingFromChildCollider()
        {
            DeviceBinding motor = CreateBinding("MOTOR-001", out _);
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(motor.transform, false);

            DeviceBinding resolved = DeviceSelectionController.ResolveBinding(
                body.GetComponent<Collider>());

            Assert.That(resolved, Is.SameAs(motor));

            UnityEngine.Object.Destroy(motor.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DetailsPanel_RefreshesWhenBoundStateChanges()
        {
            DeviceDetailsPanel panel = CreatePanel();
            DeviceState motor = CreateState(MotorOneId, "Cooling Motor A");
            panel.Bind(motor);

            motor.Apply(CreateFrame(MotorOneId, 1, 56.4f, 1472f, 63.2f));

            Assert.That(panel.DisplayedName, Is.EqualTo("Cooling Motor A"));
            Assert.That(panel.DisplayedDeviceId, Is.EqualTo("MOTOR-001"));
            Assert.That(panel.DisplayedKind, Is.EqualTo("Motor"));
            Assert.That(panel.DisplayedOperationalState, Is.EqualTo("RUNNING"));
            Assert.That(panel.DisplayedTemperature, Is.EqualTo("56.4 °C"));
            Assert.That(panel.DisplayedRpm, Is.EqualTo("1,472"));
            Assert.That(panel.DisplayedLoad, Is.EqualTo("63.2 %"));
            Assert.That(panel.DisplayedLastUpdate, Is.Not.EqualTo("—"));

            UnityEngine.Object.Destroy(panel.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DetailsPanel_SwitchingStateIgnoresPreviousStateChanges()
        {
            DeviceDetailsPanel panel = CreatePanel();
            DeviceState motorOne = CreateState(MotorOneId, "Cooling Motor A");
            DeviceState motorTwo = CreateState(MotorTwoId, "Cooling Motor B");
            panel.Bind(motorOne);
            panel.Bind(motorTwo);

            motorOne.Apply(CreateFrame(MotorOneId, 1, 60f, 1600f, 70f));

            Assert.That(panel.BoundState, Is.SameAs(motorTwo));
            Assert.That(panel.DisplayedName, Is.EqualTo("Cooling Motor B"));
            Assert.That(panel.DisplayedDeviceId, Is.EqualTo("MOTOR-002"));
            Assert.That(panel.DisplayedOperationalState, Is.EqualTo("OFFLINE"));

            UnityEngine.Object.Destroy(panel.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DetailsPanel_ClearIgnoresPreviousStateChanges()
        {
            DeviceDetailsPanel panel = CreatePanel();
            DeviceState motor = CreateState(MotorOneId, "Cooling Motor A");
            panel.Bind(motor);
            panel.Clear();

            motor.Apply(CreateFrame(MotorOneId, 1, 60f, 1600f, 70f));

            Assert.That(panel.BoundState, Is.Null);
            Assert.That(panel.DisplayedName, Is.EqualTo("No Device Selected"));
            Assert.That(panel.DisplayedDeviceId, Is.EqualTo("—"));

            UnityEngine.Object.Destroy(panel.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase002Scene_RaycastSelectsBoundDeviceAndUpdatesPanel()
        {
            const string scenePath = "Assets/TwinTrace/Scenes/Phase002.unity";
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                scenePath,
                LoadSceneMode.Additive);
            yield return loadOperation;
            yield return null;

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            DeviceBinding motor = FindSceneComponent<DeviceBinding>(scene, "MOTOR-001");
            DeviceSelectionController controller =
                FindSceneComponent<DeviceSelectionController>(scene, "TwinTraceSystem");
            DeviceDetailsPanel panel = FindSceneComponent<DeviceDetailsPanel>(scene, "UI");
            Camera camera = FindSceneComponent<Camera>(scene, "Main Camera");
            Transform motorBody = motor.transform.Find("Motor Body");

            Assert.That(motor.IsBound, Is.True);
            Assert.That(motorBody, Is.Not.Null);
            Physics.SyncTransforms();
            Vector3 screenPosition = camera.WorldToScreenPoint(motorBody.position);

            bool selected = controller.SelectAtScreenPosition(screenPosition);
            yield return null;

            Assert.That(selected, Is.True);
            Assert.That(controller.Selected, Is.SameAs(motor));
            Assert.That(
                motor.GetComponent<DeviceSelectionVisual>().IsSelected,
                Is.True);
            Assert.That(panel.BoundState, Is.SameAs(motor.BoundState));
            Assert.That(panel.DisplayedName, Is.EqualTo("Cooling Motor A"));
            Assert.That(panel.DisplayedDeviceId, Is.EqualTo("MOTOR-001"));

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static DeviceSelectionController CreateController()
        {
            var root = new GameObject("SelectionController-Test");
            return root.AddComponent<DeviceSelectionController>();
        }

        private static DeviceBinding CreateBinding(
            string id,
            out DeviceSelectionVisual selectionVisual)
        {
            var root = new GameObject(id + "-Selection-Test");
            DevicePresenter presenter = root.AddComponent<DevicePresenter>();
            DeviceBinding binding = root.AddComponent<DeviceBinding>();
            binding.Configure(new DeviceId(id), presenter);

            var marker = new GameObject("Selection Marker");
            marker.transform.SetParent(root.transform, false);
            selectionVisual = root.AddComponent<DeviceSelectionVisual>();
            selectionVisual.Configure(marker);
            return binding;
        }

        private static DeviceDetailsPanel CreatePanel()
        {
            var root = new GameObject("DeviceDetailsPanel-Test");
            root.SetActive(false);
            return root.AddComponent<DeviceDetailsPanel>();
        }

        private static DeviceState CreateState(DeviceId id, string displayName)
        {
            return new DeviceState(new DeviceDescriptor(id, DeviceKind.Motor, displayName));
        }

        private static TelemetryFrame CreateFrame(
            DeviceId id,
            long sequence,
            float temperature,
            float rpm,
            float load)
        {
            return new TelemetryFrame(
                id,
                sequence,
                new DateTimeOffset(2026, 9, 17, 10, 20, 30, TimeSpan.Zero),
                temperature,
                rpm,
                load);
        }

        private static void DestroyAll(params GameObject[] gameObjects)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
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
