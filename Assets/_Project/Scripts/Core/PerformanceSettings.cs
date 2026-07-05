using UnityEngine;

namespace BES.Core
{
    public class PerformanceSettings : MonoBehaviour
    {
        [SerializeField] int targetFrameRate = 60;
        [SerializeField] bool vSync = true;

        void Awake()
        {
            QualitySettings.vSyncCount = vSync ? 1 : 0;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
