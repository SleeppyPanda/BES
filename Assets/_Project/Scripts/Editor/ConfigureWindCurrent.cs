using UnityEditor;
using UnityEngine;
using System.IO;

namespace BES.EditorTools
{
    public static class ConfigureWindCurrent
    {
        [MenuItem("BES/World/Setup Wind Current Asset")]
        public static void SetupWindCurrent()
        {
            Debug.Log("[BES Wind Current] Bắt đầu cấu hình Bệ Gió và hiệu ứng Cột Gió...");

            string importFolder = "Assets/MeshyImports/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture_fbx";
            string fbxPath = $"{importFolder}/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture.fbx";
            string diffusePath = $"{importFolder}/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture.png";
            string metallicPath = $"{importFolder}/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture_metallic.png";
            string normalPath = $"{importFolder}/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture_normal.png";
            string roughnessPath = $"{importFolder}/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture_roughness.png";

            if (!File.Exists(fbxPath))
            {
                Debug.LogError($"[BES Wind Current] Không tìm thấy file FBX bệ gió tại: {fbxPath}");
                return;
            }

            // 1. Cấu hình Normal Map
            ConfigureNormalMap(normalPath);

            // 2. Tạo hoặc cấu hình Material cho Bệ Gió
            string matFolder = "Assets/_Project/Materials";
            if (!Directory.Exists(matFolder)) Directory.CreateDirectory(matFolder);
            string sigilMatPath = $"{matFolder}/ArcaneMoonlitSigil.mat";
            
            Material sigilMat = AssetDatabase.LoadAssetAtPath<Material>(sigilMatPath);
            if (sigilMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                sigilMat = new Material(shader);
                AssetDatabase.CreateAsset(sigilMat, sigilMatPath);
                Debug.Log($"[BES Wind Current] Đã tạo Material mới cho Bệ Gió tại: {sigilMatPath}");
            }

            Texture2D diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);

            if (diffuseTex != null)
            {
                if (sigilMat.HasProperty("_BaseMap")) sigilMat.SetTexture("_BaseMap", diffuseTex);
                else if (sigilMat.HasProperty("_MainTex")) sigilMat.SetTexture("_MainTex", diffuseTex);
            }

            if (normalTex != null)
            {
                if (sigilMat.HasProperty("_BumpMap"))
                {
                    sigilMat.SetTexture("_BumpMap", normalTex);
                    sigilMat.EnableKeyword("_NORMALMAP");
                }
            }

            if (metallicTex != null)
            {
                if (sigilMat.HasProperty("_MetallicGlossMap"))
                {
                    sigilMat.SetTexture("_MetallicGlossMap", metallicTex);
                    sigilMat.EnableKeyword("_METALLICGLOSSMAP");
                    if (sigilMat.HasProperty("_Metallic")) sigilMat.SetFloat("_Metallic", 1.0f);
                }
                else if (sigilMat.HasProperty("_Metallic"))
                {
                    sigilMat.SetFloat("_Metallic", 1.0f);
                }
            }

            EditorUtility.SetDirty(sigilMat);
            AssetDatabase.SaveAssets();

            // 3. Tạo Prefab kết hợp
            string prefabsFolder = "Assets/_Project/Resources/Prefabs";
            if (!Directory.Exists(prefabsFolder))
            {
                Directory.CreateDirectory(prefabsFolder);
            }
            string prefabPath = $"{prefabsFolder}/WindCurrent.prefab";

            // Tạo GameObject tạm
            GameObject rootGo = new GameObject("WindCurrent");
            rootGo.AddComponent<BES.Gameplay.WindCurrent>();

            // Thêm CapsuleCollider quét trigger (Dọc theo trục đứng Y)
            var col = rootGo.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 1.5f;
            col.height = 8f;
            col.center = new Vector3(0f, 4f, 0f);

            // Thêm BoxCollider phẳng trực tiếp ở root để tránh bị xoay theo FBX
            var baseBox = rootGo.AddComponent<BoxCollider>();
            baseBox.center = new Vector3(0f, 0.05f, 0f);
            baseBox.size = new Vector3(3f, 0.1f, 3f);

            // Tải mô hình FBX bệ gió
            GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxModel == null)
            {
                Debug.LogError($"[BES Wind Current] Không thể load FBX Model tại: {fbxPath}");
                Object.DestroyImmediate(rootGo);
                return;
            }

            // A. THÊM BỆ ĐỠ DƯỚI ĐẤT DẠNG NẰM NGANG (Flat Horizontal Sigil Base)
            GameObject sigilInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel);
            sigilInstance.name = "SigilBase";
            sigilInstance.transform.SetParent(rootGo.transform, false);
            
            // Xóa colliders mặc định của FBX
            var childCols = sigilInstance.GetComponentsInChildren<Collider>(true);
            foreach (var childCol in childCols)
            {
                Object.DestroyImmediate(childCol);
            }

            // Gán vật liệu đặc cho bệ đỡ
            var renderers = sigilInstance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = sigilMat;
            }
            
            // QUAN TRỌNG: Quay bệ đá góc nhập khẩu mặc định để nó nằm ngang sẵn
            sigilInstance.transform.localRotation = fbxModel.transform.localRotation;
            sigilInstance.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            // Tính toán bounds để lấy chiều cao thực tế của bệ đỡ
            Bounds sigilBounds = new Bounds(Vector3.zero, Vector3.zero);
            var sigilMeshFilters = sigilInstance.GetComponentsInChildren<MeshFilter>(true);
            bool sigilHasBounds = false;
            foreach (var mf in sigilMeshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    if (!sigilHasBounds)
                    {
                        sigilBounds = mf.sharedMesh.bounds;
                        sigilHasBounds = true;
                    }
                    else
                    {
                        sigilBounds.Encapsulate(mf.sharedMesh.bounds);
                    }
                }
            }

            // Do FBX bị xoay -90 độ quanh trục X, trục Z local của nó hướng theo trục Y thế giới!
            // Do đó chiều cao thực tế của bệ đá tương ứng với bounds.size.z nhân với scale 1.5f.
            float sigilHeight = sigilHasBounds ? sigilBounds.size.z * 1.5f : 0.2f;
            float sigilHalfHeight = sigilHeight / 2f;
            
            // Đặt localPosition Y bằng một nửa chiều cao + offset 0.05m để đảm bảo đáy bệ đá luôn chạm và nổi nhẹ trên cát
            sigilInstance.transform.localPosition = new Vector3(0f, sigilHalfHeight + 0.05f, 0f);

            // B. THÊM CỘT GIÓ HIỆU ỨNG HẠT (CFXR4 Wind Trails)
            string windTrailsPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Nature/CFXR4 Wind Trails.prefab";
            GameObject windTrailsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(windTrailsPath);
            if (windTrailsPrefab != null)
            {
                GameObject trailsInstance = (GameObject)PrefabUtility.InstantiatePrefab(windTrailsPrefab);
                trailsInstance.name = "WindTrails";
                trailsInstance.transform.SetParent(rootGo.transform, false);
                trailsInstance.transform.localPosition = Vector3.zero;
                trailsInstance.transform.localRotation = Quaternion.identity;
                trailsInstance.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                Debug.Log("[BES Wind Current] Đã gán thành công hiệu ứng Cột gió hạt Wind Trails.");
            }
            else
            {
                Debug.LogWarning($"[BES Wind Current] Không tìm thấy hiệu ứng gió tại: {windTrailsPath}. Sử dụng fallback ẩn.");
            }

            // Lưu thành Prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(rootGo, prefabPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(rootGo);

            Debug.Log($"[BES Wind Current] Đã tạo thành công Prefab cột gió tại: {prefabPath}");
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
                Debug.Log($"[BES Wind Current] Đã chuyển đổi Normal Map thành công cho: {normalPath}");
            }
        }
    }
}
