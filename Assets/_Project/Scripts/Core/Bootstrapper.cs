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
                if (existingGameManager.Save.HasSave && !existingGameManager.Save.LoadedFromContinue)
                    existingGameManager.Save.Load();
                return;
            }

            var root = new GameObject("[BES] GameSystems");
            var gameManager = root.AddComponent<GameManager>();
            root.AddComponent<SceneLoader>();
            root.AddComponent<PartySwapController>();
            root.AddComponent<PlayerWallet>();
            root.AddComponent<EquippedWeaponState>();
            root.AddComponent<PartyRoster>();
            root.AddComponent<MetaProgressState>();
            root.AddComponent<GachaPityState>();
            if (gameManager.Save.HasSave && !gameManager.Save.LoadedFromContinue)
                gameManager.Save.Load();
        }
    }
}
