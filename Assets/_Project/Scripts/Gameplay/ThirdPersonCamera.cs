using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    /// <summary>
    /// Góc nhìn third-person theo layout Main play.png — nhân vật ở giữa, camera hơi cao và xa.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float distance = 5.5f;
        [SerializeField] float lookHeight = 1.15f;
        [SerializeField] float sensitivity = 2f;
        [SerializeField] float minPitch = -12f;
        [SerializeField] float maxPitch = 42f;
        [SerializeField] float fieldOfView = 50f;

        PlayerInputReader input;
        float yaw;
        float pitch = 8f;

        void Awake()
        {
            input = FindAnyObjectByType<PlayerInputReader>();
            if (TryGetComponent<Camera>(out var cam))
                cam.fieldOfView = fieldOfView;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            var look = input != null ? input.Look : Vector2.zero;
            yaw += look.x * sensitivity;
            pitch -= look.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            var pivot = target.position + Vector3.up * lookHeight;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = pivot + rotation * new Vector3(0f, 0f, -distance);
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }

        public void SetTarget(Transform newTarget) => target = newTarget;
    }
}
