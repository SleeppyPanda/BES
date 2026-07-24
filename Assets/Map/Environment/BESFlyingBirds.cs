using UnityEngine;

namespace BES.Environment
{
    public class BESFlyingBirds : MonoBehaviour
    {
        public Transform orbitCenter;
        public float radius = 18f;
        public float heightOffset = 12f;
        public float speed = 18f;

        private float phase;

        private void Awake()
        {
            phase = Random.Range(0f, 360f);
        }

        private void Update()
        {
            if (!orbitCenter) return;

            phase += speed * Time.deltaTime;
            float rad = phase * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, heightOffset + Mathf.Sin(rad * 1.7f), Mathf.Sin(rad) * radius);
            transform.position = orbitCenter.position + offset;
            transform.forward = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }
    }
}
