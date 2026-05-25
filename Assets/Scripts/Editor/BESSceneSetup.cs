#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to create MainMenu scene and configure build settings.
/// </summary>
public static class BESSceneSetup
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("BES/Setup Scenes And Build Settings")]
    public static void SetupScenes()
    {
        CreateMainMenuScene();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[BES] MainMenu scene created and build settings updated.");
    }

    static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject menuRoot = new GameObject("BES_MainMenu");
        menuRoot.AddComponent<MainMenuUI>();

        if (Object.FindFirstObjectByType<SceneTransitionManager>() == null)
        {
            GameObject transitionGo = new GameObject("SceneTransitionManager");
            transitionGo.AddComponent<SceneTransitionManager>();
        }

        EditorSceneManager.SaveScene(scene, MainMenuPath);
    }

    static void ConfigureBuildSettings()
    {
        string gameplayPath = "Assets/Scenes/SampleScene.unity";

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuPath, true),
            new EditorBuildSettingsScene(gameplayPath, true)
        };
    }
}
#endif
