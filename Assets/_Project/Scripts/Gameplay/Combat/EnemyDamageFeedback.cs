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
        Transform scaleTarget;
        Vector3 defaultScale;
        Coroutine feedbackRoutine;

        void Awake()
        {
            targetRenderer = GetComponentInChildren<Renderer>();
            
            // Find "Visual" child as scale target to avoid scaling the physics root (which throws enemies into the sky)
            scaleTarget = transform.Find("Visual");
            if (scaleTarget == null)
            {
                if (targetRenderer != null)
                    scaleTarget = targetRenderer.transform;
                else
                    scaleTarget = transform;
            }

            defaultScale = scaleTarget.localScale;
            if (targetRenderer != null)
                normalColor = targetRenderer.material.color;
        }

        public void PlayHit(float damage, bool isCritical)
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                if (scaleTarget != null)
                    scaleTarget.localScale = defaultScale;
            }

            feedbackRoutine = StartCoroutine(Feedback(isCritical));
            CombatVfx.SpawnPulse(transform.position + Vector3.up * 1f, isCritical ? critColor : hitColor, isCritical ? 0.7f : 0.45f, 0.18f);
            WorldDamagePopup.Show(transform.position + Vector3.up * 1.6f, damage, isCritical ? critColor : hitColor, isCritical);
        }

        IEnumerator Feedback(bool isCritical)
        {
            if (targetRenderer != null)
                targetRenderer.material.color = isCritical ? critColor : hitColor;

            if (scaleTarget != null)
                scaleTarget.localScale = defaultScale * scalePunch;
            yield return new WaitForSeconds(flashDuration);

            if (scaleTarget != null)
                scaleTarget.localScale = defaultScale;
            if (targetRenderer != null)
                targetRenderer.material.color = normalColor;
            feedbackRoutine = null;
        }

        /// <summary>
        /// Reset feedback scale and color when returned to pool.
        /// </summary>
        public void ResetForPool()
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }

            if (scaleTarget != null)
                scaleTarget.localScale = defaultScale;

            if (targetRenderer != null)
                targetRenderer.material.color = normalColor;
        }

    }
}
