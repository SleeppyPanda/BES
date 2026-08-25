using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BES.UI.Editor
{
    public static class VietnameseTMPFontAssetGenerator
    {
        const string OutputFolder = "Assets/TextMesh Pro/Resources/Fonts & Materials";
        const int SamplingPointSize = 90;
        const int AtlasPadding = 9;
        const int AtlasSize = 2048;

        const string VietnameseCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "0123456789" +
            "\u00C0\u00C1\u00C2\u00C3\u00C8\u00C9\u00CA\u00CC\u00CD\u00D2\u00D3\u00D4\u00D5\u00D9\u00DA\u0102\u0110\u0128\u0168\u01A0\u01AF" +
            "\u00E0\u00E1\u00E2\u00E3\u00E8\u00E9\u00EA\u00EC\u00ED\u00F2\u00F3\u00F4\u00F5\u00F9\u00FA\u0103\u0111\u0129\u0169\u01A1\u01B0" +
            "\u1EA0\u1EA2\u1EA4\u1EA6\u1EA8\u1EAA\u1EAC\u1EAE\u1EB0\u1EB2\u1EB4\u1EB6\u1EB8\u1EBA\u1EBC\u1EBE\u1EC0\u1EC2\u1EC4\u1EC6\u1EC8\u1ECA\u1ECC\u1ECE\u1ED0\u1ED2\u1ED4\u1ED6\u1ED8\u1EDA\u1EDC\u1EDE\u1EE0\u1EE2\u1EE4\u1EE6\u1EE8\u1EEA\u1EEC\u1EEE\u1EF0\u1EF2\u1EF4\u00DD\u1EF6\u1EF8" +
            "\u1EA1\u1EA3\u1EA5\u1EA7\u1EA9\u1EAB\u1EAD\u1EAF\u1EB1\u1EB3\u1EB5\u1EB7\u1EB9\u1EBB\u1EBD\u1EBF\u1EC1\u1EC3\u1EC5\u1EC7\u1EC9\u1ECB\u1ECD\u1ECF\u1ED1\u1ED3\u1ED5\u1ED7\u1ED9\u1EDB\u1EDD\u1EDF\u1EE1\u1EE3\u1EE5\u1EE7\u1EE9\u1EEB\u1EED\u1EEF\u1EF1\u1EF3\u1EF5\u1EF7\u1EF9" +
            "\u0102\u00C2\u00CA\u00D4\u01A0\u01AF\u0103\u00E2\u00EA\u00F4\u01A1\u01B0\u0110\u0111" +
            " .,:;!?\"'()[]{}<>+-*/=%_#@&|\\~`^\u00B0\u2026\u2013\u2014\u00B7\n";

        static readonly FontBuildRequest[] Fonts =
        {
            new FontBuildRequest(
                "Assets/Art Ui/Game Vi\u1EC7t h\u00F3a m\u1EDBi/SVN-MoneyGame (1).otf",
                "SVN-MoneyGame SDF"),
            new FontBuildRequest(
                "Assets/Art Ui/Game Vi\u1EC7t h\u00F3a m\u1EDBi/hywenhei/zhcn.ttf",
                "zhcn SDF")
        };

        [MenuItem("BES/Fonts/Generate Vietnamese TMP Font Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputFolder);

            foreach (var request in Fonts)
                GenerateFontAsset(request);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BES] Vietnamese TMP font assets generated.");
        }

        [MenuItem("BES/Fonts/Regenerate Vietnamese TMP Font Assets")]
        public static void Regenerate()
        {
            Directory.CreateDirectory(OutputFolder);
            foreach (var request in Fonts)
            {
                AssetDatabase.DeleteAsset(GetAssetPath(request.AssetName));
                AssetDatabase.DeleteAsset(GetMainMaterialPath(request.AssetName));
                AssetDatabase.DeleteAsset(GetOutlineMaterialPath(request.AssetName));
            }

            Generate();
        }

        [InitializeOnLoadMethod]
        static void GenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (!AnyFontAssetMissing()) return;
                Generate();
            };
        }

        static bool AnyFontAssetMissing()
        {
            foreach (var request in Fonts)
            {
                if (!File.Exists(GetAssetPath(request.AssetName)))
                    return true;
                if (!File.Exists(GetMainMaterialPath(request.AssetName)))
                    return true;
                if (!File.Exists(GetOutlineMaterialPath(request.AssetName)))
                    return true;
            }
            return false;
        }

        static void GenerateFontAsset(FontBuildRequest request)
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(request.SourcePath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[BES] Missing source font: {request.SourcePath}");
                return;
            }

            var assetPath = GetAssetPath(request.AssetName);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset != null && IsFontAssetAtlasBroken(fontAsset))
            {
                Debug.LogWarning($"[BES] {request.AssetName} has a broken atlas reference. Deleting and recreating it.");
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(GetMainMaterialPath(request.AssetName));
                AssetDatabase.DeleteAsset(GetOutlineMaterialPath(request.AssetName));
                AssetDatabase.Refresh();
                fontAsset = null;
            }

            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    SamplingPointSize,
                    AtlasPadding,
                    GlyphRenderMode.SDFAA,
                    AtlasSize,
                    AtlasSize,
                    AtlasPopulationMode.Dynamic,
                    true);

                fontAsset.name = request.AssetName;
                ConfigureFontAsset(fontAsset);
                EnsureAtlasReference(fontAsset, request.AssetName);
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                EnsureAtlasReference(fontAsset, request.AssetName);
            }

            ConfigureFontAsset(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            EnsureAtlasReference(fontAsset, request.AssetName);
            TrySeedVietnameseGlyphs(fontAsset, request.AssetName);
            EnsureAtlasReference(fontAsset, request.AssetName);

            EnsureMainMaterial(fontAsset, request.AssetName);
            ConfigureOutlineMaterial(fontAsset, request.AssetName);
            ConfigureFallbacks(fontAsset, request.AssetName);
            EditorUtility.SetDirty(fontAsset);
        }

        static void TrySeedVietnameseGlyphs(TMP_FontAsset fontAsset, string assetName)
        {
            if (fontAsset == null) return;

            try
            {
                var missingBeforeSeed = GetCharactersMissingFromAsset(fontAsset, VietnameseCharacters);
                if (string.IsNullOrEmpty(missingBeforeSeed))
                    return;

                fontAsset.TryAddCharacters(missingBeforeSeed, out _);

                var missingAfterSeed = GetCharactersMissingFromAsset(fontAsset, missingBeforeSeed);
                if (!string.IsNullOrEmpty(missingAfterSeed))
                    Debug.LogWarning($"[BES] {assetName} missing glyphs from source font: {missingAfterSeed}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BES] Could not seed Vietnamese glyphs for {assetName}. Regenerate again after Unity refreshes the asset. {ex.GetType().Name}: {ex.Message}");
            }
        }

        static string GetCharactersMissingFromAsset(TMP_FontAsset fontAsset, string characters)
        {
            if (fontAsset == null || string.IsNullOrEmpty(characters)) return string.Empty;

            var builder = new System.Text.StringBuilder();
            var seen = new System.Collections.Generic.HashSet<char>();
            foreach (var character in characters)
            {
                if (!seen.Add(character)) continue;
                if (!fontAsset.HasCharacter(character, false, false))
                    builder.Append(character);
            }

            return builder.ToString();
        }

        static void EnsureAtlasReference(TMP_FontAsset fontAsset, string assetName)
        {
            if (fontAsset == null) return;

            var atlas = fontAsset.atlasTexture;
            if (atlas == null && fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                atlas = fontAsset.atlasTextures[0];

            if (atlas == null)
            {
                Debug.LogWarning($"[BES] {assetName} has no atlas texture after creation. Check whether the source font imports correctly.");
                return;
            }

            var assetPath = GetAssetPath(assetName);
            if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath) && !AssetDatabase.Contains(atlas))
                AssetDatabase.AddObjectToAsset(atlas, assetPath);

            var serialized = new SerializedObject(fontAsset);
            var atlasTextures = serialized.FindProperty("m_AtlasTextures");
            if (atlasTextures != null)
            {
                atlasTextures.arraySize = 1;
                atlasTextures.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
            }

            var atlasTexture = serialized.FindProperty("m_AtlasTexture");
            if (atlasTexture != null)
                atlasTexture.objectReferenceValue = atlas;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            fontAsset.atlasTextures = new[] { atlas };
            EditorUtility.SetDirty(fontAsset);
        }

        static void ConfigureFontAsset(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;
        }

        static void EnsureMainMaterial(TMP_FontAsset fontAsset, string assetName)
        {
            if (fontAsset == null) return;

            var materialPath = GetMainMaterialPath(assetName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var source = GetAssignedMaterial(fontAsset);
                material = source != null ? new Material(source) : new Material(FindTMPShader());
                material.name = assetName;
                material.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(material, materialPath);
            }

            ConfigureMaterial(material, GetAtlasTexture(fontAsset), false);
            AssignMainMaterial(fontAsset, material);
            EditorUtility.SetDirty(material);
        }

        static void EnsureAtlasTexture(TMP_FontAsset fontAsset, string assetName)
        {
            if (fontAsset == null) return;

            var serialized = new SerializedObject(fontAsset);
            var atlasTextures = serialized.FindProperty("m_AtlasTextures");
            Texture2D atlas = null;
            if (atlasTextures != null && atlasTextures.arraySize > 0)
                atlas = atlasTextures.GetArrayElementAtIndex(0).objectReferenceValue as Texture2D;

            if (atlas == null)
            {
                atlas = new Texture2D(AtlasSize, AtlasSize, TextureFormat.Alpha8, false)
                {
                    name = $"{assetName} Atlas",
                    hideFlags = HideFlags.None
                };
                atlas.Apply(false, false);
                AssetDatabase.AddObjectToAsset(atlas, AssetDatabase.GetAssetPath(fontAsset));
            }

            if (atlasTextures != null)
            {
                atlasTextures.arraySize = 1;
                atlasTextures.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
            }

            var atlasTexture = serialized.FindProperty("m_AtlasTexture");
            if (atlasTexture != null)
                atlasTexture.objectReferenceValue = atlas;

            var atlasTextureIndex = serialized.FindProperty("m_AtlasTextureIndex");
            if (atlasTextureIndex != null)
                atlasTextureIndex.intValue = 0;

            var atlasWidth = serialized.FindProperty("m_AtlasWidth");
            if (atlasWidth != null)
                atlasWidth.intValue = AtlasSize;

            var atlasHeight = serialized.FindProperty("m_AtlasHeight");
            if (atlasHeight != null)
                atlasHeight.intValue = AtlasSize;

            var atlasPadding = serialized.FindProperty("m_AtlasPadding");
            if (atlasPadding != null)
                atlasPadding.intValue = AtlasPadding;

            var atlasRenderMode = serialized.FindProperty("m_AtlasRenderMode");
            if (atlasRenderMode != null)
                atlasRenderMode.intValue = (int)GlyphRenderMode.SDFAA;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            fontAsset.atlasTextures = new[] { atlas };
            EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(fontAsset));
        }

        static bool IsFontAssetAtlasBroken(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return true;
            var serialized = new SerializedObject(fontAsset);
            var atlasTextures = serialized.FindProperty("m_AtlasTextures");
            if (atlasTextures == null || atlasTextures.arraySize == 0) return true;
            return atlasTextures.GetArrayElementAtIndex(0).objectReferenceValue == null;
        }

        static void ConfigureOutlineMaterial(TMP_FontAsset fontAsset, string assetName)
        {
            if (fontAsset == null) return;
            var mainMaterial = GetAssignedMaterial(fontAsset);
            if (mainMaterial == null) return;

            var materialPath = GetOutlineMaterialPath(assetName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(mainMaterial) { name = $"{assetName} - Outline" };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            ConfigureMaterial(material, GetAtlasTexture(fontAsset), true);
            EditorUtility.SetDirty(material);
        }

        static Material GetAssignedMaterial(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return null;
            var serialized = new SerializedObject(fontAsset);
            return serialized.FindProperty("m_Material")?.objectReferenceValue as Material;
        }

        static Texture GetAtlasTexture(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return null;
            var serialized = new SerializedObject(fontAsset);
            var atlasTextures = serialized.FindProperty("m_AtlasTextures");
            if (atlasTextures == null || atlasTextures.arraySize == 0) return null;
            return atlasTextures.GetArrayElementAtIndex(0).objectReferenceValue as Texture;
        }

        static void AssignMainMaterial(TMP_FontAsset fontAsset, Material material)
        {
            if (fontAsset == null || material == null) return;
            var serialized = new SerializedObject(fontAsset);
            var materialProperty = serialized.FindProperty("m_Material");
            if (materialProperty != null)
                materialProperty.objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureFallbacks(TMP_FontAsset fontAsset, string assetName)
        {
            if (fontAsset == null) return;
            fontAsset.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
            fontAsset.fallbackFontAssetTable.Clear();

            foreach (var request in Fonts)
            {
                if (request.AssetName == assetName) continue;
                var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GetAssetPath(request.AssetName));
                if (fallback != null && fallback != fontAsset)
                    fontAsset.fallbackFontAssetTable.Add(fallback);
            }

            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{OutputFolder}/LiberationSans SDF.asset");
            if (liberation != null && liberation != fontAsset)
                fontAsset.fallbackFontAssetTable.Add(liberation);
        }

        static Shader FindTMPShader()
        {
            return Shader.Find("TextMeshPro/Mobile/Distance Field") ??
                   Shader.Find("TextMeshPro/Distance Field") ??
                   Shader.Find("UI/Default");
        }

        static void ConfigureMaterial(Material material, Texture atlasTexture, bool outline)
        {
            if (material == null) return;
            var shader = FindTMPShader();
            if (shader != null)
                material.shader = shader;

            if (atlasTexture != null && material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", atlasTexture);
            if (material.HasProperty("_TextureWidth"))
                material.SetFloat("_TextureWidth", AtlasSize);
            if (material.HasProperty("_TextureHeight"))
                material.SetFloat("_TextureHeight", AtlasSize);
            if (material.HasProperty("_GradientScale"))
                material.SetFloat("_GradientScale", AtlasPadding + 1);
            if (material.HasProperty("_FaceColor"))
                material.SetColor("_FaceColor", Color.white);
            if (material.HasProperty("_OutlineColor"))
                material.SetColor("_OutlineColor", Color.black);
            if (material.HasProperty("_OutlineWidth"))
                material.SetFloat("_OutlineWidth", outline ? 0.035f : 0f);
            if (material.HasProperty("_FaceDilate"))
                material.SetFloat("_FaceDilate", outline ? 0f : -0.02f);
            if (material.HasProperty("_OutlineSoftness"))
                material.SetFloat("_OutlineSoftness", outline ? 0.01f : 0f);
            if (material.HasProperty("_UnderlaySoftness"))
                material.SetFloat("_UnderlaySoftness", 0f);
        }

        static string GetAssetPath(string assetName) => $"{OutputFolder}/{assetName}.asset";
        static string GetMainMaterialPath(string assetName) => $"{OutputFolder}/{assetName}.mat";
        static string GetOutlineMaterialPath(string assetName) => $"{OutputFolder}/{assetName} - Outline.mat";

        readonly struct FontBuildRequest
        {
            public readonly string SourcePath;
            public readonly string AssetName;

            public FontBuildRequest(string sourcePath, string assetName)
            {
                SourcePath = sourcePath;
                AssetName = assetName;
            }
        }
    }
}
