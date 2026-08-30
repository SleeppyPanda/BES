using UnityEngine;

namespace BES.Gameplay
{
    /// <summary>
    /// Prevents root joint translation drift in custom FBX animations where translation 
    /// is authored on the hips/root bone, eliminating snapping/jittering during walk loops.
    /// </summary>
    public class RootMotionFixer : MonoBehaviour
    {
        private Transform hips;
        private Vector3 initialLocalPos;

        void Start()
        {
            hips = FindHipsRecursively(transform);
            if (hips != null)
            {
                initialLocalPos = hips.localPosition;
            }
        }

        void LateUpdate()
        {
            if (hips != null)
            {
                // Force X and Z local positions to remain centered while allowing natural Y-axis bobbing
                Vector3 current = hips.localPosition;
                hips.localPosition = new Vector3(initialLocalPos.x, current.y, initialLocalPos.z);
            }
        }

        private Transform FindHipsRecursively(Transform current)
        {
            string lowerName = current.name.ToLower();
            if (lowerName == "hips" || lowerName == "root" || lowerName == "pelvis" || lowerName == "bip001")
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = FindHipsRecursively(current.GetChild(i));
                if (found != null) return found;
            }
            return null;
        }
    }
}
