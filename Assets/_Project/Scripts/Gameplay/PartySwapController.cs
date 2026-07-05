using BES.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BES.Gameplay
{
    /// <summary>
    /// Đổi nhân vật active bằng phím 1–4 (Genshin-style party swap).
    /// </summary>
    public class PartySwapController : MonoBehaviour
    {
        void Update()
        {
            if (GameplayInputGate.IsGameplayBlocked || PartyRoster.Instance == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame) PartyRoster.Instance.SetActiveSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) PartyRoster.Instance.SetActiveSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) PartyRoster.Instance.SetActiveSlot(2);
            if (keyboard.digit4Key.wasPressedThisFrame) PartyRoster.Instance.SetActiveSlot(3);
        }
    }
}
