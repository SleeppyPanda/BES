using System.Collections;
using UnityEngine;

namespace BES.Gameplay
{
    public static class CombatVfx
    {
        public static void SpawnPulse(Vector3 position, Color color, float radius, float duration = 0.25f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "CombatVFX_Pulse";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * Mathf.Max(0.1f, radius);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = renderer.material;
                if (material != null)
                    material.color = color;
            }

            var runner = go.AddComponent<CombatVfxLifetime>();
            runner.Play(duration);
        }
    }

    public class CombatVfxLifetime : MonoBehaviour
    {
        public void Play(float duration) => StartCoroutine(Run(duration));

        IEnumerator Run(float duration)
        {
            var startScale = transform.localScale;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                transform.localScale = Vector3.Lerp(startScale, startScale * 1.35f, t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
