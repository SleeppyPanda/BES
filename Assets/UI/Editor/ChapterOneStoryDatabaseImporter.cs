using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BES.UI.Menu;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BES.EditorTools
{
    public static class ChapterOneStoryDatabaseImporter
    {
        const string MenuDatabasePath = "Assets/Scenes/MenuContentDatabase.asset";
        const string StoryFolder = "Assets/Resources/Main Story";
        const string ImportHashKey = "BES.ChapterOneStoryDatabaseImporter.SourceHash";

        [DidReloadScripts]
        static void AutoImportAfterScriptsReload()
        {
            EditorApplication.delayCall += ImportIfStoryChanged;
        }

        [MenuItem("BES/Story/Import Chapter 1 Text To MenuContentDatabase")]
        public static void ImportNow()
        {
            Import(force: true);
        }

        static void ImportIfStoryChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Import(force: false);
        }

        static void Import(bool force)
        {
            var hash = ComputeSourceHash();
            if (string.IsNullOrWhiteSpace(hash)) return;
            if (!force && EditorPrefs.GetString(ImportHashKey, string.Empty) == hash) return;

            var database = AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(MenuDatabasePath);
            if (database == null)
            {
                Debug.LogWarning($"[BES] Cannot import Chapter 1 story. Missing database at {MenuDatabasePath}.");
                return;
            }

            ChapterOneStoryRuntime.Apply(database, true);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(ImportHashKey, hash);
            Debug.Log("[BES] Imported Chapter 1 story text into MenuContentDatabase.asset.");
        }

        static string ComputeSourceHash()
        {
            if (!Directory.Exists(StoryFolder)) return string.Empty;
            using var md5 = MD5.Create();
            var builder = new StringBuilder();
            AppendFile("story rule");
            for (var i = 1; i <= 7; i++)
                AppendFile($"Chương 1 cảnh {i}");

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", string.Empty);

            void AppendFile(string fileName)
            {
                var path = Path.Combine(StoryFolder, fileName);
                if (!File.Exists(path)) return;
                builder.AppendLine(fileName);
                builder.AppendLine(File.ReadAllText(path, Encoding.UTF8));
            }
        }
    }
}
