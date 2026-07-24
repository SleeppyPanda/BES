#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BES.Editor
{
    public static class BESProjectFix
    {
        const string TmpFixPrefsKey = "BES_TMP_EssentialsImported";

        /// <summary>TMP essentials — Burst cache bỏ qua khi Unity đang mở (DLL bị lock).</summary>
        public static void RunEnvironmentFix()
        {
            try
            {
                ImportTMPEssentials();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BES] TMP import bỏ qua: {ex.Message}");
            }

            TryClearBurstCache();
        }

        public static void EnsureTMPEssentials()
        {
            if (NeedsTMPImport())
                ImportTMPEssentials();
        }

        static bool NeedsTMPImport()
        {
            if (EditorPrefs.GetBool(TmpFixPrefsKey, false))
                return false;
            return true;
        }

        static void ImportTMPEssentials()
        {
            TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
            EditorPrefs.SetBool(TmpFixPrefsKey, true);
        }

        static void TryClearBurstCache()
        {
            var burstPath = Path.Combine(Application.dataPath, "..", "Library", "BurstCache");
            burstPath = Path.GetFullPath(burstPath);

            if (!Directory.Exists(burstPath))
                return;

            // Burst DLL thường bị Unity lock — bỏ qua im lặng, không chặn setup.
            try
            {
                Directory.Delete(burstPath, true);
                Debug.Log("[BES] Đã xóa Burst cache.");
            }
            catch (System.UnauthorizedAccessException)
            {
                Debug.Log("[BES] Bỏ qua xóa Burst cache (Unity đang giữ file). Không ảnh hưởng setup.");
            }
            catch (System.IO.IOException ex)
            {
                Debug.LogWarning($"[BES] Bỏ qua xóa Burst cache: {ex.Message}");
            }
        }
    }
}
#endif
