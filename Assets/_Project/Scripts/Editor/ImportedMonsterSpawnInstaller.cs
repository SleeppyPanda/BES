#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BES.EditorTools
{
    static class ImportedMonsterSpawnInstaller
    {
        const string ScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
        const string LegacySpawnRootName = "EnemySpawnRegions_ImportedMonster";

        [MenuItem("BES/Gameplay/Install Imported Monster Spawns")]
        public static void InstallFromMenu() => Install(true);

        static void TryAutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoInstall;
                return;
            }

            Install(false);
        }

        static void Install(bool logResult)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
            {
                if (!System.IO.File.Exists(ScenePath)) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            // Remove legacy white base.obj monster spawn regions if present
            var legacy = FindInScene(scene, LegacySpawnRootName);
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);

            // Delegate to the full Meshy Monster installer
            ImportedMeshyMonsterInstaller.Install(logResult);
        }

        static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                var found = FindInChildren(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        static Transform FindInChildren(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindInChildren(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
