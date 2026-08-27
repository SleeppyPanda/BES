using BES.Core;
using BES.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BES.Gameplay
{
    public class TreasureChest : MonoBehaviour
    {
        [SerializeField] string instanceId;
        [SerializeField] float interactRange = 2.5f;
        [SerializeField] string rewardItemId = "item_exp_gold";
        [SerializeField] int rewardAmount = 1;
        [SerializeField] string secondaryRewardItemId = "potion_heal";
        [SerializeField] int secondaryRewardAmount = 2;

        Transform player;
        bool wasInRange;
        bool isOpened;
        float nextInteractTime;

        void Awake()
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                instanceId = $"chest_{gameObject.name}_{transform.position.GetHashCode()}";
            }

            if (MetaProgressState.Instance != null && MetaProgressState.Instance.IsWorldObjectCollected(instanceId))
            {
                isOpened = true;
                // Nếu rương đã được mở trước đó, vô hiệu hóa luôn để không hiện lại
                gameObject.SetActive(false);
            }
        }

        void Start()
        {
            FindPlayer();
        }

        void Update()
        {
            if (isOpened) return;

            if (player == null)
            {
                FindPlayer();
                if (player == null) return;
            }

            float dist = Vector3.Distance(transform.position, player.position);
            bool inRange = dist <= interactRange;

            if (inRange && !wasInRange)
            {
                GameEvents.RaiseNpcInRange("Rương Kho Báu");
            }
            else if (!inRange && wasInRange)
            {
                GameEvents.RaiseNpcOutOfRange();
            }

            wasInRange = inRange;

            if (inRange && Time.time >= nextInteractTime && WasInteractPressed())
            {
                OpenChest();
            }
        }

        void OpenChest()
        {
            isOpened = true;
            nextInteractTime = Time.time + 1f;

            // Ẩn nhắc nhở tương tác trên HUD
            GameEvents.RaiseNpcOutOfRange();

            // 1. Thêm vật phẩm vào kho đồ (Inventory)
            bool addPrimary = false;
            bool addSecondary = false;

            if (GameManager.Instance != null)
            {
                addPrimary = GameManager.Instance.Inventory.AddItem(rewardItemId, rewardAmount);
                addSecondary = GameManager.Instance.Inventory.AddItem(secondaryRewardItemId, secondaryRewardAmount);
            }
            else
            {
                // Fallback nếu chạy test không qua GameManager
                addPrimary = true;
                addSecondary = true;
            }

            // 2. Giao diện hiển thị trực quan vật phẩm nhận được (LootNotificationUI)
            LootNotificationUI.Show(rewardItemId, rewardAmount);
            LootNotificationUI.Show(secondaryRewardItemId, secondaryRewardAmount);

            var chatBox = FindAnyObjectByType<ChatBoxUI>();
            if (chatBox != null)
            {
                chatBox.AddSystemMessage($"Bạn đã mở Rương báu vật và nhận được: Lọ EXP Vàng x{rewardAmount}, Bình Hồi Máu x{secondaryRewardAmount}!");
            }
            else
            {
                Debug.Log($"[BES Chest] Đã mở rương nhận: {rewardItemId} x{rewardAmount}, {secondaryRewardItemId} x{secondaryRewardAmount}");
            }

            // 3. Đánh dấu trạng thái rương đã mở (Lưu tiến trình thế giới)
            MetaProgressState.Instance?.MarkWorldObjectCollected(instanceId);

            // 4. Kích hoạt sự kiện thu thập để cập nhật tiến độ Nhiệm vụ (Quest)
            GameEvents.RaiseCollectiblePickedUp(rewardItemId);
            GameEvents.RaiseCollectiblePickedUp(secondaryRewardItemId);

            // 5. Hiệu ứng hạt lấp lánh (VFX)
            CreateSparkleVFX();

            // 6. Hiệu ứng thu nhỏ dần rồi biến mất (Smooth Loot Animation)
            StartCoroutine(LootAnimationRoutine());
        }

        System.Collections.IEnumerator LootAnimationRoutine()
        {
            // Tắt va chạm vật lý để người chơi đi qua được
            if (TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }

            Vector3 startScale = transform.localScale;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        void CreateSparkleVFX()
        {
            // Tạo một GameObject tạm chứa hiệu ứng hạt hoặc một luồng sáng vàng bốc lên
            GameObject vfx = new GameObject("ChestOpenVFX");
            vfx.transform.position = transform.position + Vector3.up * 0.5f;

            // Thêm một Point Light chiếu sáng lấp lánh tạm thời
            var light = vfx.AddComponent<Light>();
            light.color = new Color(1.0f, 0.85f, 0.4f); // Ánh sáng vàng hoàng kim
            light.range = 5f;
            light.intensity = 3f;

            // Hủy VFX sau 1.5 giây
            Destroy(vfx, 1.5f);
        }

        bool WasInteractPressed()
        {
            if (player != null &&
                player.TryGetComponent<Gameplay.PlayerInputReader>(out var input) &&
                input.InteractPressed)
                return true;

            var keyboard = Keyboard.current;
            return keyboard != null &&
                (keyboard.fKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame);
        }

        void FindPlayer()
        {
            // Tìm thông qua component PlayerMotor trước tiên (rất đáng tin cậy)
            var motor = FindAnyObjectByType<PlayerMotor>();
            if (motor != null)
            {
                player = motor.transform;
                return;
            }

            // Fallback tìm kiếm bằng tag
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        void OnDisable()
        {
            // Dọn dẹp HUD nếu rương bị vô hiệu hóa đột ngột
            if (wasInRange)
            {
                GameEvents.RaiseNpcOutOfRange();
                wasInRange = false;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
