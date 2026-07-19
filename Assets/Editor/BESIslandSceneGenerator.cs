using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BES.Environment;
using BES.Interactions;
using BES.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BESIslandSceneGenerator
{
    private const string ScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
    private const string GeneratedRoot = "Assets/Environment/Generated";
    private const string ObjectCsvPath = GeneratedRoot + "/BES_PlacedObjects.csv";
    private const string QaChecklistPath = GeneratedRoot + "/BES_QA_Checklist.md";
    private const int TerrainResolution = 257;
    private const float TerrainSize = 512f;
    private const float TerrainHeight = 54f;
    private static readonly Vector3 TerrainOrigin = new Vector3(-TerrainSize * 0.5f, -4f, -TerrainSize * 0.5f);
    private static System.Random Rng = new System.Random(16092005);

    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    [MenuItem("BES/Generate Complete Island Scene")]
    public static void GenerateFromMenu()
    {
        GenerateScene();
    }

    public static void GenerateScene()
    {
        Rng = new System.Random(16092005);
        EnsureFolders();
        CreateMaterials();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = CreateRoot("IslandScene_BES_SocialIsland");
        GameObject playerRoot = CreateChild(root, "00_Player_Spawn_And_Camera");
        GameObject terrainRoot = CreateChild(root, "Terrain");
        GameObject zonesRoot = CreateChild(root, "ZoneLabels_Annotated_Map");
        GameObject propsRoot = CreateChild(root, "Environment");
        GameObject npcRoot = CreateChild(root, "NPCs");
        GameObject animalsRoot = CreateChild(root, "Animals");
        GameObject interactionRoot = CreateChild(root, "Interactions_And_Spawn");
        GameObject vfxRoot = CreateChild(root, "VFX");
        GameObject optimizationRoot = CreateChild(root, "07_Bake_NavMesh_LOD_Occlusion");

        Terrain terrain = CreateTerrain(terrainRoot.transform);
        CreateOcean(terrainRoot.transform);
        AddLighting(vfxRoot.transform);
        AddPlayerAndCamera(playerRoot.transform);

        CreateZoneLabels(zonesRoot.transform);
        BuildCentralPlaza(propsRoot.transform, interactionRoot.transform, npcRoot.transform);
        BuildNorthForest(propsRoot.transform, interactionRoot.transform, npcRoot.transform, animalsRoot.transform, vfxRoot.transform);
        BuildCherryForest(propsRoot.transform, interactionRoot.transform, npcRoot.transform, vfxRoot.transform);
        BuildSouthPool(propsRoot.transform, interactionRoot.transform, npcRoot.transform);
        BuildEastHarbor(propsRoot.transform, interactionRoot.transform, npcRoot.transform);
        BuildNortheastViewpoint(propsRoot.transform, interactionRoot.transform, npcRoot.transform);
        BuildSoutheastCampingIsland(propsRoot.transform, interactionRoot.transform, npcRoot.transform);
        BuildPathsAndBridges(propsRoot.transform);
        BuildCoastline(propsRoot.transform);
        BuildFlyingBirds(vfxRoot.transform);
        BuildOptimizationMarkers(optimizationRoot.transform);
        ExportObjectCsv(root.transform);
        WriteQaChecklist();

        Selection.activeGameObject = root;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"BES island scene generated: {ScenePath} with terrain {terrain.name}");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Environment");
        EnsureFolder("Assets/NPC");
        EnsureFolder("Assets/Interactions");
        EnsureFolder("Assets/Editor");
        EnsureFolder(GeneratedRoot);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    private static void CreateMaterials()
    {
        Materials.Clear();
        RegisterMaterial("Grass", new Color(0.34f, 0.66f, 0.29f), 0.35f);
        RegisterMaterial("DeepGrass", new Color(0.18f, 0.46f, 0.22f), 0.4f);
        RegisterMaterial("Stone", new Color(0.62f, 0.57f, 0.49f), 0.25f);
        RegisterMaterial("Sand", new Color(0.82f, 0.72f, 0.48f), 0.45f);
        RegisterMaterial("Wood", new Color(0.43f, 0.24f, 0.11f), 0.3f);
        RegisterMaterial("Water", new Color(0.08f, 0.58f, 0.78f, 0.72f), 0.02f, true);
        RegisterMaterial("Ocean", new Color(0.02f, 0.23f, 0.42f, 0.78f), 0.03f, true);
        RegisterMaterial("SakuraPink", new Color(1f, 0.48f, 0.76f), 0.5f);
        RegisterMaterial("ToriiRed", new Color(0.78f, 0.08f, 0.04f), 0.35f);
        RegisterMaterial("ZoneBlue", new Color(0.1f, 0.45f, 1f, 0.25f), 0.2f, true);
        RegisterMaterial("DebugInvisible", new Color(0.1f, 0.45f, 1f, 0.03f), 0.2f, true);
        RegisterMaterial("White", new Color(0.92f, 0.9f, 0.82f), 0.45f);
        RegisterMaterial("Foam", new Color(0.92f, 0.98f, 1f, 0.62f), 0.08f, true);
        RegisterMaterial("Gold", new Color(1f, 0.72f, 0.24f), 0.55f);
        RegisterMaterial("BannerBlue", new Color(0.1f, 0.34f, 0.78f), 0.42f);
        RegisterMaterial("BannerRed", new Color(0.72f, 0.12f, 0.11f), 0.42f);
        RegisterMaterial("LanternWarm", new Color(1f, 0.72f, 0.34f, 0.78f), 0.15f, true);
        RegisterMaterial("LeafLight", new Color(0.48f, 0.76f, 0.28f), 0.42f);
        RegisterMaterial("StoneDark", new Color(0.36f, 0.36f, 0.35f), 0.22f);
        RegisterMaterial("PetalGround", new Color(1f, 0.62f, 0.82f, 0.52f), 0.38f, true);
        RegisterMaterial("Rope", new Color(0.62f, 0.45f, 0.26f), 0.28f);
        RegisterMaterial("FireGlow", new Color(1f, 0.38f, 0.08f, 0.7f), 0.1f, true);
    }

    private static void RegisterMaterial(string name, Color color, float smoothness, bool transparent = false)
    {
        string path = $"{GeneratedRoot}/M_BES_{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.name = $"M_BES_{name}";
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        if (transparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        Materials[name] = mat;
        EditorUtility.SetDirty(mat);
    }

    private static Terrain CreateTerrain(Transform parent)
    {
        TerrainData data = new TerrainData
        {
            heightmapResolution = TerrainResolution,
            alphamapResolution = 256,
            size = new Vector3(TerrainSize, TerrainHeight, TerrainSize)
        };

        float[,] heights = new float[TerrainResolution, TerrainResolution];
        for (int z = 0; z < TerrainResolution; z++)
        {
            for (int x = 0; x < TerrainResolution; x++)
            {
                float worldX = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, x / (float)(TerrainResolution - 1));
                float worldZ = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, z / (float)(TerrainResolution - 1));
                float mainMask = IslandMask(worldX, worldZ);
                float campMask = CampIslandMask(worldX, worldZ);
                float island = Mathf.Max(mainMask, campMask);
                float cliffBoost = Mathf.Clamp01((worldX - 46f) / 48f) * Mathf.Clamp01((worldZ + 4f) / 58f);
                float cliffRidge = EllipseMask(worldX - 84f, worldZ - 58f, 54f, 42f) * 0.16f;
                float southwestRockRise = EllipseMask(worldX + 78f, worldZ + 92f, 46f, 32f) * 0.055f;
                float northRise = Mathf.Clamp01((worldZ - 18f) / 82f) * 0.19f;
                float campLowering = CampIslandMask(worldX, worldZ) * 0.045f;
                float poolDip = 0.05f * EllipseMask(worldX + 34f, worldZ + 74f, 42f, 30f);
                float plazaFlatten = Mathf.Clamp01(1f - new Vector2(worldX, worldZ).magnitude / 48f);
                float harborBeach = SegmentDistanceMask(new Vector2(worldX, worldZ), new Vector2(56f, -28f), new Vector2(112f, -42f), 20f) * 0.06f;
                float noise = Mathf.PerlinNoise(worldX * 0.035f + 22f, worldZ * 0.035f + 11f) * 0.035f;
                float h = island * (0.16f + noise + northRise + cliffBoost * 0.2f + cliffRidge + southwestRockRise - campLowering) - poolDip;
                h = Mathf.Lerp(h, 0.17f, plazaFlatten * island);
                h -= harborBeach;
                heights[z, x] = Mathf.Clamp01(h);
            }
        }

        data.SetHeights(0, 0, heights);
        data.terrainLayers = CreateTerrainLayers();
        PaintTerrain(data);
        AddTerrainGrassDetails(data);

        string dataPath = $"{GeneratedRoot}/BES_Island_Terrain.asset";
        AssetDatabase.DeleteAsset(dataPath);
        AssetDatabase.CreateAsset(data, dataPath);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "Terrain_MainIsland_And_CampingIsland";
        terrainObject.transform.position = TerrainOrigin;
        terrainObject.transform.SetParent(parent, false);
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        TerrainCollider collider = terrainObject.GetComponent<TerrainCollider>();
        collider.terrainData = data;
        return terrain;
    }

    private static TerrainLayer[] CreateTerrainLayers()
    {
        return new[]
        {
            CreateTerrainLayer("Grass", new Color(0.28f, 0.58f, 0.25f)),
            CreateTerrainLayer("Sand", new Color(0.76f, 0.68f, 0.43f)),
            CreateTerrainLayer("Stone", new Color(0.52f, 0.50f, 0.46f)),
            CreateTerrainLayer("DeepGrass", new Color(0.16f, 0.38f, 0.18f))
        };
    }

    private static TerrainLayer CreateTerrainLayer(string name, Color color)
    {
        string texturePath = $"{GeneratedRoot}/T_BES_{name}.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GetTerrainTexturePath(name));
        if (!texture) texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (!texture)
        {
            texture = new Texture2D(4, 4, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, texturePath);
        }

        string layerPath = $"{GeneratedRoot}/TL_BES_{name}.terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (!layer)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }
        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(18f, 18f);
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static string GetTerrainTexturePath(string name)
    {
        switch (name)
        {
            case "Grass":
                return "Assets/3D set of stylized nature - GHIBLI style/Art/Textures/Grass_ground_Base_Color.png";
            case "DeepGrass":
                return "Assets/3D_Game_Assets_Flora/Terrain_Material/Materials/Green_Base_color.tga";
            case "Sand":
                return "Assets/3D_Game_Assets_Flora/Terrain_Material/Materials/Sand_Base_color.tga";
            case "Stone":
                return "Assets/3D set of stylized nature - GHIBLI style/Art/Textures/Road_Base_Color.png";
            default:
                return "";
        }
    }

    private static void PaintTerrain(TerrainData data)
    {
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] maps = new float[h, w, 4];
        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                float worldX = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, x / (float)(w - 1));
                float worldZ = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, z / (float)(h - 1));
                float plaza = Mathf.Clamp01(1f - new Vector2(worldX, worldZ).magnitude / 46f);
                float beach = Mathf.Clamp01(1f - Mathf.Max(IslandMask(worldX, worldZ), CampIslandMask(worldX, worldZ)));
                float sakura = EllipseMask(worldX + 92f, worldZ + 34f, 52f, 64f);
                float north = Mathf.Clamp01((worldZ - 22f) / 86f);
                maps[z, x, 0] = 0.55f;
                maps[z, x, 1] = Mathf.Clamp01(beach * 2.5f);
                maps[z, x, 2] = Mathf.Clamp01(plaza * 1.6f + PathMask(worldX, worldZ));
                maps[z, x, 3] = Mathf.Clamp01(north * 0.9f + sakura * 0.35f);
                NormalizeLayers(maps, z, x);
            }
        }
        data.SetAlphamaps(0, 0, maps);
    }

    private static float PathMask(float x, float z)
    {
        float result = 0f;
        result = Mathf.Max(result, SegmentDistanceMask(new Vector2(x, z), new Vector2(0, 0), new Vector2(0, 92), 8f));
        result = Mathf.Max(result, SegmentDistanceMask(new Vector2(x, z), new Vector2(0, 0), new Vector2(-96, 16), 8f));
        result = Mathf.Max(result, SegmentDistanceMask(new Vector2(x, z), new Vector2(0, 0), new Vector2(-38, -82), 8f));
        result = Mathf.Max(result, SegmentDistanceMask(new Vector2(x, z), new Vector2(0, 0), new Vector2(84, -18), 8f));
        result = Mathf.Max(result, SegmentDistanceMask(new Vector2(x, z), new Vector2(0, 0), new Vector2(78, 60), 8f));
        result = Mathf.Max(result, SegmentDistanceMask(new Vector2(x, z), new Vector2(84, -18), new Vector2(92, -92), 7f));
        return result;
    }

    private static void NormalizeLayers(float[,,] maps, int z, int x)
    {
        float total = 0f;
        for (int i = 0; i < 4; i++) total += maps[z, x, i];
        if (total <= 0f) maps[z, x, 0] = 1f;
        else for (int i = 0; i < 4; i++) maps[z, x, i] /= total;
    }

    private static void CreateOcean(Transform parent)
    {
        GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ocean.name = "Ocean_Surrounding_Island_WaterShader";
        ocean.transform.SetParent(parent, false);
        ocean.transform.position = new Vector3(0f, -3.05f, 0f);
        ocean.transform.localScale = new Vector3(1800f, 0.12f, 1800f);
        ocean.GetComponent<Renderer>().sharedMaterial = Materials["Ocean"];
        MarkStatic(ocean);

        GameObject shallow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shallow.name = "Ocean_Shallow_Turquoise_Gradient_Underlay";
        shallow.transform.SetParent(parent, false);
        shallow.transform.position = new Vector3(0f, -2.74f, 0f);
        shallow.transform.localScale = new Vector3(302f, 0.035f, 246f);
        shallow.GetComponent<Renderer>().sharedMaterial = Materials["Water"];
        MarkStatic(shallow);
    }

    private static void AddLighting(Transform parent)
    {
        GameObject lightingGroup = CreateChild(parent.gameObject, "Lighting_PostFX");
        GameObject sun = new GameObject("Directional_Sun_DayNight");
        sun.transform.SetParent(lightingGroup.transform, false);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        light.intensity = 1.55f;
        light.color = new Color(1f, 0.9f, 0.72f);
        BESDayNightCycle cycle = sun.AddComponent<BESDayNightCycle>();
        cycle.sun = light;
        cycle.timeOfDay = 11.2f;
        sun.transform.rotation = Quaternion.Euler(42f, 128f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.76f, 0.86f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.62f, 0.43f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.18f, 0.14f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.58f, 0.75f, 0.9f);
        RenderSettings.fogDensity = 0.0008f;

        GameObject reflection = new GameObject("Reflection_Probe_BakeReady");
        reflection.transform.SetParent(lightingGroup.transform, false);
        reflection.transform.position = new Vector3(0f, 25f, 0f);
        ReflectionProbe probe = reflection.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
        probe.size = new Vector3(260f, 80f, 260f);

        GameObject wind = new GameObject("Global_WindZone_Vegetation_And_Particles");
        wind.transform.SetParent(lightingGroup.transform, false);
        wind.transform.position = new Vector3(0f, 24f, 0f);
        wind.transform.rotation = Quaternion.Euler(18f, 35f, 0f);
        WindZone windZone = wind.AddComponent<WindZone>();
        windZone.mode = WindZoneMode.Directional;
        windZone.windMain = 0.35f;
        windZone.windTurbulence = 0.45f;
        windZone.windPulseMagnitude = 0.18f;
        windZone.windPulseFrequency = 0.5f;

        GameObject volumeObject = new GameObject("PostFX_GlobalVolume_Bloom_Color_Vignette");
        volumeObject.transform.SetParent(lightingGroup.transform, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "BES_Island_PostFX_Profile";
        Bloom bloom = profile.Add<Bloom>();
        bloom.intensity.Override(0.42f);
        bloom.threshold.Override(0.78f);
        ColorAdjustments color = profile.Add<ColorAdjustments>();
        color.postExposure.Override(0.18f);
        color.contrast.Override(22f);
        color.saturation.Override(26f);
        Vignette vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.18f);
        vignette.smoothness.Override(0.42f);
        string profilePath = $"{GeneratedRoot}/BES_Island_PostFX_Profile.asset";
        AssetDatabase.DeleteAsset(profilePath);
        AssetDatabase.CreateAsset(profile, profilePath);
        volume.profile = profile;
    }

    private static void AddPlayerAndCamera(Transform parent)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_Preview_Start_Capsule";
        player.transform.SetParent(parent, false);
        player.transform.position = new Vector3(0f, 8f, -18f);
        player.transform.localScale = new Vector3(1f, 1.15f, 1f);
        player.GetComponent<Renderer>().sharedMaterial = Materials["White"];
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2.2f;
        controller.radius = 0.45f;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(72f, 78f, -110f);
        cameraObject.transform.rotation = Quaternion.Euler(54f, -34f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 42f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 500f;

        AudioListener listener = cameraObject.AddComponent<AudioListener>();
        listener.enabled = true;
    }

    private static void BuildCentralPlaza(Transform props, Transform interactions, Transform npcs)
    {
        GameObject group = CreateChild(props.gameObject, "CentralPlaza_CENTER_SocialHub_CircularPlaza_Fountain");
        GameObject decor = CreateChild(group, "Zone_Decor");
        GameObject detail = CreateChild(group, "Visual_Detail");
        CreateDisc("Circular_Stone_Plaza", new Vector3(0f, 5.0f, 0f), 44f, 0.18f, Materials["Stone"], group.transform);
        CreateDisc("Inner_Fountain_Paving_Ring", new Vector3(0f, 5.18f, 0f), 18f, 0.12f, Materials["Sand"], group.transform);
        CreateDisc("Fountain_Shallow_Lake_Ring_Water", new Vector3(0f, 5.42f, 0f), 11.5f, 0.04f, Materials["Water"], group.transform);
        CreateStoneRing("Plaza_Stone_Ring_Outer_ModularPavers", Vector3.zero, 43f, 40, new Vector3(5.4f, 0.16f, 2.6f), 5.45f, detail.transform);
        CreateStoneRing("Plaza_Stone_Ring_Mid_ModularPavers", Vector3.zero, 30f, 30, new Vector3(4.7f, 0.14f, 2.15f), 5.56f, detail.transform);
        CreateStoneRing("Plaza_Stone_Ring_Fountain_ModularPavers", Vector3.zero, 14f, 20, new Vector3(3.2f, 0.12f, 1.7f), 5.7f, detail.transform);
        CreatePrefabRing("Plaza_Stone_Slab_Accent_Ring", "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Stone_Slab.prefab", Vector3.zero, 36f, 16, 5.72f, detail.transform, 0.9f, 1.25f);
        CreatePlazaEntryAssets(detail.transform);
        PlacePrefab("Assets/GVOZDY/Round Four-Tier Water Fountain/Prefabs/fountain_4_light.prefab", "Big_Central_Fountain", new Vector3(0f, 6.6f, 0f), Quaternion.identity, Vector3.one * 1.7f, group.transform, PrimitiveType.Cylinder, Materials["Water"]);
        CreateFountainWaterVfx("Fountain_Water_VFX_SoftMist", new Vector3(0f, 8.7f, 0f), detail.transform);

        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 28f, 5.6f, Mathf.Sin(a) * 28f);
            PlacePrefab("Assets/SIV/Wooden Bench/Prefabs/Wooden Bench.prefab", $"Plaza_Bench_{i:00}", p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f), Vector3.one * 1.2f, group.transform, PrimitiveType.Cube, Materials["Wood"]);
        }

        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 23f, 5.95f, Mathf.Sin(a) * 23f);
            CreatePlanter($"Plaza_Planter_FlowerCluster_{i:00}", p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), decor.transform);
            if (i % 2 == 0) CreateBanner($"Plaza_Color_Banner_{i:00}", new Vector3(Mathf.Cos(a) * 38f, 6.1f, Mathf.Sin(a) * 38f), -a * Mathf.Rad2Deg + 90f, decor.transform, i % 4 == 0);
        }

        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            CreateLantern($"Plaza_Warm_Lantern_{i:00}", new Vector3(Mathf.Cos(a) * 32f, 6.0f, Mathf.Sin(a) * 32f), decor.transform, 2.6f);
        }

        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Market Table Scene.prefab", "Plaza_Market_Stall_BlueCanopy", new Vector3(-25f, 6f, -20f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.05f, decor.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Kitchen Table Scene.prefab", "Plaza_Quest_Notice_Table_RedCanopy", new Vector3(26f, 6f, 18f), Quaternion.Euler(0f, -145f, 0f), Vector3.one * 1f, decor.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Wagon.prefab", "Plaza_Travel_Wagon_Prop", new Vector3(33f, 6f, -13f), Quaternion.Euler(0f, -62f, 0f), Vector3.one * 1.2f, decor.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Sign.prefab", "Plaza_Zone_Welcome_Sign", new Vector3(-18f, 6.2f, 31f), Quaternion.Euler(0f, 165f, 0f), Vector3.one * 1.15f, decor.transform, PrimitiveType.Cube, Materials["Wood"]);

        for (int i = 0; i < 28; i++)
        {
            float a = i * Mathf.PI * 2f / 28f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 35f, 5.55f, Mathf.Sin(a) * 35f);
            PlacePrefab(RandomFlower(), $"Plaza_FlowerBed_{i:00}", p, Quaternion.Euler(0f, Rand(0f, 360f), 0f), Vector3.one * Rand(0.8f, 1.3f), group.transform, PrimitiveType.Sphere, Materials["DeepGrass"]);
        }

        CreateNpc("Social_NPC_Welcome_Guide", BESNpcRole.QuestGiver, new Vector3(-9f, 6f, -10f), npcs, "Welcome guide and quest board");
        CreateNpc("Social_NPC_Emote_Host", BESNpcRole.Social, new Vector3(13f, 6f, 8f), npcs, "Social hub emote host");
        CreateInteraction("Player_Spawn_Center_Plaza", BESInteractionType.Spawn, new Vector3(0f, 7f, -18f), new Vector3(4f, 2f, 4f), interactions, "Default player spawn/social hub");
        CreateInteraction("Emote_Circle_Plaza", BESInteractionType.Emote, new Vector3(18f, 6f, 12f), new Vector3(8f, 2f, 8f), interactions, "Group emote point");
    }

    private static void BuildNorthForest(Transform props, Transform interactions, Transform npcs, Transform animals, Transform vfx)
    {
        GameObject group = CreateChild(props.gameObject, "Forest_North_DenseGreenForest_Resources");
        GameObject detail = CreateChild(group, "Forest_Detail_DynamicVegetation");
        ScatterPrefabs(group.transform, TreePrefabs(), new Vector2(0f, 98f), new Vector2(88f, 42f), 112, 1.05f, 2.25f);
        ScatterPrefabs(group.transform, TreePrefabs(), new Vector2(-28f, 112f), new Vector2(42f, 22f), 38, 1.4f, 2.45f);
        ScatterPrefabs(group.transform, TreePrefabs(), new Vector2(42f, 100f), new Vector2(35f, 24f), 32, 1.25f, 2.2f);
        ScatterPrefabs(group.transform, RockPrefabs(), new Vector2(0f, 88f), new Vector2(90f, 46f), 28, 0.8f, 1.5f);
        ScatterPrefabs(group.transform, BushPrefabs(), new Vector2(0f, 74f), new Vector2(74f, 34f), 46, 0.8f, 1.4f);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Big Tree.prefab", "Forest_North_Landmark_AncientTree", new Vector3(-20f, 9.4f, 108f), Quaternion.Euler(0f, -20f, 0f), Vector3.one * 1.85f, group.transform, PrimitiveType.Capsule, Materials["DeepGrass"]);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Altar.prefab", "Forest_North_Resource_Altar", new Vector3(24f, 8.9f, 94f), Quaternion.Euler(0f, 24f, 0f), Vector3.one * 1.1f, detail.transform, PrimitiveType.Cube, Materials["Stone"]);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Sign.prefab", "Forest_North_Gathering_Sign", new Vector3(-11f, 7.4f, 54f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, detail.transform, PrimitiveType.Cube, Materials["Wood"]);
        CreateClusteredVegetation("Forest_Detail_Understory_Left", new Vector2(-38f, 80f), new Vector2(28f, 22f), 48, detail.transform);
        CreateClusteredVegetation("Forest_Detail_Understory_Right", new Vector2(42f, 86f), new Vector2(30f, 24f), 52, detail.transform);
        ScatterPrefabs(detail.transform, ResourcePrefabs(), new Vector2(-8f, 82f), new Vector2(54f, 28f), 36, 0.55f, 1.1f);
        CreateFenceCurve("Forest_North_Soft_Fence_Line_West", new Vector3(-52f, 7.1f, 46f), new Vector3(-69f, 8.2f, 104f), 8, detail.transform);
        CreateFenceCurve("Forest_North_Soft_Fence_Line_East", new Vector3(49f, 7.4f, 52f), new Vector3(66f, 9.5f, 104f), 8, detail.transform);
        for (int i = 0; i < 10; i++) CreateLantern($"Forest_Path_Guide_Lantern_{i:00}", new Vector3(Rand(-16f, 18f), 8f + Rand(0f, 1.4f), 45f + i * 6.2f), detail.transform, 1.9f);

        CreateSpawnerMarker("AnimalSpawner_Deer_NorthForest", new Vector3(-26f, 9f, 92f), new Vector3(26f, 4f, 20f), animals, "Deer ambient spawn zone, north forest only");
        CreateSpawnerMarker("AnimalSpawner_Rabbit_NorthForest", new Vector3(32f, 8f, 78f), new Vector3(28f, 3f, 18f), animals, "Rabbit ambient spawn zone, north forest only");
        CreateSpawnerMarker("AnimalSpawner_Bird_NorthForest", new Vector3(2f, 16f, 118f), new Vector3(70f, 16f, 26f), animals, "Bird ambient spawn zone and flight layer");
        CreateBirdAudio("BirdAudio_NorthForest", new Vector3(0f, 13f, 92f), vfx);

        for (int i = 0; i < 5; i++) CreateAnimal($"Deer_{i:00}", new Vector3(Rand(-42f, 48f), 9f, Rand(60f, 116f)), animals, 1.5f, new Color(0.58f, 0.34f, 0.17f));
        for (int i = 0; i < 7; i++) CreateAnimal($"Rabbit_{i:00}", new Vector3(Rand(-52f, 52f), 8f, Rand(50f, 108f)), animals, 0.55f, Color.white);

        for (int i = 0; i < 6; i++)
        {
            CreateInteraction($"Resource_Gathering_Spot_{i:00}", BESInteractionType.ResourceGathering, new Vector3(Rand(-58f, 58f), 8f, Rand(52f, 118f)), new Vector3(5f, 3f, 5f), interactions, "Herbs, wood, mushroom and exploration resource node");
        }
    }

    private static void BuildCherryForest(Transform props, Transform interactions, Transform npcs, Transform vfx)
    {
        GameObject group = CreateChild(props.gameObject, "CherryGrove_West_Torii_PhotoSpots");
        GameObject detail = CreateChild(group, "Sakura_Detail_LayeredPetals_Rocks_Flowers");
        for (int i = 0; i < 58; i++)
        {
            float ribbon = i / 57f;
            Vector2 centerLine = new Vector2(Mathf.Lerp(-122f, -70f, ribbon), Mathf.Sin(ribbon * Mathf.PI * 2.1f) * 27f - 8f);
            Vector2 p = centerLine + RandomInEllipse(Vector2.zero, new Vector2(16f, 18f));
            CreateSakuraTree($"Sakura_Tree_{i:00}", new Vector3(p.x, 7f, p.y), Rand(0.8f, 1.5f), group.transform);
        }
        CreateDisc("Sakura_Petal_Ground_Wash_A", new Vector3(-86f, 5.82f, 4f), 44f, 0.025f, Materials["PetalGround"], detail.transform);
        CreateDisc("Sakura_Petal_Ground_Wash_B", new Vector3(-112f, 5.88f, 30f), 23f, 0.025f, Materials["PetalGround"], detail.transform);
        CreateClusteredVegetation("Sakura_Detail_Pink_Flower_Carpet_A", new Vector2(-92f, -14f), new Vector2(36f, 26f), 64, detail.transform);
        CreateClusteredVegetation("Sakura_Detail_Pink_Flower_Carpet_B", new Vector2(-108f, 26f), new Vector2(22f, 30f), 42, detail.transform);
        ScatterPrefabs(detail.transform, RockPrefabs(), new Vector2(-92f, -4f), new Vector2(50f, 58f), 22, 0.45f, 1.05f);

        CreateTorii("Japanese_Torii_Gate_Photo_Entrance", new Vector3(-82f, 6.2f, -42f), Quaternion.Euler(0f, 25f, 0f), group.transform);
        PlacePrefab("Assets/SIV/Stone Bench/Prefabs/Stone Bench.prefab", "Sakura_Romantic_Stone_Bench", new Vector3(-106f, 6.7f, 17f), Quaternion.Euler(0f, 112f, 0f), Vector3.one * 1.1f, detail.transform, PrimitiveType.Cube, Materials["Stone"]);
        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Round Table with Tablecloth.prefab", "Sakura_Photo_Picnic_Table", new Vector3(-96f, 6.7f, 24f), Quaternion.Euler(0f, -18f, 0f), Vector3.one * 0.9f, detail.transform, PrimitiveType.Cylinder, Materials["White"]);
        PlacePrefab("Assets/COMICOMI/VFX_FallingCherryBlossom/Art/Prefabs/FX_huaBan.prefab", "Sakura_Petal_VFX_Ambience", new Vector3(-76f, 11f, 0f), Quaternion.identity, Vector3.one * 1.8f, vfx, PrimitiveType.Sphere, Materials["SakuraPink"]);
        PlacePrefab("Assets/COMICOMI/VFX_FallingCherryBlossom/Art/Prefabs/FX_huaBan.prefab", "Sakura_Petal_VFX_PhotoSpot_SoftLayer", new Vector3(-103f, 10f, 24f), Quaternion.identity, Vector3.one * 1.25f, vfx, PrimitiveType.Sphere, Materials["SakuraPink"]);
        CreateLantern("Sakura_Torii_Warm_Lantern_Left", new Vector3(-87f, 6.6f, -45f), detail.transform, 2.2f);
        CreateLantern("Sakura_Torii_Warm_Lantern_Right", new Vector3(-77f, 6.6f, -39f), detail.transform, 2.2f);
        CreateNpc("Photographer_NPC_Sakura", BESNpcRole.Photographer, new Vector3(-72f, 7f, -25f), npcs, "Photo spot NPC for sakura forest");
        CreateInteraction("PhotoSpot_Sakura_Torii", BESInteractionType.PhotoSpot, new Vector3(-82f, 7f, -42f), new Vector3(8f, 4f, 8f), interactions, "Seasonal sakura and torii photo point");
        CreateInteraction("Sitting_Sakura_Picnic", BESInteractionType.Sitting, new Vector3(-100f, 7f, 20f), new Vector3(8f, 3f, 8f), interactions, "Relaxing social sitting area");
    }

    private static void BuildSouthPool(Transform props, Transform interactions, Transform npcs)
    {
        GameObject group = CreateChild(props.gameObject, "PoolArea_South_SwimmingPool_Recreation");
        GameObject detail = CreateChild(group, "Pool_Detail_StoneCoping_Umbrellas_Flowers");
        CreateBox("Pool_Water", new Vector3(-36f, 5.2f, -82f), new Vector3(44f, 0.25f, 28f), Materials["Water"], group.transform);
        CreateBox("Pool_Stone_Deck", new Vector3(-36f, 5.05f, -82f), new Vector3(54f, 0.18f, 38f), Materials["Stone"], group.transform);
        CreatePoolCoping("Pool_Stone_Coping_Modular_Edge", new Vector3(-36f, 5.5f, -82f), 26f, 18f, detail.transform);
        PlacePrefab("Assets/AureDevGames/Water Stylized Shader Orto & Perspective Camera/Prefabs/BeachBall.prefab", "Pool_Detail_BeachBall", new Vector3(-21f, 5.9f, -79f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 0.85f, detail.transform, PrimitiveType.Sphere, Materials["BannerRed"]);
        PlacePrefab("Assets/3D_Game_Assets_Flora/Prefap/Lotus_leaves_Prefap.prefab", "Pool_Detail_Lotus_Leaves", new Vector3(-49f, 5.7f, -80f), Quaternion.identity, Vector3.one * 1.25f, detail.transform, PrimitiveType.Sphere, Materials["LeafLight"]);
        PlacePrefab("Assets/NamuFX/StylizedWaterEffects/Prefabs/Bubbles_Vertical_Loop.prefab", "Pool_Detail_Bubble_VFX", new Vector3(-36f, 6.1f, -82f), Quaternion.identity, Vector3.one * 0.8f, detail.transform, PrimitiveType.Sphere, Materials["Foam"]);

        for (int i = 0; i < 8; i++)
        {
            Vector3 p = new Vector3(-62f + i * 7.5f, 6f, -108f + (i % 2) * 6f);
            CreateBox($"Pool_Lounge_Chair_{i:00}", p, new Vector3(4f, 0.35f, 1.5f), Materials["Wood"], group.transform);
            CreateUmbrella($"Pool_Umbrella_{i:00}", p + new Vector3(1.5f, 0.1f, 2.4f), group.transform);
            CreateInteraction($"Sitting_Pool_Chair_{i:00}", BESInteractionType.Sitting, p + Vector3.up, new Vector3(3f, 2f, 3f), interactions, "Pool lounge sitting point");
        }
        CreateClusteredVegetation("Pool_Detail_Tropical_Flower_Border", new Vector2(-36f, -105f), new Vector2(38f, 8f), 38, detail.transform);
        CreateLantern("Pool_Evening_Lantern_West", new Vector3(-64f, 6.0f, -77f), detail.transform, 2.2f);
        CreateLantern("Pool_Evening_Lantern_East", new Vector3(-8f, 6.0f, -85f), detail.transform, 2.2f);

        CreateNpc("Recreation_NPC_Minigame", BESNpcRole.Social, new Vector3(-12f, 6f, -94f), npcs, "Pool minigame and relaxation NPC");
    }

    private static void BuildEastHarbor(Transform props, Transform interactions, Transform npcs)
    {
        GameObject group = CreateChild(props.gameObject, "FishingPier_East_Docks_Boats");
        GameObject detail = CreateChild(group, "Harbor_Detail_Posts_Lanterns_Cargo");
        CreateDockLine("Harbor_Main_Dock", new Vector3(58f, 4.8f, -18f), new Vector3(105f, 3.9f, -34f), 18, group.transform);
        CreateDockLine("Harbor_Side_Dock", new Vector3(74f, 4.8f, -26f), new Vector3(78f, 3.9f, -64f), 12, group.transform);
        CreateDockPosts("Harbor_Detail_MainDock_Posts", new Vector3(58f, 4.8f, -18f), new Vector3(105f, 3.9f, -34f), 9, detail.transform);
        CreateDockPosts("Harbor_Detail_SideDock_Posts", new Vector3(74f, 4.8f, -26f), new Vector3(78f, 3.9f, -64f), 7, detail.transform);
        PlacePrefab("Assets/Stylized_Pirate_Ship/StylShip_Unity.prefab", "Fishing_Boat_Interactable", new Vector3(96f, 2.6f, -56f), Quaternion.Euler(0f, -35f, 0f), Vector3.one * 0.25f, group.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Fish.prefab", "Harbor_Fish_Props", new Vector3(72f, 6f, -21f), Quaternion.identity, Vector3.one * 1.4f, group.transform, PrimitiveType.Cube, Materials["White"]);
        PlacePrefab("Assets/Stylized_Pirate_Ship/StylShip_Barrel.prefab", "Harbor_Detail_Barrel_Stack_A", new Vector3(62f, 5.7f, -15f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 0.9f, detail.transform, PrimitiveType.Cylinder, Materials["Wood"]);
        PlacePrefab("Assets/Stylized_Pirate_Ship/StylShip_Box.prefab", "Harbor_Detail_Cargo_Crates", new Vector3(70f, 5.6f, -19f), Quaternion.Euler(0f, -18f, 0f), Vector3.one * 0.8f, detail.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/Stylized_Pirate_Ship/StylShip_Pallet.prefab", "Harbor_Detail_Pallet_Stack", new Vector3(80f, 5.15f, -25f), Quaternion.Euler(0f, 12f, 0f), Vector3.one * 0.75f, detail.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/Stylized_Pirate_Ship/StylShip_Cannon.prefab", "Harbor_Detail_Old_Cannon_Decor", new Vector3(89f, 5.0f, -31f), Quaternion.Euler(0f, 64f, 0f), Vector3.one * 0.55f, detail.transform, PrimitiveType.Cube, Materials["StoneDark"]);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Sign.prefab", "Harbor_Fishing_Sign", new Vector3(57f, 6f, -12f), Quaternion.Euler(0f, 130f, 0f), Vector3.one * 0.9f, detail.transform, PrimitiveType.Cube, Materials["Wood"]);
        CreateRopeLine("Harbor_Detail_Rope_Guide", new Vector3(59f, 6.5f, -15f), new Vector3(101f, 5.6f, -31f), 8, detail.transform);
        CreateLantern("Harbor_Detail_Lantern_Entry", new Vector3(58f, 6.1f, -18f), detail.transform, 2.4f);
        CreateLantern("Harbor_Detail_Lantern_End", new Vector3(101f, 5.1f, -33f), detail.transform, 2.0f);

        CreateNpc("Fisherman_NPC_Harbor", BESNpcRole.Fisherman, new Vector3(66f, 6f, -18f), npcs, "Fishing tutorial, daily quest and fish shop");
        for (int i = 0; i < 4; i++)
        {
            CreateInteraction($"Fishing_MinGame_Point_{i:00}", BESInteractionType.Fishing, new Vector3(78f + i * 7f, 5f, -30f - i * 5f), new Vector3(5f, 4f, 5f), interactions, "Fishing minigame trigger");
        }
        CreateInteraction("Boat_Interaction_Harbor", BESInteractionType.Boat, new Vector3(96f, 5f, -56f), new Vector3(8f, 4f, 8f), interactions, "Boat travel or boating interaction");
    }

    private static void BuildNortheastViewpoint(Transform props, Transform interactions, Transform npcs)
    {
        GameObject group = CreateChild(props.gameObject, "ViewPoint_NE_OceanCliff_Gazebo");
        GameObject detail = CreateChild(group, "Viewpoint_Detail_Railings_PhotoSet");
        CreateDisc("Circular_Wood_Viewing_Platform", new Vector3(83f, 17.6f, 57f), 20f, 0.35f, Materials["Wood"], group.transform);
        CreateRailings("Viewpoint_Circular_Platform_Railings", new Vector3(83f, 18.8f, 57f), 20f, 28, detail.transform);
        CreateGazebo("Gazebo_Cutscene_Viewpoint", new Vector3(83f, 18f, 57f), group.transform);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Arch.prefab", "Viewpoint_Cliff_Entry_Arch", new Vector3(66f, 17.2f, 47f), Quaternion.Euler(0f, 60f, 0f), Vector3.one * 1.25f, detail.transform, PrimitiveType.Cube, Materials["Stone"]);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Street_Lamp.prefab", "Viewpoint_Street_Lamp_Warm", new Vector3(93f, 18.7f, 49f), Quaternion.Euler(0f, -25f, 0f), Vector3.one * 1.1f, detail.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/SIV/Stone Bench/Prefabs/Stone Bench.prefab", "Viewpoint_Ocean_Bench", new Vector3(86f, 18.45f, 43f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, detail.transform, PrimitiveType.Cube, Materials["Stone"]);
        CreateLantern("Viewpoint_Gazebo_Lantern_A", new Vector3(74f, 18.5f, 57f), detail.transform, 2.2f);
        CreateLantern("Viewpoint_Gazebo_Lantern_B", new Vector3(92f, 18.5f, 57f), detail.transform, 2.2f);
        CreatePlanter("Viewpoint_Photo_Flower_Planter", new Vector3(83f, 18.4f, 44f), Quaternion.identity, detail.transform);
        CreateNpc("Viewpoint_NPC_Cutscene_Guide", BESNpcRole.Photographer, new Vector3(75f, 18.3f, 49f), npcs, "Photo and ocean cutscene trigger guide");
        CreateInteraction("PhotoSpot_Ocean_Viewpoint", BESInteractionType.PhotoSpot, new Vector3(83f, 18.6f, 57f), new Vector3(14f, 4f, 14f), interactions, "Ocean panorama photo and cutscene location");
    }

    private static void BuildSoutheastCampingIsland(Transform props, Transform interactions, Transform npcs)
    {
        GameObject group = CreateChild(props.gameObject, "CampIsland_SE_SeparatedCamping_Bridges");
        GameObject detail = CreateChild(group, "Camping_Detail_Props_Lanterns_Food");
        PlacePrefab("Assets/PolygonPilots/Campfire/Prefabs/Tent.prefab", "Camping_Tent_Main", new Vector3(112f, 6f, -108f), Quaternion.Euler(0f, -35f, 0f), Vector3.one * 1.5f, group.transform, PrimitiveType.Cube, Materials["White"]);
        PlacePrefab("Assets/PolygonPilots/Campfire/Prefabs/Tent.prefab", "Camping_Tent_Secondary", new Vector3(88f, 5.8f, -116f), Quaternion.Euler(0f, 20f, 0f), Vector3.one * 1.1f, group.transform, PrimitiveType.Cube, Materials["White"]);
        PlacePrefab("Assets/PolygonPilots/Campfire/Prefabs/Tent.prefab", "Camping_Tent_Photo_Backdrop", new Vector3(126f, 6.1f, -116f), Quaternion.Euler(0f, -82f, 0f), Vector3.one * 1.0f, group.transform, PrimitiveType.Cube, Materials["White"]);
        PlacePrefab("Assets/PolygonPilots/Campfire/Prefabs/CampFire.prefab", "Camping_Campfire_SocialHub", new Vector3(101f, 6.3f, -91f), Quaternion.identity, Vector3.one * 1.4f, group.transform, PrimitiveType.Cylinder, Materials["ToriiRed"]);
        PlacePrefab("Assets/FullOpaqueFire/Prefabs/VFX/VFX_FullOpaqueFire.prefab", "Camping_Campfire_Flame_VFX", new Vector3(101f, 7.0f, -91f), Quaternion.identity, Vector3.one * 0.75f, detail.transform, PrimitiveType.Sphere, Materials["FireGlow"]);
        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Market Table Scene.prefab", "Camping_Merchant_Table_FoodProps", new Vector3(123f, 6.2f, -88f), Quaternion.Euler(0f, -65f, 0f), Vector3.one * 1.2f, group.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Wagon.prefab", "Camping_Supply_Wagon", new Vector3(130f, 6.2f, -100f), Quaternion.Euler(0f, -120f, 0f), Vector3.one * 0.9f, detail.transform, PrimitiveType.Cube, Materials["Wood"]);
        PlacePrefab("Assets/Stylized 3D Animated Chests – FREE Demo/URP/Chsets/Crowned Chest/CrownedChest_PF_URP.prefab", "Camping_Detail_Reward_Chest", new Vector3(95f, 6.55f, -83f), Quaternion.Euler(0f, 32f, 0f), Vector3.one * 0.75f, detail.transform, PrimitiveType.Cube, Materials["Gold"]);

        ScatterPrefabs(group.transform, TreePrefabs(), new Vector2(104f, -108f), new Vector2(42f, 25f), 34, 0.8f, 1.45f);
        ScatterPrefabs(group.transform, BushPrefabs(), new Vector2(104f, -108f), new Vector2(46f, 28f), 30, 0.55f, 1.05f);
        ScatterPrefabs(group.transform, RockPrefabs(), new Vector2(104f, -108f), new Vector2(48f, 32f), 16, 0.6f, 1.4f);
        CreateClusteredVegetation("Camping_Detail_Wildflower_Rim", new Vector2(108f, -102f), new Vector2(38f, 24f), 36, detail.transform);
        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Pie.prefab", "Camping_Detail_Food_Pie", new Vector3(116f, 6.8f, -84f), Quaternion.Euler(0f, 30f, 0f), Vector3.one * 1.1f, detail.transform, PrimitiveType.Cylinder, Materials["White"]);
        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Fish.prefab", "Camping_Detail_Food_Fish", new Vector3(109f, 6.8f, -86f), Quaternion.Euler(0f, -20f, 0f), Vector3.one * 1f, detail.transform, PrimitiveType.Cube, Materials["White"]);
        PlacePrefab("Assets/Patchmesh/Free Stylized Hand-Painted Cozy Kitchen & Market Scene Sample/Prefabs/Sourdough 1.prefab", "Camping_Detail_Food_Bread", new Vector3(121f, 6.85f, -84f), Quaternion.Euler(0f, 18f, 0f), Vector3.one * 0.9f, detail.transform, PrimitiveType.Cube, Materials["White"]);
        CreateFenceCurve("Camping_Detail_Fence_Back_Rim", new Vector3(78f, 6.1f, -104f), new Vector3(128f, 6.2f, -129f), 9, detail.transform);
        CreateLantern("Camping_Detail_Camp_Lantern_A", new Vector3(92f, 6.6f, -97f), detail.transform, 2.1f);
        CreateLantern("Camping_Detail_Camp_Lantern_B", new Vector3(119f, 6.6f, -101f), detail.transform, 2.1f);
        CreateLantern("Camping_Detail_Dock_Lantern", new Vector3(86f, 5.5f, -125f), detail.transform, 2.0f);

        CreateNpc("Merchant_NPC_CampingIsland", BESNpcRole.Merchant, new Vector3(120f, 7f, -91f), npcs, "Camp supplies merchant");
        CreateNpc("Photographer_NPC_CampingIsland", BESNpcRole.Photographer, new Vector3(92f, 7f, -88f), npcs, "Camping island photo NPC");
        CreateNpc("Fisherman_NPC_CampingIsland", BESNpcRole.Fisherman, new Vector3(83f, 6f, -126f), npcs, "Camping island fishing NPC; no boat prop here by design");
        CreateInteraction("Sitting_Campfire_Circle", BESInteractionType.Sitting, new Vector3(101f, 7f, -91f), new Vector3(12f, 3f, 12f), interactions, "Campfire social sitting ring");
        CreateInteraction("PhotoSpot_CampingIsland", BESInteractionType.PhotoSpot, new Vector3(93f, 7f, -88f), new Vector3(8f, 4f, 8f), interactions, "Camping memory photo point");
    }

    private static void BuildPathsAndBridges(Transform props)
    {
        GameObject group = CreateChild(props.gameObject, "Stone_Paths_And_Wooden_Bridges");
        CreateCurvedStonePath("Path_Center_To_NorthForest", group.transform, new Vector3(0f, 5.7f, 38f), new Vector3(-16f, 7.4f, 68f), new Vector3(0f, 8.7f, 100f), 26);
        CreateCurvedStonePath("Path_Center_To_SakuraWest", group.transform, new Vector3(-38f, 5.7f, 4f), new Vector3(-70f, 6.5f, -8f), new Vector3(-98f, 7f, -24f), 22);
        CreateCurvedStonePath("Path_Center_To_PoolSouth", group.transform, new Vector3(-16f, 5.7f, -38f), new Vector3(-34f, 5.9f, -54f), new Vector3(-40f, 6f, -72f), 16);
        CreateCurvedStonePath("Path_Center_To_HarborEast", group.transform, new Vector3(38f, 5.7f, -8f), new Vector3(54f, 5.7f, -8f), new Vector3(68f, 5.5f, -23f), 14);
        CreateCurvedStonePath("Path_Center_To_ViewpointNE", group.transform, new Vector3(35f, 6f, 30f), new Vector3(58f, 10f, 42f), new Vector3(80f, 18f, 55f), 20);
        CreateDockLine("Bridge_To_Camping_Island_A", new Vector3(72f, 4.6f, -64f), new Vector3(90f, 4.7f, -84f), 12, group.transform);
        CreateDockLine("Bridge_To_Camping_Island_B", new Vector3(90f, 4.7f, -84f), new Vector3(102f, 5.1f, -92f), 8, group.transform);
    }

    private static void BuildCoastline(Transform props)
    {
        GameObject group = CreateChild(props.gameObject, "Rocky_Coastline_Beaches");
        GameObject foam = CreateChild(group, "Coast_Foam_Wave_Strips");
        for (int i = 0; i < 95; i++)
        {
            float a = i * Mathf.PI * 2f / 95f;
            Vector3 p = IrregularBoundaryPoint(Vector2.zero, 136f + Rand(-4f, 5f), 112f + Rand(-4f, 5f), a, 4.2f + Rand(-1f, 2f));
            PlacePrefab(RandomRock(), $"MainIsland_Coast_Rock_{i:00}", p, Quaternion.Euler(0f, Rand(0f, 360f), 0f), Vector3.one * Rand(0.9f, 2.2f), group.transform, PrimitiveType.Sphere, Materials["Stone"]);
            if (i % 2 == 0)
            {
                Vector3 foamPosition = IrregularBoundaryPoint(Vector2.zero, 143f, 119f, a, -2.2f);
                CreateFoamPatch($"Coast_Foam_MainIsland_{i:00}", foamPosition, a * Mathf.Rad2Deg, Rand(5.5f, 10f), foam.transform);
            }
        }
        for (int i = 0; i < 36; i++)
        {
            float a = i * Mathf.PI * 2f / 36f;
            Vector3 p = IrregularBoundaryPoint(new Vector2(96f, -104f), 64f, 46f, a, 3.8f);
            PlacePrefab(RandomRock(), $"CampingIsland_Coast_Rock_{i:00}", p, Quaternion.Euler(0f, Rand(0f, 360f), 0f), Vector3.one * Rand(0.8f, 1.8f), group.transform, PrimitiveType.Sphere, Materials["Stone"]);
            if (i % 2 == 0)
            {
                Vector3 foamPosition = IrregularBoundaryPoint(new Vector2(96f, -104f), 69f, 51f, a, -2.18f);
                CreateFoamPatch($"Coast_Foam_CampingIsland_{i:00}", foamPosition, a * Mathf.Rad2Deg, Rand(4.5f, 8f), foam.transform);
            }
        }
        CreateCliffRockCluster("Coast_Cliff_NE_Viewpoint_RockWall", new Vector2(88f, 58f), new Vector2(35f, 24f), 34, 13.5f, group.transform);
        CreateCliffRockCluster("Coast_Cliff_East_Harbor_RockWall", new Vector2(112f, -28f), new Vector2(26f, 42f), 30, 5.2f, group.transform);
        CreateCliffRockCluster("Coast_Cliff_SW_Pool_RockWall", new Vector2(-72f, -90f), new Vector2(38f, 22f), 28, 5.4f, group.transform);
        for (int i = 0; i < 18; i++)
        {
            float a = Mathf.Lerp(0.1f, 1.35f, i / 17f);
            CreateFoamPatch($"Coast_Foam_NE_Cliff_Extra_{i:00}", new Vector3(88f + Mathf.Cos(a) * 54f, -2.05f, 58f + Mathf.Sin(a) * 40f), a * Mathf.Rad2Deg, Rand(7.5f, 13f), foam.transform);
        }
    }

    private static void BuildFlyingBirds(Transform vfx)
    {
        GameObject center = new GameObject("Bird_Orbit_Center");
        center.transform.SetParent(vfx, false);
        center.transform.position = new Vector3(20f, 28f, 20f);

        for (int i = 0; i < 8; i++)
        {
            GameObject bird = CreateBird($"Flying_Bird_{i:00}", center.transform.position, vfx);
            BESFlyingBirds flight = bird.AddComponent<BESFlyingBirds>();
            flight.orbitCenter = center.transform;
            flight.radius = Rand(42f, 86f);
            flight.heightOffset = Rand(10f, 22f);
            flight.speed = Rand(8f, 18f);
        }
    }

    private static void BuildOptimizationMarkers(Transform parent)
    {
        GameObject navRoot = CreateChild(parent.gameObject, "NavMeshReady_SurfaceMarker");
        Type navMeshSurfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation.Runtime");
        if (navMeshSurfaceType != null) navRoot.AddComponent(navMeshSurfaceType);
        navRoot.AddComponent<LODGroup>();
        navRoot.AddComponent<OcclusionArea>().size = new Vector3(420f, 70f, 390f);
    }

    private static void AddTerrainGrassDetails(TerrainData data)
    {
        Texture2D grassTexture = CreateSolidTextureAsset("T_BES_DetailGrass", new Color(0.38f, 0.68f, 0.24f, 1f));
        DetailPrototype grass = new DetailPrototype
        {
            prototypeTexture = grassTexture,
            renderMode = DetailRenderMode.GrassBillboard,
            minWidth = 0.8f,
            maxWidth = 1.7f,
            minHeight = 0.6f,
            maxHeight = 1.45f,
            noiseSeed = 1609,
            noiseSpread = 0.45f,
            healthyColor = new Color(0.42f, 0.72f, 0.25f),
            dryColor = new Color(0.74f, 0.68f, 0.38f)
        };

        data.detailPrototypes = new[] { grass };
        data.SetDetailResolution(256, 16);
        int[,] density = new int[data.detailHeight, data.detailWidth];
        for (int y = 0; y < data.detailHeight; y++)
        {
            for (int x = 0; x < data.detailWidth; x++)
            {
                float worldX = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, x / (float)(data.detailWidth - 1));
                float worldZ = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, y / (float)(data.detailHeight - 1));
                float island = Mathf.Max(IslandMask(worldX, worldZ), CampIslandMask(worldX, worldZ));
                float plazaClear = Mathf.Clamp01(1f - new Vector2(worldX, worldZ).magnitude / 54f);
                float pathClear = PathMask(worldX, worldZ);
                float noise = Mathf.PerlinNoise(worldX * 0.08f + 5f, worldZ * 0.08f + 9f);
                density[y, x] = island > 0.08f && plazaClear < 0.15f && pathClear < 0.18f && noise > 0.38f ? Mathf.RoundToInt(Mathf.Lerp(1f, 7f, noise)) : 0;
            }
        }
        data.SetDetailLayer(0, 0, 0, density);
    }

    private static Texture2D CreateSolidTextureAsset(string name, Color color)
    {
        string path = $"{GeneratedRoot}/{name}.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!texture)
        {
            texture = new Texture2D(8, 8, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
        }
        return texture;
    }

    private static void CreateStoneRing(string name, Vector3 center, float radius, int count, Vector3 stoneScale, float y, Transform parent)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = $"Plaza_Stone_Ring_Paver_{i:00}";
            stone.transform.SetParent(ring.transform, false);
            stone.transform.position = p;
            stone.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f + Rand(-5f, 5f), 0f);
            stone.transform.localScale = stoneScale * Rand(0.9f, 1.12f);
            stone.GetComponent<Renderer>().sharedMaterial = i % 3 == 0 ? Materials["StoneDark"] : Materials["Stone"];
            MarkStatic(stone);
        }
    }

    private static void CreatePlanter(string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject planter = new GameObject(name);
        planter.transform.SetParent(parent, false);
        planter.transform.SetPositionAndRotation(position, rotation);
        CreateBox("Wood_Base", Vector3.zero, new Vector3(3.2f, 0.55f, 1.45f), Materials["Wood"], planter.transform);
        for (int i = 0; i < 5; i++)
        {
            Vector3 offset = new Vector3(Rand(-1.15f, 1.15f), 0.45f, Rand(-0.42f, 0.42f));
            PlacePrefab(RandomFlower(), $"Planter_Flower_{i:00}", offset, Quaternion.Euler(0f, Rand(0f, 360f), 0f), Vector3.one * Rand(0.45f, 0.8f), planter.transform, PrimitiveType.Sphere, Materials["LeafLight"]);
        }
        MarkStatic(planter);
    }

    private static void CreateBanner(string name, Vector3 position, float yaw, Transform parent, bool blue)
    {
        GameObject banner = new GameObject(name);
        banner.transform.SetParent(parent, false);
        banner.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        CreateBox("Pole_Left", new Vector3(-1.1f, 1.7f, 0f), new Vector3(0.16f, 3.4f, 0.16f), Materials["Wood"], banner.transform);
        CreateBox("Pole_Right", new Vector3(1.1f, 1.7f, 0f), new Vector3(0.16f, 3.4f, 0.16f), Materials["Wood"], banner.transform);
        CreateBox("Cloth", new Vector3(0f, 2.75f, 0f), new Vector3(2.35f, 1.25f, 0.08f), blue ? Materials["BannerBlue"] : Materials["BannerRed"], banner.transform);
        CreateBox("Gold_Trim", new Vector3(0f, 3.42f, -0.01f), new Vector3(2.45f, 0.13f, 0.1f), Materials["Gold"], banner.transform);
        MarkStatic(banner);
    }

    private static void CreateLantern(string name, Vector3 position, Transform parent, float height)
    {
        GameObject lantern = new GameObject(name);
        lantern.transform.SetParent(parent, false);
        lantern.transform.position = position;
        CreateBox("Post", Vector3.up * height * 0.5f, new Vector3(0.18f, height, 0.18f), Materials["Wood"], lantern.transform);
        CreateBox("Arm", new Vector3(0.48f, height - 0.25f, 0f), new Vector3(1f, 0.13f, 0.13f), Materials["Wood"], lantern.transform);
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "Warm_Glow_NoRealtimeLight";
        glow.transform.SetParent(lantern.transform, false);
        glow.transform.localPosition = new Vector3(1f, height - 0.58f, 0f);
        glow.transform.localScale = Vector3.one * 0.42f;
        glow.GetComponent<Renderer>().sharedMaterial = Materials["LanternWarm"];
        MarkStatic(lantern);
    }

    private static void CreateClusteredVegetation(string name, Vector2 center, Vector2 radii, int count, Transform parent)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent, false);
        for (int i = 0; i < count; i++)
        {
            Vector2 p = RandomInEllipse(center, radii);
            bool flower = i % 3 != 0;
            string prefab = flower ? RandomFlower() : BushPrefabs()[Rng.Next(BushPrefabs().Length)];
            float y = center.y < -70f ? 6.0f : 7.0f + Rand(-0.2f, 1.2f);
            PlacePrefab(prefab, $"{name}_Plant_{i:00}", new Vector3(p.x, y, p.y), Quaternion.Euler(0f, Rand(0f, 360f), 0f), Vector3.one * Rand(0.35f, 0.95f), cluster.transform, PrimitiveType.Sphere, flower ? Materials["SakuraPink"] : Materials["LeafLight"]);
        }
        MarkStatic(cluster);
    }

    private static void CreatePrefabRing(string name, string prefabPath, Vector3 center, float radius, int count, float y, Transform parent, float minScale, float maxScale)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            PlacePrefab(prefabPath, $"{name}_Prefab_{i:00}", p, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f + Rand(-7f, 7f), 0f), Vector3.one * Rand(minScale, maxScale), ring.transform, PrimitiveType.Cube, Materials["Stone"]);
        }
        MarkStatic(ring);
    }

    private static void CreatePlazaEntryAssets(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(0f, 6.1f, 43f),
            new Vector3(-43f, 6.1f, 2f),
            new Vector3(-17f, 6.1f, -40f),
            new Vector3(42f, 6.1f, -9f),
            new Vector3(34f, 6.3f, 31f)
        };
        float[] yaws = { 180f, 96f, 24f, -64f, -138f };
        for (int i = 0; i < positions.Length; i++)
        {
            PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Arch.prefab", $"Plaza_Entry_Arch_{i:00}", positions[i], Quaternion.Euler(0f, yaws[i], 0f), Vector3.one * 0.9f, parent, PrimitiveType.Cube, Materials["Stone"]);
            Vector3 lampOffset = Quaternion.Euler(0f, yaws[i], 0f) * new Vector3(4.2f, 0.2f, 0f);
            PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Street_Lamp.prefab", $"Plaza_Entry_StreetLamp_{i:00}", positions[i] + lampOffset, Quaternion.Euler(0f, yaws[i] - 24f, 0f), Vector3.one * 0.95f, parent, PrimitiveType.Cube, Materials["Wood"]);
        }
    }

    private static void CreateFenceCurve(string name, Vector3 start, Vector3 end, int count, Transform parent)
    {
        GameObject fence = new GameObject(name);
        fence.transform.SetParent(parent, false);
        Vector3 mid = (start + end) * 0.5f;
        Vector3 side = Vector3.Cross(Vector3.up, (end - start).normalized);
        for (int i = 0; i < count; i++)
        {
            float t = i / Mathf.Max(1f, count - 1f);
            Vector3 p = Vector3.Lerp(start, end, t) + side * Mathf.Sin(t * Mathf.PI) * 5f;
            Quaternion rot = Quaternion.LookRotation((p - mid).normalized, Vector3.up);
            string prefab = i % 2 == 0 ? "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Fence_01.prefab" : "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Fence_02.prefab";
            PlacePrefab(prefab, $"{name}_Segment_{i:00}", p, rot, Vector3.one * Rand(0.85f, 1.15f), fence.transform, PrimitiveType.Cube, Materials["Wood"]);
        }
        MarkStatic(fence);
    }

    private static void CreateRopeLine(string name, Vector3 start, Vector3 end, int posts, Transform parent)
    {
        GameObject ropeLine = new GameObject(name);
        ropeLine.transform.SetParent(parent, false);
        Vector3 direction = end - start;
        Quaternion rot = Quaternion.LookRotation(direction.normalized, Vector3.up);
        for (int i = 0; i < posts; i++)
        {
            Vector3 p = Vector3.Lerp(start, end, i / (float)(posts - 1));
            CreateBox($"Rope_Post_{i:00}", p, new Vector3(0.22f, 1.2f, 0.22f), Materials["Wood"], ropeLine.transform);
            if (i < posts - 1)
            {
                Vector3 next = Vector3.Lerp(start, end, (i + 1) / (float)(posts - 1));
                GameObject rope = CreateBox($"Rope_Span_{i:00}", (p + next) * 0.5f + Vector3.up * 0.45f, new Vector3(0.14f, 0.14f, Vector3.Distance(p, next)), Materials["Rope"], ropeLine.transform);
                rope.transform.rotation = rot;
            }
        }
        MarkStatic(ropeLine);
    }

    private static void CreateCliffRockCluster(string name, Vector2 center, Vector2 radii, int count, float baseY, Transform parent)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent, false);
        for (int i = 0; i < count; i++)
        {
            Vector2 p = RandomInEllipse(center, radii);
            float edgeBias = i / (float)Mathf.Max(1, count - 1);
            float y = baseY + Mathf.Sin(edgeBias * Mathf.PI) * Rand(0.6f, 3.8f) + Rand(-0.6f, 1.2f);
            float scale = Mathf.Lerp(1.1f, 3.4f, Mathf.PerlinNoise(p.x * 0.07f + 4f, p.y * 0.07f + 8f));
            PlacePrefab(RandomRock(), $"{name}_Rock_{i:00}", new Vector3(p.x, y, p.y), Quaternion.Euler(Rand(-6f, 6f), Rand(0f, 360f), Rand(-5f, 5f)), Vector3.one * scale, cluster.transform, PrimitiveType.Sphere, Materials["Stone"]);
        }
        MarkStatic(cluster);
    }

    private static void CreatePoolCoping(string name, Vector3 center, float halfX, float halfZ, Transform parent)
    {
        GameObject coping = new GameObject(name);
        coping.transform.SetParent(parent, false);
        for (int i = 0; i < 18; i++)
        {
            float x = Mathf.Lerp(-halfX, halfX, i / 17f);
            CreateBox($"North_Coping_{i:00}", center + new Vector3(x, 0f, halfZ), new Vector3(2.7f, 0.2f, 1.2f), Materials["StoneDark"], coping.transform);
            CreateBox($"South_Coping_{i:00}", center + new Vector3(x, 0f, -halfZ), new Vector3(2.7f, 0.2f, 1.2f), Materials["StoneDark"], coping.transform);
        }
        for (int i = 0; i < 12; i++)
        {
            float z = Mathf.Lerp(-halfZ, halfZ, i / 11f);
            CreateBox($"West_Coping_{i:00}", center + new Vector3(-halfX, 0f, z), new Vector3(1.2f, 0.2f, 2.7f), Materials["StoneDark"], coping.transform);
            CreateBox($"East_Coping_{i:00}", center + new Vector3(halfX, 0f, z), new Vector3(1.2f, 0.2f, 2.7f), Materials["StoneDark"], coping.transform);
        }
        MarkStatic(coping);
    }

    private static void CreateDockPosts(string name, Vector3 start, Vector3 end, int count, Transform parent)
    {
        GameObject posts = new GameObject(name);
        posts.transform.SetParent(parent, false);
        Vector3 direction = (end - start).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * 2.8f;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = Vector3.Lerp(start, end, i / (float)(count - 1));
            CreateBox($"Post_L_{i:00}", p + side + Vector3.up * 1.1f, new Vector3(0.32f, 2.6f, 0.32f), Materials["Wood"], posts.transform);
            CreateBox($"Post_R_{i:00}", p - side + Vector3.up * 1.1f, new Vector3(0.32f, 2.6f, 0.32f), Materials["Wood"], posts.transform);
            if (i % 3 == 0) CreateLantern($"Dock_Post_Lantern_{i:00}", p + side + Vector3.up * 1.2f, posts.transform, 1.7f);
        }
        MarkStatic(posts);
    }

    private static void CreateRailings(string name, Vector3 center, float radius, int count, Transform parent)
    {
        GameObject railings = new GameObject(name);
        railings.transform.SetParent(parent, false);
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Quaternion rot = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
            CreateBox($"Rail_Post_{i:00}", p + Vector3.up * 0.8f, new Vector3(0.26f, 1.6f, 0.26f), Materials["Wood"], railings.transform);
            if (i % 2 == 0)
            {
                CreateBox($"Rail_Top_{i:00}", p + Vector3.up * 1.55f, new Vector3(3.9f, 0.18f, 0.18f), Materials["Wood"], railings.transform).transform.rotation = rot;
            }
        }
        MarkStatic(railings);
    }

    private static void CreateFoamPatch(string name, Vector3 position, float yaw, float length, Transform parent)
    {
        GameObject foam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        foam.name = name;
        foam.transform.SetParent(parent, false);
        foam.transform.position = position;
        foam.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        foam.transform.localScale = new Vector3(length, 0.025f, Rand(0.42f, 0.9f));
        foam.GetComponent<Renderer>().sharedMaterial = Materials["Foam"];
        MarkStatic(foam);
    }

    private static void CreateFountainWaterVfx(string name, Vector3 position, Transform parent)
    {
        GameObject vfx = new GameObject(name);
        vfx.transform.SetParent(parent, false);
        vfx.transform.position = position;
        ParticleSystem particles = vfx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = 1.15f;
        main.startSpeed = 1.8f;
        main.startSize = 0.16f;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 24f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.9f;
        ParticleSystemRenderer renderer = vfx.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Materials["Foam"];
    }

    private static void CreateSpawnerMarker(string name, Vector3 position, Vector3 scale, Transform parent, string note)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = name;
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        marker.transform.localScale = scale;
        marker.GetComponent<Renderer>().sharedMaterial = Materials["DebugInvisible"];
        Collider collider = marker.GetComponent<Collider>();
        collider.isTrigger = true;
        BESNpcMarker npcMarker = marker.AddComponent<BESNpcMarker>();
        npcMarker.role = BESNpcRole.Animal;
        npcMarker.npcName = name.Replace('_', ' ');
        npcMarker.purpose = note;
    }

    private static void CreateBirdAudio(string name, Vector3 position, Transform parent)
    {
        GameObject audio = new GameObject(name);
        audio.transform.SetParent(parent, false);
        audio.transform.position = position;
        AudioSource source = audio.AddComponent<AudioSource>();
        source.playOnAwake = true;
        source.loop = true;
        source.spatialBlend = 1f;
        source.minDistance = 12f;
        source.maxDistance = 82f;
        source.volume = 0.35f;
    }

    private static void CreateZoneLabels(Transform parent)
    {
        CreateTextLabel("1 NORTH: Dense Green Forest - deer, rabbits, birds, gathering", new Vector3(-45f, 22f, 116f), parent);
        CreateTextLabel("2 SOUTHEAST: Camping island - tents, campfire, merchant, photographer, fisherman", new Vector3(97f, 16f, -137f), parent);
        CreateTextLabel("3 EAST: Fishing Harbor - docks, boat, fishing points", new Vector3(104f, 14f, -30f), parent);
        CreateTextLabel("4 SOUTH: Swimming Pool and recreation", new Vector3(-54f, 14f, -119f), parent);
        CreateTextLabel("5 CENTER: Social Plaza, big fountain, spawn", new Vector3(-12f, 18f, -20f), parent);
        CreateTextLabel("6 WEST: Cherry blossom forest, torii, sakura VFX", new Vector3(-126f, 18f, -10f), parent);
        CreateTextLabel("7 NORTHEAST: Ocean viewpoint cliff, platform, gazebo, cutscene", new Vector3(96f, 29f, 72f), parent);
    }

    private static void CreateTextLabel(string text, Vector3 position, Transform parent)
    {
        GameObject go = new GameObject(text.Split(':')[0]);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.characterSize = 2.2f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = Color.white;
        go.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
    }

    private static void CreateInteraction(string name, BESInteractionType type, Vector3 position, Vector3 scale, Transform parent, string notes)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Materials["DebugInvisible"];
        Collider collider = go.GetComponent<Collider>();
        collider.isTrigger = true;
        BESInteractionZone zone = go.AddComponent<BESInteractionZone>();
        zone.interactionType = type;
        zone.displayName = name.Replace('_', ' ');
        zone.notes = notes;
    }

    private static void CreateNpc(string name, BESNpcRole role, Vector3 position, Transform parent, string purpose)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        go.GetComponent<Renderer>().sharedMaterial = role == BESNpcRole.Fisherman ? Materials["Water"] : Materials["White"];
        BESNpcMarker marker = go.AddComponent<BESNpcMarker>();
        marker.role = role;
        marker.npcName = name.Replace('_', ' ');
        marker.purpose = purpose;
        CreateInteraction($"{name}_InteractionZone", BESInteractionType.NPC, position + Vector3.forward * 1.2f, new Vector3(4f, 3f, 4f), parent, purpose);
    }

    private static void CreateAnimal(string name, Vector3 position, Transform parent, float scale, Color color)
    {
        GameObject animal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        animal.name = name;
        animal.transform.SetParent(parent, false);
        animal.transform.position = position;
        animal.transform.localScale = new Vector3(scale, scale * 0.8f, scale * 1.6f);
        Material mat = new Material(Materials["White"]) { color = color };
        animal.GetComponent<Renderer>().sharedMaterial = mat;
        BESNpcMarker marker = animal.AddComponent<BESNpcMarker>();
        marker.role = BESNpcRole.Animal;
        marker.npcName = name.Replace('_', ' ');
        marker.purpose = "Ambient forest animal placeholder ready to swap with final model.";
    }

    private static void CreateDisc(string name, Vector3 position, float radius, float height, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        MarkStatic(go);
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        MarkStatic(go);
        return go;
    }

    private static GameObject PlacePrefab(string path, string name, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent, PrimitiveType fallback, Material fallbackMaterial)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab) prefab = FindPrefabByFileName(System.IO.Path.GetFileNameWithoutExtension(path));
        GameObject go;
        if (prefab)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            go = GameObject.CreatePrimitive(fallback);
            go.GetComponent<Renderer>().sharedMaterial = fallbackMaterial;
        }

        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        MarkStatic(go);
        return go;
    }

    private static GameObject FindPrefabByFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string[] guids = AssetDatabase.FindAssets($"{fileName} t:Prefab", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string candidate = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(candidate) == fileName)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(candidate);
            }
        }
        return null;
    }

    private static void CreateSakuraTree(string name, Vector3 position, float scale, Transform parent)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.position = position;
        CreateBox("Trunk", Vector3.up * 2.1f * scale, new Vector3(0.9f, 4.2f, 0.9f) * scale, Materials["Wood"], tree.transform);
        for (int i = 0; i < 4; i++)
        {
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = $"Pink_Canopy_{i}";
            crown.transform.SetParent(tree.transform, false);
            crown.transform.localPosition = new Vector3(Rand(-2f, 2f), Rand(5f, 7.2f), Rand(-2f, 2f)) * scale;
            crown.transform.localScale = Vector3.one * Rand(4.4f, 6.4f) * scale;
            crown.GetComponent<Renderer>().sharedMaterial = Materials["SakuraPink"];
        }
    }

    private static void CreateTorii(string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject torii = new GameObject(name);
        torii.transform.SetParent(parent, false);
        torii.transform.SetPositionAndRotation(position, rotation);
        CreateBox("Left_Pillar", new Vector3(-3f, 2.6f, 0f), new Vector3(0.7f, 5.2f, 0.7f), Materials["ToriiRed"], torii.transform);
        CreateBox("Right_Pillar", new Vector3(3f, 2.6f, 0f), new Vector3(0.7f, 5.2f, 0.7f), Materials["ToriiRed"], torii.transform);
        CreateBox("Top_Beam", new Vector3(0f, 5.5f, 0f), new Vector3(8.4f, 0.55f, 0.9f), Materials["ToriiRed"], torii.transform);
        CreateBox("Upper_Beam", new Vector3(0f, 6.25f, 0f), new Vector3(10.2f, 0.45f, 0.7f), Materials["ToriiRed"], torii.transform);
    }

    private static void CreateUmbrella(string name, Vector3 position, Transform parent)
    {
        GameObject umbrella = new GameObject(name);
        umbrella.transform.SetParent(parent, false);
        umbrella.transform.position = position;
        CreateBox("Pole", Vector3.up * 1.6f, new Vector3(0.2f, 3.2f, 0.2f), Materials["Wood"], umbrella.transform);
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        top.name = "Canopy";
        top.transform.SetParent(umbrella.transform, false);
        top.transform.localPosition = Vector3.up * 3.25f;
        top.transform.localScale = new Vector3(3.2f, 0.18f, 3.2f);
        top.GetComponent<Renderer>().sharedMaterial = Materials["White"];
    }

    private static void CreateGazebo(string name, Vector3 position, Transform parent)
    {
        GameObject gazebo = new GameObject(name);
        gazebo.transform.SetParent(parent, false);
        gazebo.transform.position = position;
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            CreateBox($"Post_{i}", new Vector3(Mathf.Cos(a) * 7f, 2.6f, Mathf.Sin(a) * 7f), new Vector3(0.45f, 5.2f, 0.45f), Materials["Wood"], gazebo.transform);
        }
        CreateDisc("Roof", Vector3.up * 5.4f, 8.4f, 0.35f, Materials["Wood"], gazebo.transform);
        CreateDisc("Floor", Vector3.up * 0.2f, 8f, 0.16f, Materials["Stone"], gazebo.transform);
    }

    private static void CreatePath(string name, Transform parent, Vector3 start, Vector3 end, int count, Material mat)
    {
        GameObject path = new GameObject(name);
        path.transform.SetParent(parent, false);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector3 p = Vector3.Lerp(start, end, t);
            CreateBox($"Paver_{i:00}", p, new Vector3(5.8f, 0.16f, 4.2f), mat, path.transform);
        }
    }

    private static void CreateCurvedStonePath(string name, Transform parent, Vector3 start, Vector3 control, Vector3 end, int count)
    {
        GameObject path = new GameObject(name);
        path.transform.SetParent(parent, false);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector3 p = Bezier(start, control, end, t);
            Vector3 next = Bezier(start, control, end, Mathf.Clamp01(t + 0.04f));
            Vector3 tangent = (next - p).normalized;
            float yaw = tangent.sqrMagnitude > 0.001f ? Quaternion.LookRotation(tangent, Vector3.up).eulerAngles.y : 0f;
            p += new Vector3(Rand(-0.45f, 0.45f), Rand(-0.02f, 0.04f), Rand(-0.45f, 0.45f));
            PlacePrefab("Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Stone_Slab.prefab", $"Curved_Paver_{i:00}", p, Quaternion.Euler(0f, yaw + Rand(-8f, 8f), 0f), Vector3.one * Rand(0.82f, 1.08f), path.transform, PrimitiveType.Cube, Materials["Stone"]);
            if (i % 4 == 0)
            {
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized * Rand(3.5f, 4.8f);
                PlacePrefab(RandomFlower(), $"Path_Edge_Flower_{i:00}", p + side + Vector3.up * 0.18f, Quaternion.Euler(0f, Rand(0f, 360f), 0f), Vector3.one * Rand(0.45f, 0.75f), path.transform, PrimitiveType.Sphere, Materials["LeafLight"]);
            }
        }
        MarkStatic(path);
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private static void CreateDockLine(string name, Vector3 start, Vector3 end, int planks, Transform parent)
    {
        GameObject dock = new GameObject(name);
        dock.transform.SetParent(parent, false);
        Vector3 direction = end - start;
        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        for (int i = 0; i < planks; i++)
        {
            Vector3 p = Vector3.Lerp(start, end, i / (float)(planks - 1));
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = $"Wood_Plank_{i:00}";
            plank.transform.SetParent(dock.transform, false);
            plank.transform.position = p;
            plank.transform.rotation = rotation;
            plank.transform.localScale = new Vector3(5.2f, 0.35f, 2.1f);
            plank.GetComponent<Renderer>().sharedMaterial = Materials["Wood"];
        }
    }

    private static GameObject CreateBird(string name, Vector3 position, Transform parent)
    {
        GameObject bird = new GameObject(name);
        bird.transform.SetParent(parent, false);
        bird.transform.position = position;
        CreateBox("Body", Vector3.zero, new Vector3(0.7f, 0.2f, 0.25f), Materials["White"], bird.transform);
        CreateBox("Left_Wing", new Vector3(-0.55f, 0f, 0f), new Vector3(0.9f, 0.08f, 0.22f), Materials["White"], bird.transform);
        CreateBox("Right_Wing", new Vector3(0.55f, 0f, 0f), new Vector3(0.9f, 0.08f, 0.22f), Materials["White"], bird.transform);
        return bird;
    }

    private static void ScatterPrefabs(Transform parent, string[] paths, Vector2 center, Vector2 radii, int count, float minScale, float maxScale)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 p = RandomInEllipse(center, radii);
            string path = paths[Rng.Next(paths.Length)];
            PlacePrefab(path, $"{System.IO.Path.GetFileNameWithoutExtension(path)}_{i:00}", new Vector3(p.x, 6f + Rand(-0.4f, 2.7f), p.y), Quaternion.Euler(0f, Rand(0f, 360f), 0f), Vector3.one * Rand(minScale, maxScale), parent, PrimitiveType.Capsule, Materials["DeepGrass"]);
        }
    }

    private static string[] TreePrefabs() => new[]
    {
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Trees/S_Tree_A.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Trees/S_Tree_B.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Trees/S_Tree_C.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Trees/S_Tree_F.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Trees/S_Tree_H.prefab",
        "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Tree_01.prefab",
        "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Tree_03.prefab",
        "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Tree_05.prefab"
    };

    private static string[] RockPrefabs() => new[]
    {
        "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_01.prefab",
        "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_02.prefab",
        "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_04.prefab",
        "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_07.prefab",
        "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_10.prefab",
        "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Rock_06.prefab",
        "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Rock_10.prefab",
        "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Rock_11.prefab"
    };

    private static string[] BushPrefabs() => new[]
    {
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Bushes/S_Bush_A.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Bushes/S_Bush_B.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Bushes/S_Bush_D.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Bushes/S_Fern_A.prefab",
        "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Bushes/S_Fern_C.prefab",
        "Assets/YughuesFreeBushes2018/Prefabs/P_Bush01.prefab",
        "Assets/YughuesFreeBushes2018/Prefabs/P_Bush03.prefab",
        "Assets/3D_Game_Assets_Flora/Prefap/Bush_1_Prefap.prefab"
    };

    private static string RandomFlower()
    {
        string[] paths =
        {
            "Assets/3D_Game_Assets_Flora/Prefap/Pink_Flower_1.prefab",
            "Assets/3D_Game_Assets_Flora/Prefap/Pink_Flower_2.prefab",
            "Assets/3D_Game_Assets_Flora/Prefap/Flower_yellow_1_Prefap.prefab",
            "Assets/3D_Game_Assets_Flora/Prefap/Flower_yellow_2_Prefap.prefab",
            "Assets/3D_Game_Assets_Flora/Prefap/Flower_purple_1_Prefap.prefab",
            "Assets/3D_Game_Assets_Flora/Prefap/Flower_purple_2_Prefap.prefab",
            "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Bushes/S_Flowers_A.prefab",
            "Assets/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Bushes/S_Flowers_D.prefab",
            "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Flower_08_1.prefab"
        };
        return paths[Rng.Next(paths.Length)];
    }

    private static string[] ResourcePrefabs() => new[]
    {
        "Assets/3D_Game_Assets_Flora/Prefap/Mushroom_Red_Prefap.prefab",
        "Assets/3D_Game_Assets_Flora/Prefap/Mushroom_White_1_Prefap.prefab",
        "Assets/3D_Game_Assets_Flora/Prefap/Mushroom_White_2_Prefap.prefab",
        "Assets/3D_Game_Assets_Flora/Prefap/Fern_1_Prefap.prefab",
        "Assets/3D_Game_Assets_Flora/Prefap/Fern_2_Prefap.prefab",
        "Assets/3D_Game_Assets_Flora/Prefap/Mint.prefab",
        "Assets/3D_Game_Assets_Flora/Prefap/Chamomile.prefab",
        "Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/Gras_02.prefab"
    };

    private static string RandomRock()
    {
        string[] paths = RockPrefabs();
        return paths[Rng.Next(paths.Length)];
    }

    private static float EllipseMask(float x, float z, float rx, float rz)
    {
        float d = (x * x) / (rx * rx) + (z * z) / (rz * rz);
        return Mathf.Clamp01(1f - Mathf.InverseLerp(0.72f, 1.12f, d));
    }

    private static float IslandMask(float x, float z)
    {
        return IrregularEllipseMask(x, z, Vector2.zero, 136f, 112f, 0.15f);
    }

    private static float CampIslandMask(float x, float z)
    {
        return IrregularEllipseMask(x, z, new Vector2(96f, -104f), 64f, 46f, 0.62f);
    }

    private static float IrregularEllipseMask(float x, float z, Vector2 center, float rx, float rz, float phase)
    {
        float lx = x - center.x;
        float lz = z - center.y;
        float angle = Mathf.Atan2(lz, lx);
        float shape = 1f
            + Mathf.Sin(angle * 3.0f + phase) * 0.075f
            + Mathf.Sin(angle * 5.0f - phase * 1.7f) * 0.052f
            + Mathf.PerlinNoise(Mathf.Cos(angle) * 1.8f + 8.3f, Mathf.Sin(angle) * 1.8f + 3.1f) * 0.05f;
        float d = (lx * lx) / (rx * rx * shape * shape) + (lz * lz) / (rz * rz * shape * shape);
        return Mathf.Clamp01(1f - Mathf.InverseLerp(0.68f, 1.13f, d));
    }

    private static Vector3 IrregularBoundaryPoint(Vector2 center, float rx, float rz, float angle, float y)
    {
        float shape = 1f
            + Mathf.Sin(angle * 3.0f + 0.15f) * 0.075f
            + Mathf.Sin(angle * 5.0f - 1.05f) * 0.052f
            + Mathf.PerlinNoise(Mathf.Cos(angle) * 1.8f + 8.3f, Mathf.Sin(angle) * 1.8f + 3.1f) * 0.05f;
        return new Vector3(center.x + Mathf.Cos(angle) * rx * shape, y, center.y + Mathf.Sin(angle) * rz * shape);
    }

    private static float SegmentDistanceMask(Vector2 p, Vector2 a, Vector2 b, float radius)
    {
        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
        float dist = Vector2.Distance(p, a + ab * t);
        return Mathf.Clamp01(1f - dist / radius);
    }

    private static Vector2 RandomInEllipse(Vector2 center, Vector2 radii)
    {
        float angle = Rand(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(Rand(0f, 1f));
        return center + new Vector2(Mathf.Cos(angle) * radii.x * radius, Mathf.Sin(angle) * radii.y * radius);
    }

    private static float Rand(float min, float max)
    {
        return Mathf.Lerp(min, max, (float)Rng.NextDouble());
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.position = Vector3.zero;
        return go;
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void ExportObjectCsv(Transform root)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Name,Position x,Position y,Position z,Rotation x,Rotation y,Rotation z,Scale x,Scale y,Scale z,Parent,Note");
        AppendObjectCsv(csv, root);
        System.IO.File.WriteAllText(ObjectCsvPath, csv.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(ObjectCsvPath);
    }

    private static void AppendObjectCsv(StringBuilder csv, Transform transform)
    {
        Vector3 p = transform.position;
        Vector3 r = transform.eulerAngles;
        Vector3 s = transform.localScale;
        csv.Append(EscapeCsv(transform.name)).Append(',')
            .Append(F(p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(p.z)).Append(',')
            .Append(F(r.x)).Append(',').Append(F(r.y)).Append(',').Append(F(r.z)).Append(',')
            .Append(F(s.x)).Append(',').Append(F(s.y)).Append(',').Append(F(s.z)).Append(',')
            .Append(EscapeCsv(GetParentPath(transform))).Append(',')
            .Append(EscapeCsv(GetObjectNote(transform.gameObject))).AppendLine();

        foreach (Transform child in transform)
        {
            AppendObjectCsv(csv, child);
        }
    }

    private static string GetParentPath(Transform transform)
    {
        if (!transform.parent) return "";
        List<string> names = new List<string>();
        Transform current = transform.parent;
        while (current)
        {
            names.Add(current.name);
            current = current.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    private static string GetObjectNote(GameObject go)
    {
        BESInteractionZone interaction = go.GetComponent<BESInteractionZone>();
        if (interaction) return $"{interaction.interactionType}: {interaction.notes}";
        BESNpcMarker npc = go.GetComponent<BESNpcMarker>();
        if (npc) return $"{npc.role}: {npc.purpose}";
        if (go.name.Contains("Terrain")) return "512m x 512m terrain, bake-ready";
        if (go.name.Contains("PostFX")) return "URP global volume visual polish";
        if (go.name.Contains("Foam")) return "Coastline foam/wave visual polish";
        if (go.name.Contains("Stone_Ring")) return "Central plaza modular paver detail";
        return "";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static void WriteQaChecklist()
    {
        string checklist =
@"# BES Island QA Checklist

## Layout
- [ ] Map frame is 512m x 512m.
- [ ] Central plaza, north forest, west cherry grove, south pool, east fishing harbor, NE viewpoint and SE camping island match the approved sketch.
- [ ] Camping island is separate and connected by two small wooden bridges.
- [ ] Fountain appears only in the central plaza.
- [ ] Boats appear only in the east fishing harbor.
- [ ] Cherry trees appear only in the west cherry grove.
- [ ] Animals appear only in the north forest.

## Lighting And Rendering
- [ ] Bake lightmaps after final prefab swaps.
- [ ] Confirm `PostFX_GlobalVolume_Bloom_Color_Vignette` is active in URP.
- [ ] Confirm daytime preview camera shows the full island clearly.
- [ ] Check bloom, fog and water transparency on mobile quality settings.

## Gameplay Readiness
- [ ] Bake NavMesh and verify all main stone paths are walkable.
- [ ] Confirm `Player_Spawn_Center_Plaza` is centered and unobstructed.
- [ ] Test NPC interaction zones, fishing points, photo spots and sitting spots.
- [ ] Verify animal spawner markers are inside the north forest only.

## Optimization
- [ ] Swap placeholder primitive animals/NPCs with final LOD prefabs when available.
- [ ] Add colliders only to walkable/blocking props; keep small decorative props light.
- [ ] Mark static environment props for batching, occlusion and lightmap baking.
- [ ] Validate mobile draw calls after prefab replacement.
";
        System.IO.File.WriteAllText(QaChecklistPath, checklist, Encoding.UTF8);
        AssetDatabase.ImportAsset(QaChecklistPath);
    }

    private static void MarkStatic(GameObject go)
    {
        if (!go) return;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        foreach (Transform child in go.transform)
        {
            MarkStatic(child.gameObject);
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == scenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
