using System;
using TwinTrace.Domain;
using UnityEngine;

namespace TwinTrace.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DevicePresenter))]
    public sealed class DeviceBinding : MonoBehaviour
    {
        [SerializeField] private string deviceId = string.Empty;
        [SerializeField] private DevicePresenter presenter;

        public DeviceId Id => new DeviceId(deviceId);
        public DeviceState BoundState { get; private set; }
        public DevicePresenter Presenter => presenter;
        public bool IsBound => BoundState != null;

        public void Configure(DeviceId id, DevicePresenter devicePresenter)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Device binding requires a valid ID.", nameof(id));
            }

            deviceId = id.ToString();
            presenter = devicePresenter ?? throw new ArgumentNullException(nameof(devicePresenter));
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

        public void Unbind()
        {
            presenter?.Unbind();
            BoundState = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Reset()
        {
            presenter = GetComponent<DevicePresenter>();
        }
    }
}
