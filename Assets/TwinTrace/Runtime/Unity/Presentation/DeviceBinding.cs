using System;
using TwinTrace.Alarms;
using TwinTrace.Domain;
using UnityEngine;

namespace TwinTrace.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DevicePresenter))]
    [RequireComponent(typeof(DeviceAlarmPresenter))]
    public sealed class DeviceBinding : MonoBehaviour
    {
        [SerializeField] private string deviceId = string.Empty;
        [SerializeField] private DevicePresenter presenter;
        [SerializeField] private DeviceAlarmPresenter alarmPresenter;

        public DeviceId Id => new DeviceId(deviceId);
        public DeviceState BoundState { get; private set; }
        public DeviceAlarmState BoundAlarmState { get; private set; }
        public DevicePresenter Presenter => presenter;
        public DeviceAlarmPresenter AlarmPresenter => alarmPresenter;
        public bool IsBound => BoundState != null;
        public bool IsAlarmBound => BoundAlarmState != null;

        public void Configure(
            DeviceId id,
            DevicePresenter devicePresenter,
            DeviceAlarmPresenter deviceAlarmPresenter = null)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Device binding requires a valid ID.", nameof(id));
            }

            deviceId = id.ToString();
            presenter = devicePresenter ?? throw new ArgumentNullException(nameof(devicePresenter));
            alarmPresenter = deviceAlarmPresenter;
        }

        public bool TryGetId(out DeviceId id)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                id = default;
                return false;
            }

            id = new DeviceId(deviceId);
            return true;
        }

        public void Bind(DeviceState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!TryGetId(out DeviceId id) || state.Id != id)
            {
                throw new InvalidOperationException(
                    $"Cannot bind device '{state.Id}' to scene binding '{deviceId}'.");
            }

            presenter ??= GetComponent<DevicePresenter>();
            if (presenter == null)
            {
                throw new InvalidOperationException("A DevicePresenter component is required.");
            }

            Unbind();
            BoundState = state;
            presenter.Bind(state);
        }

        public void Bind(DeviceState state, DeviceAlarmState alarmState)
        {
            Bind(state);
            BindAlarm(alarmState);
        }

        public void BindAlarm(DeviceAlarmState alarmState)
        {
            if (alarmState == null)
            {
                throw new ArgumentNullException(nameof(alarmState));
            }

            if (!TryGetId(out DeviceId id) || alarmState.DeviceId != id)
            {
                throw new InvalidOperationException(
                    $"Cannot bind alarm '{alarmState.DeviceId}' to scene binding '{deviceId}'.");
            }

            alarmPresenter ??= GetComponent<DeviceAlarmPresenter>();
            if (alarmPresenter == null)
            {
                throw new InvalidOperationException("A DeviceAlarmPresenter component is required.");
            }

            UnbindAlarm();
            BoundAlarmState = alarmState;
            alarmPresenter.Bind(alarmState);
        }

        public void Unbind()
        {
            presenter?.Unbind();
            BoundState = null;
            UnbindAlarm();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Reset()
        {
            presenter = GetComponent<DevicePresenter>();
            alarmPresenter = GetComponent<DeviceAlarmPresenter>();
        }

        private void UnbindAlarm()
        {
            alarmPresenter?.Unbind();
            BoundAlarmState = null;
        }
    }
}
