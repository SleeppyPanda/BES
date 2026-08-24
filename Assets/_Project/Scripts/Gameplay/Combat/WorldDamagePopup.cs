using System.Collections;
using TMPro;
using UnityEngine;

namespace BES.Gameplay
{
    public class WorldDamagePopup : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] float duration = 0.8f;
        [SerializeField] Vector3 travel = new(0f, 1.1f, 0f);

        Camera followCamera;
        Vector3 startPosition;
        Color startColor;

        public static void Show(Vector3 position, float amount, Color color, bool critical = false)
        {
            var go = new GameObject("WorldDamagePopup");
            go.transform.position = position;
            var popup = go.AddComponent<WorldDamagePopup>();
            popup.BuildLabel();
            popup.Configure(amount, color, critical);
        }

        void BuildLabel()
        {
            label = gameObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 4f;
            label.raycastTarget = false;
        }

        void Configure(float amount, Color color, bool critical)
        {
            followCamera = Camera.main;
            startPosition = transform.position;
            startColor = color;
            label.text = critical ? $"CRIT -{Mathf.CeilToInt(amount)}" : $"-{Mathf.CeilToInt(amount)}";
            label.color = color;
            label.fontStyle = critical ? FontStyles.Bold : FontStyles.Normal;
            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 2f);
                transform.position = startPosition + travel * eased;
                if (followCamera != null)
                    transform.rotation = Quaternion.LookRotation(transform.position - followCamera.transform.position);
                var color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                label.color = color;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
