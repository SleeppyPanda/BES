using System.IO;
using BES.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BESIslandMapPreviewRenderer
{
    private const string ScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
    private const string OutputPath = "Assets/Scenes/BES_Island_MapPreview.png";

    [MenuItem("BES/Render Island Map Preview")]
    public static void RenderPreviewFromMenu()
    {
        RenderPreview();
    }

    public static void RenderPreview()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError($"Scene not found: {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        HideDebugObjectsForBeautyRender();

        GameObject cameraObject = new GameObject("Temp_MapPreview_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.orthographic = true;
        camera.orthographicSize = 138f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 900f;
        camera.fieldOfView = 35f;
        camera.transform.position = new Vector3(92f, 235f, -214f);
        camera.transform.rotation = Quaternion.Euler(58f, -24f, 0f);

        const int width = 2048;
        const int height = 1536;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };

        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        camera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        camera.Render();
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;

        byte[] bytes = image.EncodeToPNG();
        File.WriteAllBytes(OutputPath, bytes);

        Object.DestroyImmediate(image);
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(cameraObject);

        AssetDatabase.ImportAsset(OutputPath);
        AssetDatabase.Refresh();
        Debug.Log($"BES map preview rendered: {OutputPath}");
    }

    private static void HideDebugObjectsForBeautyRender()
    {
        GameObject labels = GameObject.Find("ZoneLabels_Annotated_Map");
        if (labels) labels.SetActive(false);

        GameObject interactions = GameObject.Find("Interactions_And_Spawn");
        if (interactions) interactions.SetActive(false);

        foreach (TextMesh text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            text.gameObject.SetActive(false);
        }

        foreach (BESInteractionZone zone in Object.FindObjectsByType<BESInteractionZone>(FindObjectsSortMode.None))
        {
            zone.gameObject.SetActive(false);
        }

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name.Contains("AnimalSpawner") || go.name.Contains("InteractionZone") || go.name.Contains("SurfaceMarker"))
            {
                go.SetActive(false);
            }
        }
    }
}
