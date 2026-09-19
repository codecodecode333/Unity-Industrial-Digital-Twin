using System;
using System.Collections;
using NUnit.Framework;
using TwinTrace.Alarms;
using TwinTrace.Domain;
using TwinTrace.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TwinTrace.Tests
{
    public sealed class AlarmPresentationTests
    {
        private static readonly DateTimeOffset CapturedAt =
            new DateTimeOffset(2026, 9, 20, 5, 0, 0, TimeSpan.Zero);

        [UnityTest]
        public IEnumerator AlarmPresenter_WarningActivatesAndNoneHidesIndicator()
        {
            var root = new GameObject("AlarmPresenter-Test");
            DeviceAlarmPresenter presenter = root.AddComponent<DeviceAlarmPresenter>();
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.transform.SetParent(root.transform, false);
            presenter.Configure(indicator.GetComponent<Renderer>());
            DeviceState state = CreateState("MOTOR-001");
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceAlarmState alarm = monitor.Register(state);
            presenter.Bind(alarm);

            Assert.That(indicator.activeSelf, Is.False);

            Apply(state, 1, 70f);
            Assert.That(indicator.activeSelf, Is.True);
            Assert.That(presenter.DisplayedSeverity, Is.EqualTo(AlarmSeverity.Warning));

            Apply(state, 2, 66f);
            Assert.That(indicator.activeSelf, Is.False);
            Assert.That(presenter.DisplayedSeverity, Is.EqualTo(AlarmSeverity.None));

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DetailsPanel_TracksSelectedAlarmAndUnsubscribesPreviousAlarm()
        {
            var panelObject = new GameObject("AlarmDetailsPanel-Test");
            panelObject.SetActive(false);
            DeviceDetailsPanel panel = panelObject.AddComponent<DeviceDetailsPanel>();
            DeviceState motorOne = CreateState("MOTOR-001");
            DeviceState motorTwo = CreateState("MOTOR-002");
            using var monitor = new AlarmMonitor(new AlarmEvaluator());
            DeviceAlarmState alarmOne = monitor.Register(motorOne);
            DeviceAlarmState alarmTwo = monitor.Register(motorTwo);

            panel.Bind(motorOne, alarmOne);
            Apply(motorOne, 1, 70f);

            Assert.That(panel.DisplayedAlarmSeverity, Is.EqualTo("WARNING"));
            Assert.That(panel.DisplayedAlarmCode, Is.EqualTo("Motor Overheat"));

            panel.Bind(motorTwo, alarmTwo);
            Apply(motorOne, 2, 82f);

            Assert.That(panel.BoundAlarmState, Is.SameAs(alarmTwo));
            Assert.That(panel.DisplayedAlarmSeverity, Is.EqualTo("NONE"));
            Assert.That(panel.DisplayedAlarmCode, Is.EqualTo("No Active Alarm"));

            UnityEngine.Object.Destroy(panelObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase002Scene_BindsAlarmStateAndHasSeparateIndicators()
        {
            const string scenePath = "Assets/TwinTrace/Scenes/Phase002.unity";
            yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            yield return null;

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            string[] deviceIds = { "MOTOR-001", "MOTOR-002", "CONVEYOR-001" };
            foreach (string deviceId in deviceIds)
            {
                DeviceBinding binding = FindSceneComponent<DeviceBinding>(scene, deviceId);

                Assert.That(binding.IsBound, Is.True, deviceId);
                Assert.That(binding.IsAlarmBound, Is.True, deviceId);
                Assert.That(binding.BoundAlarmState.DeviceId, Is.EqualTo(binding.Id), deviceId);
                Assert.That(binding.GetComponent<DeviceAlarmPresenter>(), Is.Not.Null, deviceId);
                Assert.That(binding.transform.Find("Status Indicator"), Is.Not.Null, deviceId);
                Assert.That(binding.transform.Find("Alarm Indicator"), Is.Not.Null, deviceId);
            }

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static DeviceState CreateState(string id)
        {
            return new DeviceState(new DeviceDescriptor(
                new DeviceId(id),
                DeviceKind.Motor,
                id));
        }

        private static void Apply(DeviceState state, long sequence, float temperature)
        {
            state.Apply(new TelemetryFrame(
                state.Id,
                sequence,
                CapturedAt.AddSeconds(sequence),
                temperature,
                1450f,
                60f));
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
