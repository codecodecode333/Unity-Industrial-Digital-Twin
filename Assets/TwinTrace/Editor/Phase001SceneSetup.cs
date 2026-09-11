using System.IO;
using TwinTrace.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwinTrace.EditorTools
{
    public static class Phase001SceneSetup
    {
        private const string SceneDirectory = "Assets/TwinTrace/Scenes";
        private const string ScenePath = SceneDirectory + "/Phase001.unity";

        [MenuItem("TwinTrace/Create Phase 001 Demo Scene")]
        public static void CreateDemoScene()
        {
            Directory.CreateDirectory(SceneDirectory);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);

            GameObject motor = new GameObject("MOTOR-001");
            motor.AddComponent<TwinTraceBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Selection.activeGameObject = motor;
            Debug.Log($"Created TwinTrace Phase 001 demo scene at '{ScenePath}'.");
        }
    }
}
