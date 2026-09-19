using System.Globalization;
using TwinTrace.Domain;
using TwinTrace.Interaction;
using TwinTrace.Telemetry;
using UnityEngine;
using UnityEngine.UIElements;

namespace TwinTrace.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class DeviceDetailsPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private DeviceSelectionController selectionController;
        [SerializeField] private FaultInjectionController faultInjectionController;

        private DeviceState _state;
        private bool _isListeningForSelection;
        private VisualElement _viewRoot;
        private VisualElement _panelRoot;
        private VisualElement _emptyState;
        private VisualElement _detailsContent;
        private VisualElement _faultControls;
        private Label _displayNameLabel;
        private Label _deviceIdLabel;
        private Label _deviceKindLabel;
        private Label _operationalStateLabel;
        private Label _temperatureLabel;
        private Label _rpmLabel;
        private Label _loadLabel;
        private Label _lastUpdateLabel;
        private Button _injectFaultButton;
        private Button _clearFaultButton;

        public DeviceState BoundState => _state;
        public string DisplayedName { get; private set; } = "No Device Selected";
        public string DisplayedDeviceId { get; private set; } = "—";
        public string DisplayedKind { get; private set; } = "—";
        public string DisplayedOperationalState { get; private set; } = "—";
        public string DisplayedTemperature { get; private set; } = "—";
        public string DisplayedRpm { get; private set; } = "—";
        public string DisplayedLoad { get; private set; } = "—";
        public string DisplayedLastUpdate { get; private set; } = "—";

        public void Configure(
            UIDocument uiDocument,
            StyleSheet panelStyleSheet,
            DeviceSelectionController controller,
            FaultInjectionController faultController = null)
        {
            StopListeningForSelection();
            UnregisterFaultButtonCallbacks();
            document = uiDocument;
            styleSheet = panelStyleSheet;
            selectionController = controller;
            faultInjectionController = faultController;
            _viewRoot = null;
            InitializeView();

            if (isActiveAndEnabled)
            {
                StartListeningForSelection();
                HandleSelectionChanged(selectionController?.Selected);
            }
        }

        public FaultInjectionResult InjectFaultForSelection()
        {
            return faultInjectionController == null
                ? FaultInjectionResult.UnknownDevice
                : faultInjectionController.InjectDefaultFaultForSelectedDevice();
        }

        public bool ClearFaultForSelection()
        {
            return faultInjectionController != null &&
                faultInjectionController.ClearFaultForSelectedDevice();
        }

        public bool ContainsScreenPosition(Vector2 screenPosition)
        {
            InitializeView();
            if (_panelRoot?.panel == null)
            {
                return false;
            }

            Vector2 topLeftScreenPosition = new Vector2(
                screenPosition.x,
                Screen.height - screenPosition.y);
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(
                _panelRoot.panel,
                topLeftScreenPosition);
            return _panelRoot.worldBound.Contains(panelPosition);
        }

        public void Bind(DeviceState state)
        {
            if (ReferenceEquals(_state, state))
            {
                Refresh(state);
                return;
            }

            UnbindState();
            if (state == null)
            {
                ShowEmptyState();
                return;
            }

            _state = state;
            _state.Changed += HandleStateChanged;
            Refresh(_state);
        }

        public void Clear()
        {
            UnbindState();
            ShowEmptyState();
        }

        private void OnEnable()
        {
            InitializeView();
            StartListeningForSelection();
            HandleSelectionChanged(selectionController?.Selected);
        }

        private void OnDisable()
        {
            StopListeningForSelection();
            Clear();
        }

        private void InitializeView()
        {
            document ??= GetComponent<UIDocument>();
            VisualElement root = document == null ? null : document.rootVisualElement;
            if (root == null || ReferenceEquals(root, _viewRoot))
            {
                return;
            }

            _viewRoot = root;
            if (styleSheet != null && !_viewRoot.styleSheets.Contains(styleSheet))
            {
                _viewRoot.styleSheets.Add(styleSheet);
            }

            _panelRoot = _viewRoot.Q<VisualElement>("details-panel");
            _emptyState = _viewRoot.Q<VisualElement>("empty-state");
            _detailsContent = _viewRoot.Q<VisualElement>("details-content");
            _faultControls = _viewRoot.Q<VisualElement>("fault-controls");
            _displayNameLabel = _viewRoot.Q<Label>("display-name");
            _deviceIdLabel = _viewRoot.Q<Label>("device-id");
            _deviceKindLabel = _viewRoot.Q<Label>("device-kind");
            _operationalStateLabel = _viewRoot.Q<Label>("operational-state");
            _temperatureLabel = _viewRoot.Q<Label>("temperature-value");
            _rpmLabel = _viewRoot.Q<Label>("rpm-value");
            _loadLabel = _viewRoot.Q<Label>("load-value");
            _lastUpdateLabel = _viewRoot.Q<Label>("last-update-value");
            _injectFaultButton = _viewRoot.Q<Button>("inject-fault-button");
            _clearFaultButton = _viewRoot.Q<Button>("clear-fault-button");
            RegisterFaultButtonCallbacks();
            UpdateView();
        }

        private void StartListeningForSelection()
        {
            if (_isListeningForSelection || selectionController == null)
            {
                return;
            }

            selectionController.SelectionChanged += HandleSelectionChanged;
            _isListeningForSelection = true;
        }

        private void StopListeningForSelection()
        {
            if (!_isListeningForSelection || selectionController == null)
            {
                return;
            }

            selectionController.SelectionChanged -= HandleSelectionChanged;
            _isListeningForSelection = false;
        }

        private void HandleSelectionChanged(DeviceBinding binding)
        {
            Bind(binding != null && binding.IsBound ? binding.BoundState : null);
        }

        private void HandleStateChanged(DeviceState state)
        {
            Refresh(state);
        }

        private void Refresh(DeviceState state)
        {
            if (state == null)
            {
                ShowEmptyState();
                return;
            }

            DisplayedName = state.Descriptor.DisplayName;
            DisplayedDeviceId = state.Id.ToString();
            DisplayedKind = state.Descriptor.Kind.ToString();
            DisplayedOperationalState = state.OperationalState.ToString().ToUpperInvariant();
            DisplayedTemperature = string.Format(
                CultureInfo.InvariantCulture,
                "{0:F1} °C",
                state.TemperatureCelsius);
            DisplayedRpm = string.Format(
                CultureInfo.InvariantCulture,
                "{0:N0}",
                state.Rpm);
            DisplayedLoad = string.Format(
                CultureInfo.InvariantCulture,
                "{0:F1} %",
                state.LoadPercent);
            DisplayedLastUpdate = state.LastTelemetryAtUtc == default
                ? "No telemetry"
                : state.LastTelemetryAtUtc.ToLocalTime().ToString(
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture);
            UpdateView();
        }

        private void UnbindState()
        {
            if (_state == null)
            {
                return;
            }

            _state.Changed -= HandleStateChanged;
            _state = null;
        }

        private void ShowEmptyState()
        {
            DisplayedName = "No Device Selected";
            DisplayedDeviceId = "—";
            DisplayedKind = "—";
            DisplayedOperationalState = "—";
            DisplayedTemperature = "—";
            DisplayedRpm = "—";
            DisplayedLoad = "—";
            DisplayedLastUpdate = "—";
            UpdateView();
        }

        private void UpdateView()
        {
            bool hasSelection = _state != null;
            if (_emptyState != null)
            {
                _emptyState.style.display = hasSelection
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            if (_detailsContent != null)
            {
                _detailsContent.style.display = hasSelection
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            SetText(_displayNameLabel, DisplayedName);
            SetText(_deviceIdLabel, DisplayedDeviceId);
            SetText(_deviceKindLabel, DisplayedKind);
            SetText(_operationalStateLabel, DisplayedOperationalState);
            SetText(_temperatureLabel, DisplayedTemperature);
            SetText(_rpmLabel, DisplayedRpm);
            SetText(_loadLabel, DisplayedLoad);
            SetText(_lastUpdateLabel, DisplayedLastUpdate);
            UpdateFaultControls();
        }

        private void UpdateFaultControls()
        {
            bool canControlFault = _state != null && faultInjectionController != null;
            if (_faultControls != null)
            {
                _faultControls.style.display = canControlFault
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_injectFaultButton == null || _state == null)
            {
                return;
            }

            _injectFaultButton.text = _state.Descriptor.Kind == DeviceKind.Motor
                ? "Inject Motor Overheat"
                : "Inject Conveyor Jam";
        }

        private void RegisterFaultButtonCallbacks()
        {
            if (_injectFaultButton != null)
            {
                _injectFaultButton.clicked += HandleInjectFaultClicked;
            }

            if (_clearFaultButton != null)
            {
                _clearFaultButton.clicked += HandleClearFaultClicked;
            }
        }

        private void UnregisterFaultButtonCallbacks()
        {
            if (_injectFaultButton != null)
            {
                _injectFaultButton.clicked -= HandleInjectFaultClicked;
            }

            if (_clearFaultButton != null)
            {
                _clearFaultButton.clicked -= HandleClearFaultClicked;
            }

            _injectFaultButton = null;
            _clearFaultButton = null;
        }

        private void HandleInjectFaultClicked()
        {
            InjectFaultForSelection();
        }

        private void HandleClearFaultClicked()
        {
            ClearFaultForSelection();
        }

        private static void SetText(Label label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
