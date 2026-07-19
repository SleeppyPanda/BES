using UnityEngine;

namespace BES.Environment
{
    [ExecuteAlways]
    public class BESDayNightCycle : MonoBehaviour
    {
        public Light sun;
        public Gradient sunColor = new Gradient();
        [Range(0f, 24f)] public float timeOfDay = 10f;
        public float dayLengthSeconds = 900f;
        public bool animateInPlayMode = true;

        private void Reset()
        {
            sun = GetComponent<Light>();
            sunColor.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.25f, 0.35f, 0.8f), 0f),
                    new GradientColorKey(new Color(1f, 0.76f, 0.45f), 0.25f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(new Color(1f, 0.58f, 0.35f), 0.75f),
                    new GradientColorKey(new Color(0.22f, 0.3f, 0.65f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        }

        private void Update()
        {
            if (Application.isPlaying && animateInPlayMode && dayLengthSeconds > 0f)
            {
                timeOfDay = (timeOfDay + Time.deltaTime * 24f / dayLengthSeconds) % 24f;
            }

            if (!sun) return;

            float normalized = timeOfDay / 24f;
            sun.transform.rotation = Quaternion.Euler(normalized * 360f - 90f, 135f, 0f);
            sun.color = sunColor.Evaluate(normalized);
            sun.intensity = Mathf.Lerp(0.15f, 1.25f, Mathf.Clamp01(Mathf.Sin(normalized * Mathf.PI)));
        }
    }
}
