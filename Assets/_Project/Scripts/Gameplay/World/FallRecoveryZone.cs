using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace BES.Gameplay
{
    public class FallRecoveryZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] float fallThresholdY = -2.0f; // Độ cao kích hoạt rơi hố
        [SerializeField] float damageAmount = 10f; // Máu bị trừ khi rơi hố
        [SerializeField] float safeCheckInterval = 0.5f; // Thời gian giãn cách lưu vị trí an toàn
        
        Vector3 lastSafePosition = Vector3.zero;
        GameObject player;
        bool isRecovering = false;
        float safeCheckTimer = 0f;

        void Start()
        {
            FindPlayer();
            if (player != null)
            {
                lastSafePosition = player.transform.position;
            }
        }

        void Update()
        {
            if (player == null)
            {
                FindPlayer();
                return;
            }

            if (isRecovering) return;

            // 1. Kiểm tra lưu vị trí an toàn định kỳ
            safeCheckTimer += Time.deltaTime;
            if (safeCheckTimer >= safeCheckInterval)
            {
                safeCheckTimer = 0f;
                var cc = player.GetComponent<CharacterController>();
                
                // Nếu người chơi đang đứng trên mặt đất và có NavMesh ở dưới chân
                if (cc != null && cc.isGrounded && NavMesh.SamplePosition(player.transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    lastSafePosition = hit.position;
                }
            }

            // 2. Phát hiện rơi vực
            if (player.transform.position.y < fallThresholdY)
            {
                StartCoroutine(RecoverFromFallRoutine());
            }
        }

        void FindPlayer()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && lastSafePosition == Vector3.zero)
            {
                lastSafePosition = player.transform.position;
            }
        }

        IEnumerator RecoverFromFallRoutine()
        {
            isRecovering = true;

            // Tạm dừng điều khiển nhân vật
            var motor = player.GetComponent<PlayerMotor>();
            var cc = player.GetComponent<CharacterController>();
            if (motor != null) motor.enabled = false;
            
            // Nếu có Animator, đặt Speed về 0 để dừng chạy
            var animator = player.GetComponentInChildren<Animator>();
            if (animator != null) animator.SetFloat("Speed", 0f);

            // 1. Tạo Canvas màn hình đen để Fade Out
            var canvasGo = new GameObject("FallFadeCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("BlackOverlay");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var image = imageGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Fade Out màn hình đen (trong 0.4 giây)
            float fadeTime = 0.4f;
            float timer = 0f;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Clamp01(timer / fadeTime);
                image.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            image.color = Color.black;

            // 2. Trừ máu người chơi (Take Damage)
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damageAmount);
            }

            // 3. Dịch chuyển (Warp) về vị trí an toàn đã lưu
            if (cc != null) cc.enabled = false;
            
            // Warp về lastSafePosition
            player.transform.position = lastSafePosition + Vector3.up * 0.15f;
            
            if (cc != null) cc.enabled = true;

            // Đợi 0.4 giây khi màn hình đang đen
            yield return new WaitForSeconds(0.4f);

            // Fade In màn hình sáng lại (trong 0.5 giây)
            timer = 0f;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(timer / fadeTime);
                image.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }

            // Dọn dẹp canvas
            Destroy(canvasGo);

            // Bật lại điều khiển nhân vật
            if (motor != null) motor.enabled = true;

            isRecovering = false;
        }
    }
}
