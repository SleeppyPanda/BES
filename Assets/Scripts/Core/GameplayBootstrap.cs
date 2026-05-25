using UnityEngine;

/// <summary>
/// Ensures gameplay systems exist when entering the gameplay scene.
/// </summary>
public class GameplayBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (SceneTransitionManager.Instance == null)
        {
            GameObject transitionGo = new GameObject("SceneTransitionManager");
            transitionGo.AddComponent<SceneTransitionManager>();
        }

        if (FindFirstObjectByType<MobileTouchUIBuilder>() == null)
        {
            GameObject uiGo = new GameObject("MobileTouchUI");
            uiGo.AddComponent<MobileTouchUIBuilder>();
        }

        if (FindFirstObjectByType<GameplayMenuButton>() == null)
        {
            GameObject menuBtnGo = new GameObject("GameplayMenuButton");
            menuBtnGo.AddComponent<GameplayMenuButton>();
        }
    }
}
