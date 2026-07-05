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
            if (Object.FindAnyObjectByType<GameManager>() != null)
                return;

            var root = new GameObject("[BES] GameSystems");
            root.AddComponent<GameManager>();
            root.AddComponent<SceneLoader>();
            root.AddComponent<PlayerWallet>();
            root.AddComponent<EquippedWeaponState>();
            root.AddComponent<PartyRoster>();
            root.AddComponent<MetaProgressState>();
            root.AddComponent<GachaPityState>();
        }
    }
}
