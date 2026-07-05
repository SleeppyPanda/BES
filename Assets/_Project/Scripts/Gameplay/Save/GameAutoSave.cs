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
                GameManager.Instance?.SaveGame();
            }
        }

        void OnApplicationQuit()
        {
            GameManager.Instance?.SaveGame();
        }
    }
}
