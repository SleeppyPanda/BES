using BES.Narrative;
using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    public static class GameplayInputGate
    {
        public static bool IsGameplayBlocked
        {
            get
            {
                if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)
                    return true;

                var nav = Object.FindAnyObjectByType<UINavigationController>();
                return nav != null && nav.IsBlockingGameplay;
            }
        }
    }
}
