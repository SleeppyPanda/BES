using UnityEngine;
using UnityEngine.InputSystem;

namespace BES.Gameplay
{
    /// <summary>
    /// FPS Overlay display for developers.
    /// Shows real-time FPS, frame time (ms), and managed heap memory.
    /// Inspired by AAA dev builds (Elden Ring, Horizon Zero Dawn debug overlay).
    /// Toggle with F3 key.
    ///
    /// Optimization: Uses a fixed float[] circular buffer instead of Queue<float>
    /// to eliminate per-frame heap allocation (zero GC). The Queue.Enqueue/Dequeue
    /// pattern allocates linked-list nodes internally on each call.
    /// </summary>
    public class FPSDisplay : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] Key toggleKey = Key.F3;
        [SerializeField] bool showOnStart = true;

        [Header("Target FPS Marker")]
        [Tooltip("Show a target FPS line in the overlay (e.g. 60 or 120). 0 = hide")]
        [SerializeField] int targetFpsMarker = 120;

        // ── Circular buffer (zero GC, replaces Queue<float>) ──────────────────
        // Fixed-size ring buffer. No heap allocations after initial array creation.
        const int SAMPLE_WINDOW = 60;
        readonly float[] frameTimeBuffer = new float[SAMPLE_WINDOW];
        int   bufferHead;       // Next write index
        int   bufferCount;      // Number of valid samples (ramps up to SAMPLE_WINDOW)

        float fpsTimer;
        float avgFps;
        float avgMs;
        float minFps = float.MaxValue;
        float maxFps;

        bool visible;

        // GUI Style (cached to avoid GC each OnGUI call)
        GUIStyle boxStyle;
        GUIStyle labelStyle;

        void Start()
        {
            visible = showOnStart;
        }

        void Update()
        {
            // Toggle
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                visible = !visible;

            if (!visible) return;

            // Accumulate samples into circular buffer — zero allocation
            float dt = Time.unscaledDeltaTime;
            float fps = dt > 0f ? 1f / dt : 0f;

            frameTimeBuffer[bufferHead] = dt;
            bufferHead = (bufferHead + 1) % SAMPLE_WINDOW;
            if (bufferCount < SAMPLE_WINDOW) bufferCount++;

            if (fps > 0f)
            {
                if (fps < minFps) minFps = fps;
                if (fps > maxFps) maxFps = fps;
            }

            // Compute rolling average every 0.2s — read from circular buffer
            fpsTimer += dt;
            if (fpsTimer >= 0.2f)
            {
                fpsTimer = 0f;
                float totalDt = 0f;
                int n = bufferCount;
                for (int i = 0; i < n; i++)
                    totalDt += frameTimeBuffer[i];

                avgMs  = n > 0 ? (totalDt / n) * 1000f : 0f;
                avgFps = avgMs > 0f ? 1000f / avgMs : 0f;
            }
        }

        void OnGUI()
        {
            if (!visible) return;

            // Lazy-init styles (first frame only)
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize  = 13,
                    alignment = TextAnchor.UpperLeft
                };
                boxStyle.normal.background = MakeSolidTexture(2, 2, new Color(0f, 0f, 0f, 0.6f));

                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 13,
                    fontStyle = FontStyle.Bold
                };
            }

            float heap = System.GC.GetTotalMemory(false) / (1024f * 1024f);

            // Color-code FPS
            Color fpsColor;
            if      (avgFps >= 100f) fpsColor = new Color(0f, 1f, 0.4f);   // Bright green = 100+
            else if (avgFps >=  60f) fpsColor = Color.green;                // Green = 60+
            else if (avgFps >=  45f) fpsColor = Color.yellow;               // Yellow = 45+
            else                    fpsColor = Color.red;                   // Red = below 45

            // Show target marker color
            Color targetColor = avgFps >= targetFpsMarker
                ? new Color(0f, 0.8f, 1f)   // Cyan = at/above target
                : new Color(1f, 0.5f, 0f);  // Orange = below target

            bool showTarget = targetFpsMarker > 0;
            float boxHeight = showTarget ? 112f : 92f;

            const float W = 210f;
            float x = Screen.width - W - 8f;
            float y = 8f;

            GUI.Box(new Rect(x, y, W, boxHeight), GUIContent.none, boxStyle);

            // Line 1: Current FPS
            labelStyle.normal.textColor = fpsColor;
            GUI.Label(new Rect(x + 8, y + 6,  W - 16, 22),
                $"FPS  {avgFps:F0}  ({avgMs:F1} ms)", labelStyle);

            // Line 2: Min/Max
            labelStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(x + 8, y + 28, W - 16, 22),
                $"MIN {minFps:F0}  MAX {maxFps:F0}", labelStyle);

            // Line 3: Heap
            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 8, y + 50, W - 16, 22),
                $"Heap  {heap:F1} MB", labelStyle);

            // Line 4: Target FPS marker (optional)
            if (showTarget)
            {
                labelStyle.normal.textColor = targetColor;
                string targetLabel = avgFps >= targetFpsMarker ? "✓" : "✗";
                GUI.Label(new Rect(x + 8, y + 72, W - 16, 22),
                    $"Target {targetFpsMarker} FPS  {targetLabel}", labelStyle);

                labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(x + 8, y + 90, W - 16, 18), $"[{toggleKey}] Toggle", labelStyle);
            }
            else
            {
                labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(x + 8, y + 68, W - 16, 20), $"[{toggleKey}] Toggle", labelStyle);
            }
        }

        static Texture2D MakeSolidTexture(int w, int h, Color col)
        {
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        void OnApplicationQuit()
        {
            // Reset min/max so next play session is fresh
            minFps = float.MaxValue;
            maxFps = 0f;
        }
    }
}
