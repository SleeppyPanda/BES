using System.Collections.Generic;
using UnityEngine;

namespace BES.Narrative
{
    public class QuestMarker : MonoBehaviour
    {
        static readonly Dictionary<string, Transform> Markers = new();

        [SerializeField] string markerId;

        public string MarkerId => markerId;
        public Vector3 WorldPosition => transform.position;

        public void SetMarkerId(string id)
        {
            if (isActiveAndEnabled && !string.IsNullOrEmpty(markerId) &&
                Markers.TryGetValue(markerId, out var existing) && existing == transform)
                Markers.Remove(markerId);

            markerId = id;

            if (isActiveAndEnabled && !string.IsNullOrEmpty(markerId))
                Markers[markerId] = transform;
        }

        void OnEnable()
        {
            if (!string.IsNullOrEmpty(markerId))
                Markers[markerId] = transform;
        }

        void OnDisable()
        {
            if (!string.IsNullOrEmpty(markerId) && Markers.TryGetValue(markerId, out var t) && t == transform)
                Markers.Remove(markerId);
        }

        public static Transform GetMarker(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return Markers.TryGetValue(id, out var marker) ? marker : null;
        }

        public static IReadOnlyCollection<Transform> AllMarkers => Markers.Values;
    }
}
