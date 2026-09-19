using System.IO;
using TwinTrace.Composition;
using TwinTrace.Domain;
using TwinTrace.Interaction;
using TwinTrace.Presentation;
using TwinTrace.Telemetry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TwinTrace.EditorTools
{
    public static class Phase002SceneSetup
    {
        private const string SceneDirectory = "Assets/TwinTrace/Scenes";
        private const string ScenePath = SceneDirectory + "/Phase002.unity";
        private const string UiDirectory = "Assets/TwinTrace/UI";
        private const string PanelSettingsPath = UiDirectory + "/TwinTracePanelSettings.asset";
        private const string DetailsLayoutPath = UiDirectory + "/DeviceDetailsPanel.uxml";
        private const string DetailsStylePath = UiDirectory + "/DeviceDetailsPanel.uss";

        [MenuItem("TwinTrace/Create Phase 002 Demo Scene")]
        public static void CreateDemoScene()
        {
            Directory.CreateDirectory(SceneDirectory);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);

            ConfigureEnvironment();
            CreateFloor();

            GameObject system = new GameObject("TwinTraceSystem");
            SimulationTelemetrySource simulationSource =
                system.AddComponent<SimulationTelemetrySource>();
            system.AddComponent<TwinTraceBootstrap>();
            DeviceSelectionController selectionController =
                system.AddComponent<DeviceSelectionController>();
            selectionController.Configure(Camera.main);
            FaultInjectionController faultInjectionController =
                system.AddComponent<FaultInjectionController>();
            faultInjectionController.Configure(selectionController, simulationSource);

            CreateMotor("MOTOR-001", new Vector3(-3.5f, 0f, 0f));
            CreateMotor("MOTOR-002", new Vector3(-0.8f, 0f, 0f));
            CreateConveyor("CONVEYOR-001", new Vector3(3f, 0f, 0f));
            DeviceDetailsPanel detailsPanel = CreateDetailsPanel(
                selectionController,
                faultInjectionController);
            selectionController.ConfigureInputBlocker(detailsPanel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Selection.activeGameObject = system;
            Debug.Log($"Created TwinTrace Phase 002 demo scene at '{ScenePath}'.");
        }

        private static void ConfigureEnvironment()
        {
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(0f, 6.5f, -12f);
            camera.transform.LookAt(new Vector3(0f, 0.8f, 0f));
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);

            Light light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }
        }

        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.5f, 8f);
        }

        private static void CreateMotor(string id, Vector3 position)
        {
            GameObject root = CreateDeviceRoot(
                id,
                position,
                out DevicePresenter presenter,
                out DeviceAlarmPresenter alarmPresenter);

            GameObject body = CreateChildPrimitive(root, "Motor Body", PrimitiveType.Cube);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.transform.localScale = new Vector3(1.8f, 1.2f, 1.2f);

            GameObject rotor = CreateChildPrimitive(root, "Rotor", PrimitiveType.Cylinder);
            rotor.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            rotor.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rotor.transform.localScale = new Vector3(0.42f, 1.2f, 0.42f);

            Renderer indicator = CreateStatusIndicator(root, new Vector3(-0.35f, 1.7f, 0f));
            Renderer alarmIndicator = CreateAlarmIndicator(
                root,
                new Vector3(0.35f, 1.7f, 0f));
            presenter.ConfigureVisuals(
                indicator,
                new[] { rotor.transform },
                Vector3.up,
                0.05f);
            alarmPresenter.Configure(alarmIndicator);
            CreateSelectionMarker(root, new Vector3(1.25f, 0.03f, 1.25f));
        }

        private static void CreateConveyor(string id, Vector3 position)
        {
            GameObject root = CreateDeviceRoot(
                id,
                position,
                out DevicePresenter presenter,
                out DeviceAlarmPresenter alarmPresenter);

            GameObject body = CreateChildPrimitive(root, "Conveyor Body", PrimitiveType.Cube);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(4f, 0.5f, 1.6f);

            var rollers = new Transform[3];
            for (int index = 0; index < rollers.Length; index++)
            {
                GameObject roller = CreateChildPrimitive(
                    root,
                    $"Roller {index + 1}",
                    PrimitiveType.Cylinder);
                roller.transform.localPosition = new Vector3(-1.3f + index * 1.3f, 0.95f, 0f);
                roller.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                roller.transform.localScale = new Vector3(0.28f, 0.75f, 0.28f);
                rollers[index] = roller.transform;
            }

            Renderer indicator = CreateStatusIndicator(root, new Vector3(-0.35f, 1.65f, 0f));
            Renderer alarmIndicator = CreateAlarmIndicator(
                root,
                new Vector3(0.35f, 1.65f, 0f));
            presenter.ConfigureVisuals(indicator, rollers, Vector3.up, 0.05f);
            alarmPresenter.Configure(alarmIndicator);
            CreateSelectionMarker(root, new Vector3(2.4f, 0.03f, 1.15f));
        }

        private static GameObject CreateDeviceRoot(
            string id,
            Vector3 position,
            out DevicePresenter presenter,
            out DeviceAlarmPresenter alarmPresenter)
        {
            var root = new GameObject(id);
            root.transform.position = position;
            presenter = root.AddComponent<DevicePresenter>();
            alarmPresenter = root.AddComponent<DeviceAlarmPresenter>();
            DeviceBinding binding = root.AddComponent<DeviceBinding>();
            binding.Configure(new DeviceId(id), presenter, alarmPresenter);
            return root;
        }

        private static GameObject CreateChildPrimitive(
            GameObject parent,
            string name,
            PrimitiveType primitiveType)
        {
            GameObject child = GameObject.CreatePrimitive(primitiveType);
            child.name = name;
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static Renderer CreateStatusIndicator(GameObject parent, Vector3 localPosition)
        {
            GameObject indicator = CreateChildPrimitive(
                parent,
                "Status Indicator",
                PrimitiveType.Sphere);
            indicator.transform.localPosition = localPosition;
            indicator.transform.localScale = Vector3.one * 0.35f;
            return indicator.GetComponent<Renderer>();
        }

        private static Renderer CreateAlarmIndicator(GameObject parent, Vector3 localPosition)
        {
            GameObject indicator = CreateChildPrimitive(
                parent,
                "Alarm Indicator",
                PrimitiveType.Sphere);
            indicator.transform.localPosition = localPosition;
            indicator.transform.localScale = Vector3.one * 0.28f;
            return indicator.GetComponent<Renderer>();
        }

        private static void CreateSelectionMarker(GameObject parent, Vector3 localScale)
        {
            GameObject marker = CreateChildPrimitive(
                parent,
                "Selection Marker",
                PrimitiveType.Cylinder);
            marker.transform.SetAsFirstSibling();
            marker.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            marker.transform.localScale = localScale;

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Object.DestroyImmediate(markerCollider);
            }

            DeviceSelectionVisual selectionVisual =
                parent.AddComponent<DeviceSelectionVisual>();
            selectionVisual.Configure(marker);
        }

        private static DeviceDetailsPanel CreateDetailsPanel(
            DeviceSelectionController selectionController,
            FaultInjectionController faultInjectionController)
        {
            PanelSettings panelSettings = GetOrCreatePanelSettings();
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                DetailsLayoutPath);
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(DetailsStylePath);

            if (layout == null || styleSheet == null)
            {
                throw new FileNotFoundException(
                    "The Device Details UI Toolkit assets could not be loaded.");
            }

            var ui = new GameObject("UI");
            UIDocument document = ui.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = layout;
            document.sortingOrder = 10;

            DeviceDetailsPanel panel = ui.AddComponent<DeviceDetailsPanel>();
            panel.Configure(
                document,
                styleSheet,
                selectionController,
                faultInjectionController);
            return panel;
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            Directory.CreateDirectory(UiDirectory);
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
                PanelSettingsPath);
            if (panelSettings != null)
            {
                return panelSettings;
            }

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            return panelSettings;
        }
    }
}
