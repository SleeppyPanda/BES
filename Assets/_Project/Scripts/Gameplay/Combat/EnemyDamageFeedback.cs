using System.Collections;
using UnityEngine;

namespace BES.Gameplay
{
    public class EnemyDamageFeedback : MonoBehaviour
    {
        [SerializeField] float flashDuration = 0.12f;
        [SerializeField] float scalePunch = 1.18f;
        [SerializeField] Color normalColor = new Color(0.75f, 0.12f, 0.12f, 1f);
        [SerializeField] Color hitColor = new Color(1f, 0.95f, 0.25f, 1f);
        [SerializeField] Color critColor = new Color(1f, 0.35f, 0.05f, 1f);

        Renderer targetRenderer;
        Vector3 defaultScale;
        Coroutine feedbackRoutine;

        void Awake()
        {
            targetRenderer = GetComponentInChildren<Renderer>();
            defaultScale = transform.localScale;
            if (targetRenderer != null)
                normalColor = targetRenderer.material.color;
        }

        public void PlayHit(float damage, bool isCritical)
        {
            if (feedbackRoutine != null)
                StopCoroutine(feedbackRoutine);

            feedbackRoutine = StartCoroutine(Feedback(isCritical));
            CombatVfx.SpawnPulse(transform.position + Vector3.up * 1f, isCritical ? critColor : hitColor, isCritical ? 0.7f : 0.45f, 0.18f);
        }

        IEnumerator Feedback(bool isCritical)
        {
            if (targetRenderer != null)
                targetRenderer.material.color = isCritical ? critColor : hitColor;

            transform.localScale = defaultScale * scalePunch;
            yield return new WaitForSeconds(flashDuration);

            transform.localScale = defaultScale;
            if (targetRenderer != null)
                targetRenderer.material.color = normalColor;
            feedbackRoutine = null;
        }
    }
}
