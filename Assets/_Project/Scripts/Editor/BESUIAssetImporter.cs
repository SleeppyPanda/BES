#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BES.Editor
{
    public static class BESUIAssetImporter
    {
        const string DefaultSource = @"C:\Users\Admin\Downloads\BES 2.0";
        const string WorkspaceAssetsSource = @"C:\Users\Admin\.cursor\projects\c-Users-Admin-Documents-BES\assets";

        public static void Import(string sourceFolder = null)
        {
            if (string.IsNullOrEmpty(sourceFolder))
            {
                if (Directory.Exists(DefaultSource))
                    sourceFolder = DefaultSource;
                else if (Directory.Exists(WorkspaceAssetsSource))
                    sourceFolder = WorkspaceAssetsSource;
                else
                    sourceFolder = Path.Combine(Application.dataPath, "_Project/Art/UI/_Incoming");
            }

            if (!Directory.Exists(sourceFolder))
            {
                Debug.LogWarning("[BES] Không tìm thấy PNG UI. Đặt vào Downloads/BES 2.0 hoặc Assets/_Project/Art/UI/_Incoming");
                return;
            }

            EnsureFolders();
            var count = 0;
            foreach (var file in Directory.GetFiles(sourceFolder, "*.png"))
            {
                var name = NormalizeImportedFileName(Path.GetFileName(file));
                var category = Categorize(name);
                var destDir = Path.Combine(Application.dataPath, "_Project/Art/UI", category).Replace("\\", "/");
                var dest = Path.Combine(destDir, name);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, true);
                count++;
            }

            EnsureWeaponEnhanceBackground();
            CleanupIncomingDuplicates(sourceFolder);
            AssetDatabase.Refresh();
            ApplySpriteImportSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[BES] Imported {count} UI sprites into Assets/_Project/Art/UI");
        }

        static string NormalizeImportedFileName(string fileName)
        {
            const string prefix = "c__Users_Admin_AppData_Roaming_Cursor_User_workspaceStorage_";
            if (!fileName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return fileName;

            var stem = Path.GetFileNameWithoutExtension(fileName);
            var idx = stem.LastIndexOf("_images_", System.StringComparison.Ordinal);
            if (idx < 0)
                return fileName;

            var logical = stem[(idx + "_images_".Length)..];
            var dash = logical.LastIndexOf('-');
            if (dash > 0 && logical.Length - dash > 30)
                logical = logical[..dash];

            return logical.Replace('_', ' ') + ".png";
        }

        static void EnsureWeaponEnhanceBackground()
        {
            var src = Path.Combine(Application.dataPath, "_Project/Art/UI/Backgrounds/Up level Weapon.png");
            var dest = Path.Combine(Application.dataPath, "_Project/Art/UI/Weapon/Up level Weapon.png");
            if (!File.Exists(src) || File.Exists(dest))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(src, dest, true);
        }

        static void CleanupIncomingDuplicates(string sourceFolder)
        {
            var incoming = Path.Combine(Application.dataPath, "_Project/Art/UI/_Incoming");
            if (!Directory.Exists(incoming) || sourceFolder.Replace("\\", "/").EndsWith("/_Incoming"))
                return;

            foreach (var file in Directory.GetFiles(incoming, "*.png"))
            {
                try { File.Delete(file); }
                catch (System.Exception ex) { Debug.LogWarning($"[BES] Không xóa được {file}: {ex.Message}"); }
            }
        }

        static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/_Project/Art/UI",
                "Assets/_Project/Art/UI/Backgrounds",
                "Assets/_Project/Art/UI/Icons",
                "Assets/_Project/Art/UI/Frames",
                "Assets/_Project/Art/UI/HUD",
                "Assets/_Project/Art/UI/Weapon",
                "Assets/_Project/Art/UI/Common",
                "Assets/_Project/Art/UI/_Incoming",
                "Assets/_Project/UI/Prefabs/Atoms",
                "Assets/_Project/UI/Prefabs/Screens",
                "Assets/_Project/Data/UI"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
                    var name = Path.GetFileName(folder);
                    if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                        AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        static string Categorize(string fileName)
        {
            var n = fileName.ToLowerInvariant();
            if (System.Text.RegularExpressions.Regex.IsMatch(n,
                    @"^(start|loading|main play|character profile|interaction|mission|wish|event |team set|team setting|artifacts|soonview|username|personal)\.?|weapon\.png|weapon infor|weapon level"))
                return "Backgrounds";
            if (System.Text.RegularExpressions.Regex.IsMatch(n, @"weapon rank|up level weapon|weapon refinement|ehancement|bane of"))
                return "Weapon";
            if (System.Text.RegularExpressions.Regex.IsMatch(n, @"^(vector|star |polygon|user\.png|object|mask group|subtract|click to begin|required|loading \.)"))
                return "Icons";
            if (System.Text.RegularExpressions.Regex.IsMatch(n, @"^(rectangle|ellipse|union|frame |line |intersect)"))
                return "Frames";
            if (System.Text.RegularExpressions.Regex.IsMatch(n,
                    @"group 427322668|group 427322669|group 427322670|group 427322671|group 427322672|group 427322695|group 427322696|group 427322697|group 427322698|group 427322699|group 427322700|group 427322706|group 427322707|group 427322710|group 427322712|group 427322718|group 427322719|group 427322720|group 427322554|group 427322555|group 427322556|group 427322557|group 427322624|group 42732255|group 42732262|group 42732245|group 42732246|group 42732247"))
                return "HUD";
            return "Common";
        }

        static void ApplySpriteImportSettings()
        {
            var root = "Assets/_Project/Art/UI";
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png"))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = path.Contains("/Backgrounds/") || path.Contains("/Weapon/Up level") ? 4096 : 2048;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
