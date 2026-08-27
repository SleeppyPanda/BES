using System.Linq;
using UnityEditor;
using UnityEngine;
using BES.UI.Menu;

namespace BES.UI.Editor
{
    internal static class MenkaraAttackPreviewBuilder
    {
        private const string SheetPath = "Assets/Art Ui/Game Việt hóa mới/Character/Menkara/tétt.png";
        private const string IdlePath = "Assets/Art Ui/Game Việt hóa mới/Character/Menkara/Idle Chibi (1).png";
        private const string DatabasePath = "Assets/Scenes/MenuContentDatabase.asset";
        private const string BuildVersion = "menkara-battle-attack-v3";

        // The five drawings have unequal widths, so these boundaries follow the
        // transparent gaps in the supplied 2172 x 724 sheet instead of a grid.
        private static readonly int[] FrameEdges = { 0, 335, 705, 1118, 1563, 2172 };

        [InitializeOnLoadMethod]
        private static void BuildWhenImported()
        {
            EditorApplication.delayCall += () =>
            {
                var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
                if (importer != null && importer.userData != BuildVersion)
                    Build(false);
            };
        }

        [MenuItem("Tools/BES/Battle/Setup Menkara Attack Frames")]
        private static void RebuildFromMenu() => Build(true);

        private static void Build(bool selectResult)
        {
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Cannot find Menkara attack sheet at '{SheetPath}'.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritesheet = Enumerable.Range(0, 5).Select(i => new SpriteMetaData
            {
                name = $"Menkara_Attack_{i + 1:00}",
                rect = new Rect(FrameEdges[i], 0, FrameEdges[i + 1] - FrameEdges[i], 724),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            }).ToArray();
            importer.userData = BuildVersion;
            importer.SaveAndReimport();

            var frames = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();

            var idleImporter = AssetImporter.GetAtPath(IdlePath) as TextureImporter;
            if (idleImporter != null && idleImporter.spriteImportMode != SpriteImportMode.Single)
            {
                idleImporter.textureType = TextureImporterType.Sprite;
                idleImporter.spriteImportMode = SpriteImportMode.Single;
                idleImporter.mipmapEnabled = false;
                idleImporter.alphaIsTransparency = true;
                idleImporter.SaveAndReimport();
            }
            var idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IdlePath);
            AssignMenkaraBattleFrames(idleSprite, frames);

            AssetDatabase.SaveAssets();
            if (selectResult)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(DatabasePath);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            Debug.Log("Menkara's 5 attack frames are assigned directly to MenuContentDatabase and ready in Turn Battle.");
        }

        private static void AssignMenkaraBattleFrames(Sprite idleSprite, Sprite[] frames)
        {
            if (frames == null || frames.Length < 5) return;
            var database = AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(DatabasePath);
            if (database == null) return;

            var serialized = new SerializedObject(database);
            var characters = serialized.FindProperty("characters");
            for (int i = 0; i < characters.arraySize; i++)
            {
                var character = characters.GetArrayElementAtIndex(i);
                if (!string.Equals(character.FindPropertyRelative("id").stringValue, "menkara", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (idleSprite != null)
                    character.FindPropertyRelative("chibi").objectReferenceValue = idleSprite;
                for (int frame = 0; frame < 5; frame++)
                    character.FindPropertyRelative($"attackFrame{frame + 1}").objectReferenceValue = frames[frame];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(database);
                return;
            }
        }
    }
}
