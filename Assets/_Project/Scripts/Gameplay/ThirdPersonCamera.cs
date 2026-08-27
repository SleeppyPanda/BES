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
        float yaw;
        float pitch = 12f;
        
        float targetDistance;
        float currentDistance;
        float distanceVelocity;
        
        Vector3 smoothedTargetPosition;
        Vector3 targetPosVelocity;

        void Awake()
        {
            input = FindAnyObjectByType<PlayerInputReader>();
            if (TryGetComponent<Camera>(out var cam))
                cam.fieldOfView = fieldOfView;

            targetDistance = defaultDistance;
            currentDistance = defaultDistance;
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
                return;

            // 1. Zoom bằng cuộn chuột (Genshin Style)
            float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
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

            // 7. SphereCast tránh xuyên tường mượt mà
            float finalDistance = currentDistance;
            int playerLayer = target.gameObject.layer;
            int mask = ~(1 << playerLayer); // Bỏ qua người chơi
            float rayRadius = 0.25f;

            if (Physics.SphereCast(pivot, rayRadius, targetDirection, out RaycastHit hitInfo, currentDistance, mask, QueryTriggerInteraction.Ignore))
            {
                finalDistance = Mathf.Max(0.5f, hitInfo.distance - 0.05f);
            }

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
    }
}
