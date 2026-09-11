using System.Collections;
using NUnit.Framework;
using TwinTrace.Composition;
using TwinTrace.Domain;
using TwinTrace.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TwinTrace.Tests
{
    public sealed class TelemetryFlowTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_UpdatesPresenterFromSimulatedTelemetry()
        {
            GameObject motor = new GameObject("MOTOR-001-Test");
            motor.AddComponent<TwinTraceBootstrap>();

            yield return null;

            DevicePresenter presenter = motor.GetComponent<DevicePresenter>();
            Assert.That(presenter.DisplayedDeviceId, Is.EqualTo("MOTOR-001"));
            Assert.That(presenter.DisplayedSequence, Is.GreaterThan(0));
            Assert.That(
                presenter.DisplayedOperationalState,
                Is.EqualTo(DeviceOperationalState.Running));

            long firstSequence = presenter.DisplayedSequence;
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(presenter.DisplayedSequence, Is.GreaterThan(firstSequence));

            Object.Destroy(motor);
            yield return null;
        }
    }
}
