using BES.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BES.Gameplay
{
    /// <summary>
    /// Góc nhìn third-person theo layout Main play.png — nhân vật ở giữa, camera hơi cao và xa.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Header("Distance & Zoom")]
        [SerializeField] float defaultDistance = 5.5f;
        [SerializeField] float minDistance = 2.0f;
        [SerializeField] float maxDistance = 8.5f;
        [SerializeField] float zoomSpeed = 2.0f;
        
        [Header("Angles")]
        [SerializeField] float sensitivity = 0.15f;
        [SerializeField] float minPitch = -15f;
        [SerializeField] float maxPitch = 50f;
        [SerializeField] float fieldOfView = 50f;
        
        [Header("Smoothing")]
        [SerializeField] float positionSmoothTime = 0.08f; // Trễ nhẹ khi nhân vật di chuyển
        [SerializeField] float rotationSmoothTime = 0.05f; // Xoay camera mượt
        [SerializeField] float zoomSmoothTime = 0.15f; // Zoom cuộn chuột mượt
        
        [Header("Height Offset")]
        [SerializeField] float lowZoomHeight = 0.8f; // Cao ngang vai khi zoom gần
        [SerializeField] float highZoomHeight = 1.35f; // Cao hơn khi zoom xa để nhìn bao quát

        [SerializeField] bool lockCameraWhileShiftHeld = true;

        PlayerInputReader input;
        Mouse cachedMouse;      // Cached once — Mouse.current lookup is not free
        float yaw;
        float pitch = 12f;
        
        float targetDistance;
        float currentDistance;
        float distanceVelocity;
        
        Vector3 smoothedTargetPosition;
        Vector3 targetPosVelocity;

        // ── Physics optimization (Genshin-style) ──────────────────────────────
        // SphereCastNonAlloc avoids heap allocation every LateUpdate (was 60 allocs/sec)
        const int RAY_BUFFER_SIZE = 16;
        readonly RaycastHit[] rayBuffer = new RaycastHit[RAY_BUFFER_SIZE];

        // Cache layer masks in Awake — NameToLayer has dict lookup overhead
        int cachedCameraMask = -1;      // -1 = not yet built
        int cachedPlayerLayer;

        // Find-Player cooldown — avoids FindWithTag scene scan every frame
        float findPlayerTimer;

        void Awake()
        {
            input = FindAnyObjectByType<PlayerInputReader>();
            if (TryGetComponent<Camera>(out var cam))
            {
                cam.fieldOfView  = fieldOfView;
                // Fix ground Z-fighting: smaller nearClipPlane reduces precision fighting
                // between ground tiles at similar Y. 0.1 is safe for third-person distance.
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane  = 500f;
            }

            targetDistance  = defaultDistance;
            currentDistance = defaultDistance;

            // Cache Mouse.current once — avoids per-LateUpdate device lookup
            cachedMouse = Mouse.current;

            // Pre-build camera occluder layer mask once (avoids 5x NameToLayer per LateUpdate)
            RebuildCameraLayerMask();
        }

        void Start()
        {
            if (target != null)
            {
                smoothedTargetPosition = target.position;
                yaw = target.eulerAngles.y;
            }
        }

        void LateUpdate()
        {
            if (target == null)
            {
                // Only scan scene every 0.5s, not every frame
                findPlayerTimer -= Time.deltaTime;
                if (findPlayerTimer <= 0f)
                {
                    findPlayerTimer = 0.5f;
                    var player = GameObject.FindWithTag("Player");
                    if (player != null)
                        SetTarget(player.transform);
                    else
                        return;
                }
                else return;
            }

            // 1. Zoom bằng cuộn chuột (Genshin Style)
            float scroll = cachedMouse != null ? cachedMouse.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetDistance -= (scroll / 120f) * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }

            // 2. Xoay camera (Yaw / Pitch)
            if (!IsCameraLocked())
            {
                var look = input != null ? input.Look : Vector2.zero;
                if (look.sqrMagnitude < 0.001f)
                    look = ReadMouseLook();

                yaw += look.x * sensitivity * 10f;
                pitch -= look.y * sensitivity * 10f;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // 3. Smooth follow vị trí của Target (Tránh giật camera khi nhảy/rơi)
            smoothedTargetPosition = Vector3.SmoothDamp(
                smoothedTargetPosition, 
                target.position, 
                ref targetPosVelocity, 
                positionSmoothTime
            );

            // 4. Tính toán độ cao động dựa theo mức độ zoom (Genshin dynamic lookHeight)
            float zoomT = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
            float lookHeight = Mathf.Lerp(lowZoomHeight, highZoomHeight, zoomT);
            Vector3 pivot = smoothedTargetPosition + Vector3.up * lookHeight;

            // 5. Tính toán hướng và vị trí camera lý thuyết
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var targetDirection = rotation * new Vector3(0f, 0f, -1f);

            // 6. Smooth zoom distance
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

            // 7. SphereCast tránh xuyên tường mượt mà — SphereCastNonAlloc (zero GC alloc!)
            // Rebuild mask lazily if player layer changed (e.g., after character swap)
            if (cachedCameraMask < 0 || target.gameObject.layer != cachedPlayerLayer)
                RebuildCameraLayerMask();

            float rayRadius = 0.25f;
            int hitCount = Physics.SphereCastNonAlloc(
                pivot, rayRadius, targetDirection, rayBuffer, currentDistance,
                cachedCameraMask, QueryTriggerInteraction.Ignore);

            float desiredDistance = currentDistance;
            float nearestHitDistance = currentDistance;
            bool hitObstacle = false;

            for (int i = 0; i < hitCount; i++)
            {
                ref RaycastHit hit = ref rayBuffer[i];
                if (hit.collider == null) continue;
                if (hit.transform.IsChildOf(target) || hit.transform == target) continue;
                if (hit.collider.CompareTag("Enemy") || hit.collider.GetComponentInParent<EnemyAI>() != null) continue;

                if (hit.distance < nearestHitDistance)
                {
                    nearestHitDistance = hit.distance;
                    hitObstacle = true;
                }
            }

            if (hitObstacle)
            {
                desiredDistance = Mathf.Max(1.6f, nearestHitDistance - 0.1f);
            }

            float finalDistance = desiredDistance;

            // 8. Cập nhật vị trí và góc nhìn camera
            transform.position = pivot + targetDirection * finalDistance;
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            input = newTarget != null ? newTarget.GetComponent<PlayerInputReader>() : null;
            if (newTarget != null)
            {
                smoothedTargetPosition = newTarget.position;
                yaw = newTarget.eulerAngles.y;
            }
        }

        bool IsCameraLocked()
        {
            if (!lockCameraWhileShiftHeld)
                return false;

            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        static Vector2 ReadMouseLook()
        {
            var mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue() * 0.03f : Vector2.zero;
        }

        void RebuildCameraLayerMask()
        {
            // Called once in Awake and lazily when player layer changes.
            // Avoids 5x LayerMask.NameToLayer() dictionary lookups per LateUpdate.
            int playerLayer    = target != null ? target.gameObject.layer : 0;
            int enemyLayer     = LayerMask.NameToLayer("Enemy");
            int ignoreRaycast  = LayerMask.NameToLayer("Ignore Raycast");
            int transparentFX  = LayerMask.NameToLayer("TransparentFX");
            int uiLayer        = LayerMask.NameToLayer("UI");

            int excludeMask = (1 << playerLayer) | (1 << ignoreRaycast)
                            | (1 << transparentFX) | (1 << uiLayer);
            if (enemyLayer >= 0) excludeMask |= (1 << enemyLayer);

            cachedCameraMask  = ~excludeMask;
            cachedPlayerLayer = playerLayer;
        }
    }
}
