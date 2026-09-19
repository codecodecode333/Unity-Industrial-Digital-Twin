using System;
using TwinTrace.Alarms;
using UnityEngine;

namespace TwinTrace.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DeviceAlarmPresenter : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer alarmRenderer;
        [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0.05f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.05f, 0.03f);

        [Header("Runtime State (Read Only)")]
        [SerializeField] private string alarmSeverity = AlarmSeverity.None.ToString();
        [SerializeField] private string alarmCode = AlarmCode.None.ToString();

        private DeviceAlarmState _alarmState;
        private MaterialPropertyBlock _propertyBlock;

        public bool IsBound => _alarmState != null;
        public AlarmSeverity DisplayedSeverity =>
            _alarmState == null ? AlarmSeverity.None : _alarmState.Severity;
        public AlarmCode DisplayedCode =>
            _alarmState == null ? AlarmCode.None : _alarmState.Code;

        public void Configure(Renderer deviceAlarmRenderer)
        {
            alarmRenderer = deviceAlarmRenderer
                ?? throw new ArgumentNullException(nameof(deviceAlarmRenderer));
            Refresh(_alarmState);
        }

        public void Bind(DeviceAlarmState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Unbind();
            _alarmState = state;
            _alarmState.Changed += HandleAlarmChanged;
            Refresh(_alarmState);
        }

        public void Unbind()
        {
            if (_alarmState != null)
            {
                _alarmState.Changed -= HandleAlarmChanged;
                _alarmState = null;
            }

            alarmSeverity = AlarmSeverity.None.ToString();
            alarmCode = AlarmCode.None.ToString();
            SetIndicatorVisible(false);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void HandleAlarmChanged(DeviceAlarmState state)
        {
            Refresh(state);
        }

        private void Refresh(DeviceAlarmState state)
        {
            AlarmSeverity severity = state?.Severity ?? AlarmSeverity.None;
            alarmSeverity = severity.ToString();
            alarmCode = (state?.Code ?? AlarmCode.None).ToString();

            bool isVisible = severity != AlarmSeverity.None;
            SetIndicatorVisible(isVisible);
            if (isVisible)
            {
                ApplyIndicatorColor(
                    severity == AlarmSeverity.Critical ? criticalColor : warningColor);
            }
        }

        private void SetIndicatorVisible(bool isVisible)
        {
            if (alarmRenderer != null)
            {
                alarmRenderer.gameObject.SetActive(isVisible);
            }
        }

        private void ApplyIndicatorColor(Color color)
        {
            if (alarmRenderer == null || alarmRenderer.sharedMaterial == null)
            {
                return;
            }

            int propertyId;
            if (alarmRenderer.sharedMaterial.HasProperty(BaseColorProperty))
            {
                propertyId = BaseColorProperty;
            }
            else if (alarmRenderer.sharedMaterial.HasProperty(ColorProperty))
            {
                propertyId = ColorProperty;
            }
            else
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            alarmRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(propertyId, color);
            alarmRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
