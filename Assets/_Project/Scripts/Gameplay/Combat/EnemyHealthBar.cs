using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.Gameplay
{
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] Vector2 barSize = new Vector2(1.2f, 0.12f);
        [SerializeField] float heightOffset = 0.3f;
        [SerializeField] float showDuration = 4.0f;

        [Header("Colors")]
        [SerializeField] Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        [SerializeField] Color catchUpColor = new Color(0.95f, 0.8f, 0.3f, 1f);
        [SerializeField] Color healthColor = new Color(0.85f, 0.15f, 0.15f, 1f);

        EnemyHealth enemyHealth;
        Canvas canvas;
        CanvasGroup canvasGroup;
        RectTransform mainBarRect;
        RectTransform catchUpBarRect;
        TMP_Text nameLabel;
        Camera mainCamera;

        float targetFill = 1f;
        float catchUpFill = 1f;
        float visibilityTimer;

        void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            mainCamera = Camera.main;
            
            CreateHealthBarUI();
        }

        void OnEnable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnHealthChanged += HandleHealthChanged;
            }
        }

        void OnDisable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnHealthChanged -= HandleHealthChanged;
            }
        }

        void Start()
        {
            // Initial update
            UpdateHealthBars(enemyHealth.CurrentHealth, enemyHealth.MaxHealth, false);
            
            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        void CreateHealthBarUI()
        {
            // Calculate height above enemy head
            float spawnHeight = 2.0f;
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                spawnHeight = capsule.height;
            }

            // Create Canvas GameObject
            GameObject canvasGo = new GameObject("EnemyHealthBarCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, spawnHeight + heightOffset, 0f);
            canvasGo.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f); // 1 unit in Canvas = 0.5cm in World

            // Add Canvas
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Add CanvasGroup for smooth fade in/out
            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Set RectTransform size (in pixels)
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(240f, 80f);

            // 1. Background image (200 x 20 pixels)
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = backgroundColor;
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0f);
            bgRect.anchorMax = new Vector2(0.5f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.sizeDelta = new Vector2(200f, 20f);
            bgRect.anchoredPosition = new Vector2(0f, 10f); // 10px from canvas bottom

            // 2. Catch Up health bar (Yellow) - slightly smaller for border
            GameObject catchUpGo = new GameObject("CatchUpBar");
            catchUpGo.transform.SetParent(bgGo.transform, false);
            var catchUpImg = catchUpGo.AddComponent<Image>();
            catchUpImg.color = catchUpColor;
            catchUpBarRect = catchUpGo.GetComponent<RectTransform>();
            catchUpBarRect.anchorMin = new Vector2(0f, 0.5f);
            catchUpBarRect.anchorMax = new Vector2(0f, 0.5f);
            catchUpBarRect.pivot = new Vector2(0f, 0.5f);
            catchUpBarRect.sizeDelta = new Vector2(196f, 16f); // 2px border all around
            catchUpBarRect.anchoredPosition = new Vector2(2f, 0f);

            // 3. Main health bar (Red)
            GameObject mainGo = new GameObject("MainHealthBar");
            mainGo.transform.SetParent(bgGo.transform, false);
            var mainImg = mainGo.AddComponent<Image>();
            mainImg.color = healthColor;
            mainBarRect = mainGo.GetComponent<RectTransform>();
            mainBarRect.anchorMin = new Vector2(0f, 0.5f);
            mainBarRect.anchorMax = new Vector2(0f, 0.5f);
            mainBarRect.pivot = new Vector2(0f, 0.5f);
            mainBarRect.sizeDelta = new Vector2(196f, 16f);
            mainBarRect.anchoredPosition = new Vector2(2f, 0f);

            // 4. Enemy Name Text
            GameObject textGo = new GameObject("NameText");
            textGo.transform.SetParent(canvasGo.transform, false);
            nameLabel = textGo.AddComponent<TextMeshProUGUI>();
            
            // Clean up name string
            string cleanName = enemyHealth.EnemyId;
            cleanName = cleanName.Replace("Enemy_", "").Replace("Boss_", "").Replace("(Clone)", "").Replace("_", " ");
            if (cleanName.EndsWith("biped"))
            {
                cleanName = cleanName.Substring(0, cleanName.Length - 5).Trim();
            }
            nameLabel.text = cleanName;
            
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.fontSize = 14f; // Normal readable pixel size
            nameLabel.color = Color.white;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.outlineColor = Color.black;
            nameLabel.outlineWidth = 0.22f;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.sizeDelta = new Vector2(240f, 30f);
            textRect.anchoredPosition = new Vector2(0f, -5f); // 5px from canvas top
        }

        void HandleHealthChanged(float current, float max)
        {
            UpdateHealthBars(current, max, true);
        }

        void UpdateHealthBars(float current, float max, bool showUI)
        {
            if (max <= 0f) return;
            
            targetFill = Mathf.Clamp01(current / max);
            
            if (mainBarRect != null)
            {
                mainBarRect.localScale = new Vector3(targetFill, 1f, 1f);
            }

            if (showUI)
            {
                visibilityTimer = showDuration;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }

        void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (canvas != null && mainCamera != null)
            {
                // Face the camera
                canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - mainCamera.transform.position);
            }

            // Smoothly catch up the yellow lag bar
            if (catchUpBarRect != null)
            {
                if (catchUpFill > targetFill)
                {
                    catchUpFill = Mathf.MoveTowards(catchUpFill, targetFill, Time.deltaTime * 0.4f);
                    catchUpBarRect.localScale = new Vector3(catchUpFill, 1f, 1f);
                }
                else
                {
                    catchUpFill = targetFill;
                    catchUpBarRect.localScale = new Vector3(catchUpFill, 1f, 1f);
                }
            }

            // Fade out timer
            if (visibilityTimer > 0f)
            {
                visibilityTimer -= Time.deltaTime;
                if (visibilityTimer <= 0f && canvasGroup != null)
                {
                    StartCoroutine(FadeOutCanvas());
                }
            }
        }

        System.Collections.IEnumerator FadeOutCanvas()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < duration)
            {
                if (visibilityTimer > 0f)
                {
                    canvasGroup.alpha = 1f;
                    yield break;
                }

                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}
