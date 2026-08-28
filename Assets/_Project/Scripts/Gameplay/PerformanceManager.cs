using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BES.Gameplay
{
    /// <summary>
    /// Runtime Performance Manager — Applied at game start.
    ///
    /// Technique references:
    ///   Genshin Impact  — disables shadows entirely on mobile/low-end
    ///   Elden Ring       — dynamic quality tiers based on GPU vRAM
    ///   Horizon ZD       — LOD bias + occlusion aggressive tuning
    ///   Cyberpunk 2077   — Dynamic Resolution Scaling (auto renderScale)
    ///   God of War PC    — Gradual resolution restore when FPS recovers
    ///
    /// Attached to the same GameObject as GameplaySceneBootstrap (auto-added).
    /// </summary>
    [DefaultExecutionOrder(-50)]  // Run before other scripts
    public class PerformanceManager : MonoBehaviour
    {
        [Header("Target Frame Rate")]
        [Tooltip("0 = uncapped (max FPS). Set 60 or 120 to cap.")]
        [SerializeField] int targetFrameRate = 0;

        [Header("Shadow Settings")]
        [Tooltip("Disabled = no shadows at all (maximum FPS mode, Genshin mobile style)")]
        [SerializeField] bool disableShadows = true;

        [Header("Quality Settings")]
        [Tooltip("LOD bias: higher = more detail at distance (more GPU cost). 0.7 = Elden Ring-like aggressive LOD swap")]
        [SerializeField] float lodBias = 0.7f;
        [Tooltip("Max LOD level: 0 = full detail. 1+ = never render highest LOD at distance")]
        [SerializeField] int maximumLODLevel = 0;
        [Tooltip("Anisotropic filtering for textures (ForceEnable costs some GPU, Disable gains FPS)")]
        [SerializeField] AnisotropicFiltering anisotropicFiltering = AnisotropicFiltering.Enable;

        [Header("Rendering")]
        [Tooltip("Reduces render resolution fraction: 1.0 = native, 0.75 = 75% (big GPU save on higher-res monitors)")]
        [SerializeField][Range(0.5f, 1.0f)] float renderScale = 1.0f;

        [Header("Camera Distance Culling")]
        [Tooltip("Layer indices that should be culled aggressively at distance (e.g. small props, particles)")]
        [SerializeField] float smallPropCullDistance = 50f;
        [SerializeField] float enemyCullDistance = 80f;

        // ── Dynamic Resolution Scaling (Cyberpunk 2077 / God of War PC) ──────
        // Monitors real FPS every second and auto-adjusts renderScale to
        // keep the game at or above the target FPS.
        [Header("Dynamic Resolution Scaling")]
        [Tooltip("Enable automatic renderScale reduction when FPS drops below target")]
        [SerializeField] bool dynamicResolutionEnabled = true;
        [Tooltip("FPS target. Below this, scale starts reducing. 0 = auto (uses targetFrameRate or 120)")]
        [SerializeField] int dynamicResolutionTargetFPS = 120;
        [Tooltip("Minimum render scale DRS will reduce to (0.65 is Cyberpunk's floor)")]
        [SerializeField][Range(0.5f, 1.0f)] float drsMinScale = 0.75f;
        [Tooltip("Seconds to wait before attempting to restore render scale (prevents thrashing)")]
        [SerializeField] float drsRecoveryCooldown = 3f;

        Camera mainCamera;
        UniversalRenderPipelineAsset urpAsset;

        // DRS state
        float drsTimer;
        float drsSampleSum;
        int   drsSampleCount;
        float drsCurrentScale;
        float drsLastReduceTime;
        float drsWarmupTimer;   // Delay before DRS starts evaluating (avoids FPS=0 during load)

        void Awake()
        {
            ApplyAll();
            drsCurrentScale = renderScale;
            drsWarmupTimer  = 5f;  // Give the scene 5 seconds to load before DRS starts
        }

        [ContextMenu("Apply Performance Settings")]
        public void ApplyAll()
        {
            ApplyFrameRate();
            ApplyQualitySettings();
            ApplyURPSettings();
            // Camera culling applied after camera is ready
            Invoke(nameof(ApplyCameraCulling), 0.5f);
        }

        void ApplyFrameRate()
        {
            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = targetFrameRate;
                QualitySettings.vSyncCount = 0; // VSync off — let targetFrameRate control
            }
            else
            {
                Application.targetFrameRate = -1; // Uncapped
                QualitySettings.vSyncCount = 0;
            }
        }

        void ApplyQualitySettings()
        {
            // Shadow
            if (disableShadows)
            {
                QualitySettings.shadowDistance = 0f;
                QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            }
            else
            {
                QualitySettings.shadowDistance = 50f;
                QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
            }

            // LOD
            QualitySettings.lodBias              = lodBias;
            QualitySettings.maximumLODLevel      = maximumLODLevel;
            QualitySettings.anisotropicFiltering = anisotropicFiltering;

            // Pixel light count (only relevant for built-in pipeline, but harmless for URP)
            QualitySettings.pixelLightCount = 2;

            // Soft particles off (saves bandwidth)
            QualitySettings.softParticles = false;
        }

        void ApplyURPSettings()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset asset)
            {
                urpAsset = asset;

                // Render Scale (super-sampling off, resolution drop)
                asset.renderScale = renderScale;

                // Shadow
                if (disableShadows)
                {
                    asset.shadowDistance     = 0f;
                    asset.mainLightShadowmapResolution = 256; // Lowest possible
                }
                else
                {
                    asset.shadowDistance = 50f;
                    asset.mainLightShadowmapResolution = 1024;
                }

                // Enable 4x MSAA for high-frequency aliasing protection on PC (very cheap on modern GPUs like RTX 3060)
                asset.msaaSampleCount = 4;

                // Force Anisotropic Filtering globally to eliminate distant texture shimmering at oblique angles
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

                Debug.Log($"[PerformanceManager] URP: renderScale={asset.renderScale:F2} " +
                          $"shadowDist={asset.shadowDistance} MSAA={asset.msaaSampleCount} Aniso=ForceEnable");
            }
            else
            {
                Debug.LogWarning("[PerformanceManager] No URP asset found — skipping URP tuning.");
            }
        }

        void ApplyCameraCulling()
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Unity's per-layer cull distances (array of 32 layer distances)
            float[] layerCullDistances = new float[32];

            // Set default large distance for all layers
            for (int i = 0; i < 32; i++)
                layerCullDistances[i] = 0f; // 0 = use camera far clip (no override)

            // Layer 10: SmallProps — aggressive culling at 50m (saves draw calls)
            // Adjust layer index to match your Unity layer setup
            layerCullDistances[10] = smallPropCullDistance;

            // Layer 11: Enemies — cull at 80m (matches EnemyAI.RENDERER_CULL_DISTANCE)
            layerCullDistances[11] = enemyCullDistance;

            mainCamera.layerCullDistances = layerCullDistances;
            mainCamera.layerCullSpherical = true; // Spherical culling is more accurate

            Debug.Log($"[PerformanceManager] Camera layer culling applied. " +
                      $"Props={smallPropCullDistance}m, Enemies={enemyCullDistance}m");
        }

        void Update()
        {
            if (urpAsset == null) return;

            // ── Smooth Resolution Interpolation (runs every single frame) ─────
            // Interpolates actual renderScale towards target drsCurrentScale smoothly
            // (0.25f per second: a 10% change takes 0.4 seconds, making it invisible to the eye)
            if (dynamicResolutionEnabled)
            {
                urpAsset.renderScale = Mathf.MoveTowards(
                    urpAsset.renderScale, 
                    drsCurrentScale, 
                    0.25f * Time.unscaledDeltaTime
                );
            }
            else
            {
                // Reset to default scale if DRS is disabled
                urpAsset.renderScale = Mathf.MoveTowards(
                    urpAsset.renderScale, 
                    renderScale, 
                    0.25f * Time.unscaledDeltaTime
                );
            }

            // ── DRS Evaluation (sample collection and decision) ──────────────
            if (!dynamicResolutionEnabled) return;

            // Warmup: don't evaluate during scene load/startup
            if (drsWarmupTimer > 0f)
            {
                drsWarmupTimer -= Time.unscaledDeltaTime;
                return;
            }

            float dt = Time.unscaledDeltaTime;

            // Skip frames where engine was stalled (loading, GC, etc.)
            // dt > 0.2s = FPS < 5 = we're in a loading stall, not real gameplay
            if (dt > 0.2f) return;

            // Accumulate FPS samples
            drsSampleSum += 1f / dt;
            drsSampleCount++;
            drsTimer += dt;

            if (drsTimer < 1f) return;  // Wait for 1 second of data

            float avgFps = drsSampleSum / drsSampleCount;
            drsSampleSum   = 0f;
            drsSampleCount = 0;
            drsTimer       = 0f;

            int fpsTarget = dynamicResolutionTargetFPS > 0
                ? dynamicResolutionTargetFPS
                : (targetFrameRate > 0 ? targetFrameRate : 120);

            float newScale = drsCurrentScale;

            // ── Step-down: FPS is below target — reduce render scale ──────────
            if (avgFps < fpsTarget * 0.85f)         // Below 85% of target → urgent
            {
                newScale = Mathf.Max(drsMinScale, drsCurrentScale - 0.10f);
                drsLastReduceTime = Time.unscaledTime;
                if (!Mathf.Approximately(newScale, drsCurrentScale))
                    Debug.Log($"[DRS] FPS {avgFps:F0} < {fpsTarget*0.85f:F0} → target scale {drsCurrentScale:F2}→{newScale:F2}");
            }
            else if (avgFps < fpsTarget * 0.95f)    // Below 95% → gentle reduce
            {
                newScale = Mathf.Max(drsMinScale, drsCurrentScale - 0.05f);
                drsLastReduceTime = Time.unscaledTime;
            }
            // ── Step-up: FPS is well above target — recover render scale ──────
            else if (avgFps > fpsTarget * 1.1f &&
                     (Time.unscaledTime - drsLastReduceTime) > drsRecoveryCooldown)
            {
                // Gradual recovery to avoid thrashing (Cyberpunk technique)
                newScale = Mathf.Min(renderScale, drsCurrentScale + 0.05f);
                if (!Mathf.Approximately(newScale, drsCurrentScale))
                    Debug.Log($"[DRS] FPS {avgFps:F0} > {fpsTarget*1.1f:F0} → target scale {drsCurrentScale:F2}→{newScale:F2}");
            }

            // Update target scale if changed
            if (!Mathf.Approximately(newScale, drsCurrentScale))
            {
                drsCurrentScale = newScale;
            }
        }

        void OnValidate()
        {
            // Live-update in Play Mode when Inspector values change
            if (Application.isPlaying)
                ApplyAll();
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            // Small indicator in top-left (editor only, FPSDisplay handles runtime)
            GUI.Label(new Rect(8, 8, 360, 20),
                $"[PM] Shadows:{!disableShadows} Scale:{drsCurrentScale:F2} DRS:{(dynamicResolutionEnabled ? "ON" : "OFF")}");
        }
#endif
    }
}
