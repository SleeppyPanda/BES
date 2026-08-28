#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using BES.Gameplay;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    public static class CheckSceneSpawners
    {
        static CheckSceneSpawners()
        {
            EditorApplication.delayCall += RunScan;
        }

        private static void RunScan()
        {
            var activeScene = SceneManager.GetActiveScene();
            var filePath = @"C:\Users\Admin\.gemini\antigravity-ide\brain\139abc84-6eed-49de-94b1-448993f98d3f\scratch\spawners.txt";
            
            try
            {
                using (var writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine($"Active scene: {activeScene.name}");
                    var spawners = Object.FindObjectsByType<EnemySpawnRegion>(FindObjectsSortMode.None);
                    writer.WriteLine($"Total spawners found: {spawners.Length}");
                    foreach (var s in spawners)
                    {
                        var serialized = new SerializedObject(s);
                        var prefabsProp = serialized.FindProperty("enemyPrefabs");
                        var areaProp = serialized.FindProperty("spawnArea");
                        var minProp = serialized.FindProperty("minSpawnCount");
                        var maxProp = serialized.FindProperty("maxSpawnCount");
                        
                        writer.WriteLine($"Spawner: {s.gameObject.name}");
                        writer.WriteLine($"  - RegionId: {s.RegionId}, SubRegionId: {s.SubRegionId}");
                        writer.WriteLine($"  - Spawn Area: {(areaProp.objectReferenceValue != null ? areaProp.objectReferenceValue.name : "NULL")}");
                        writer.WriteLine($"  - Min Count: {minProp.intValue}, Max Count: {maxProp.intValue}");
                        writer.WriteLine($"  - Prefabs Array Size: {prefabsProp.arraySize}");
                        for (int i = 0; i < prefabsProp.arraySize; i++)
                        {
                            var elem = prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue;
                            writer.WriteLine($"    * Prefab [{i}]: {(elem != null ? elem.name : "NULL")}");
                        }
                    }
                }
                Debug.Log("[CheckSceneSpawners] Finished writing spawners to scratch file.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CheckSceneSpawners] Error writing file: {ex.Message}");
            }
        }
    }
}
#endif
