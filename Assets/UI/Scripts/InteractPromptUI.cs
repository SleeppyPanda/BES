using BES.Core;
using UnityEngine;
using TMPro;

namespace BES.UI
{
    public class InteractPromptUI : MonoBehaviour
    {
        [SerializeField] GameObject promptRoot;
        [SerializeField] TMP_Text promptText;
        [SerializeField] string defaultMessage = "Nhấn F để tương tác";

        void Awake()
        {
            if (promptRoot != null)
                promptRoot.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.OnNpcInRange += ShowPrompt;
            GameEvents.OnNpcOutOfRange += HidePrompt;
            GameEvents.OnDialogueStarted += OnDialogueStarted;
        }

        void OnDisable()
        {
            GameEvents.OnNpcInRange -= ShowPrompt;
            GameEvents.OnNpcOutOfRange -= HidePrompt;
            GameEvents.OnDialogueStarted -= OnDialogueStarted;
        }

        void OnDialogueStarted(string _) => HidePrompt();

        void ShowPrompt(string npcName)
        {
            if (promptRoot != null)
                promptRoot.SetActive(true);

            if (promptText != null)
                promptText.text = string.IsNullOrEmpty(npcName)
                    ? defaultMessage
                    : $"{defaultMessage} — {npcName}";
        }

        void HidePrompt()
        {
            if (promptRoot != null)
                promptRoot.SetActive(false);
        }
    }
}
