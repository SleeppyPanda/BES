using BES.Gameplay;
using BES.UI;
using UnityEngine;

namespace BES.Core
{
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var existingGameManager = Object.FindAnyObjectByType<GameManager>();
            if (existingGameManager != null)
            {
                if (Object.FindAnyObjectByType<PartySwapController>() == null)
                    existingGameManager.gameObject.AddComponent<PartySwapController>();
                return;
            }

            var root = new GameObject("[BES] GameSystems");
            root.AddComponent<GameManager>();
            root.AddComponent<SceneLoader>();
            root.AddComponent<PartySwapController>();
            root.AddComponent<PlayerWallet>();
            root.AddComponent<EquippedWeaponState>();
            root.AddComponent<PartyRoster>();
            root.AddComponent<MetaProgressState>();
            root.AddComponent<GachaPityState>();
        }
    }
}
