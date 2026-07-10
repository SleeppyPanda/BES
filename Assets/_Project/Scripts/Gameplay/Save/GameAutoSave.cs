using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    public class GameAutoSave : MonoBehaviour
    {
        [SerializeField] float autoSaveInterval = 120f;

        float timer;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= autoSaveInterval)
            {
                timer = 0f;
                SaveNow();
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveNow();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        void SaveNow()
        {
            GameManager.Instance?.SaveGame();
        }
    }
}
