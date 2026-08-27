using UnityEditor;
using UnityEngine;
using System.IO;

namespace BES.EditorTools
{
    public static class ConfigureTreasureChest
    {
        [MenuItem("BES/World/Setup Treasure Chest Asset")]
        public static void SetupChest()
        {
            Debug.Log("[BES Chest Setup] Bắt đầu cấu hình Rương Kho Báu...");

            string importFolder = "Assets/MeshyImports/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture_fbx";
            string fbxPath = $"{importFolder}/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture.fbx";
            string diffusePath = $"{importFolder}/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture.png";
            string metallicPath = $"{importFolder}/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture_metallic.png";
            string normalPath = $"{importFolder}/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture_normal.png";
            string roughnessPath = $"{importFolder}/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture_roughness.png";

            if (!File.Exists(fbxPath))
            {
                Debug.LogError($"[BES Chest Setup] Không tìm thấy file FBX rương tại: {fbxPath}");
                return;
            }

            // 1. Cấu hình Normal Map Texture Type
            ConfigureNormalMap(normalPath);

            // 2. Tạo hoặc cấu hình Material
            string materialsFolder = "Assets/_Project/Materials";
            if (!Directory.Exists(materialsFolder))
            {
                Directory.CreateDirectory(materialsFolder);
            }
            string materialPath = $"{materialsFolder}/TreasureChest.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                // Chọn shader URP Lit nếu có, ngược lại dùng Standard
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, materialPath);
                Debug.Log($"[BES Chest Setup] Đã tạo Material mới tại: {materialPath}");
            }

            // Gán texture vào Material
            Texture2D diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);

            if (diffuseTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diffuseTex); // URP Lit
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diffuseTex); // Standard
            }

            if (normalTex != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            if (metallicTex != null)
            {
                if (mat.HasProperty("_MetallicGlossMap"))
                {
                    mat.SetTexture("_MetallicGlossMap", metallicTex);
                    mat.EnableKeyword("_METALLICGLOSSMAP");
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 1.0f);
                }
                else if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 1.0f);
                }
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // 3. Tạo Prefab Rương Kho Báu
            string prefabsFolder = "Assets/_Project/Resources/Prefabs";
            if (!Directory.Exists(prefabsFolder))
            {
                Directory.CreateDirectory(prefabsFolder);
            }
            string prefabPath = $"{prefabsFolder}/TreasureChest.prefab";

            // Khởi tạo đối tượng tạm trong Scene để dựng Prefab
            GameObject rootGo = new GameObject("TreasureChest");
            
            // Instantiate mô hình FBX
            GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxModel == null)
            {
                Debug.LogError($"[BES Chest Setup] Không thể load FBX Model từ path: {fbxPath}");
                Object.DestroyImmediate(rootGo);
                return;
            }

            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel);
            modelInstance.name = "Model";
            modelInstance.transform.SetParent(rootGo.transform, false);

            // Gán Material cho tất cả MeshRenderer trong Model
            var renderers = modelInstance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // Thêm BoxCollider va chạm vật lý dựa trên Bounds của Mesh
            var boxCol = rootGo.AddComponent<BoxCollider>();
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            var meshFilters = modelInstance.GetComponentsInChildren<MeshFilter>(true);
            bool hasBounds = false;
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    if (!hasBounds)
                    {
                        bounds = mf.sharedMesh.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(mf.sharedMesh.bounds);
                    }
                }
            }

            if (hasBounds)
            {
                // Dịch chuyển mô hình lên phía trên bằng một nửa chiều cao thực tế để đáy rương chạm mốc Y = 0 (mặt đất)
                float halfHeight = bounds.size.y / 2f;
                modelInstance.transform.localPosition = new Vector3(0f, halfHeight, 0f);

                // Co giãn nhẹ kích thước collider cho khớp
                boxCol.center = new Vector3(bounds.center.x, bounds.center.y + halfHeight, bounds.center.z);
                boxCol.size = bounds.size;
            }
            else
            {
                modelInstance.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                boxCol.center = new Vector3(0f, 0.45f, 0f);
                boxCol.size = new Vector3(1.2f, 0.9f, 0.9f);
            }

            // Thêm SphereCollider quét tương tác
            var sphereCol = rootGo.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 2.5f;
            sphereCol.center = boxCol.center;

            // Thêm script tương tác TreasureChest (Sẽ tạo sau)
            System.Type chestType = System.Type.GetType("BES.Gameplay.TreasureChest, Assembly-CSharp");
            if (chestType != null)
            {
                rootGo.AddComponent(chestType);
            }
            else
            {
                Debug.LogWarning("[BES Chest Setup] Script 'TreasureChest' chưa được biên dịch. Component sẽ được add tự động sau.");
            }

            // Lưu thành Prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(rootGo, prefabPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(rootGo);

            Debug.Log($"[BES Chest Setup] Đã cấu hình và lưu Prefab rương thành công tại: {prefabPath}");
            AssetDatabase.Refresh();
        }

        static void ConfigureNormalMap(string normalPath)
        {
            if (!File.Exists(normalPath)) return;
            var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
                Debug.Log($"[BES Chest Setup] Đã cấu hình Texture Normal Map thành công cho: {normalPath}");
            }
        }
    }
}
