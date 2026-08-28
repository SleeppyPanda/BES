#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    public static class PrintPlatformPositions
    {
        static PrintPlatformPositions()
        {
            EditorApplication.delayCall += RunScan;
        }

        private static void RunScan()
        {
            var activeScene = SceneManager.GetActiveScene();
            var filePath = @"C:\Users\Admin\.gemini\antigravity-ide\brain\139abc84-6eed-49de-94b1-448993f98d3f\scratch\platforms.txt";
            
            try
            {
                using (var writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine($"Active scene: {activeScene.name}");
                    var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                    writer.WriteLine($"Total renderers found: {renderers.Length}");
                    foreach (var r in renderers)
                    {
                        if (r.gameObject.scene == activeScene)
                        {
                            string name = r.gameObject.name.ToLower();
                            writer.WriteLine($"Renderer: {r.gameObject.name} | Pos: {r.transform.position.x:F3},{r.transform.position.y:F3},{r.transform.position.z:F3}");
                        }
                    }
                }
                Debug.Log("[PrintPlatformPositions] Finished writing platforms to scratch file.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PrintPlatformPositions] Error writing file: {ex.Message}");
            }
        }
    }
}
#endif
