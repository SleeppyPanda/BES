using BES.Core;
using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class TeamSetupUI : UIScreenBase
    {
        [SerializeField] Transform slotsContainer;
        [SerializeField] GameObject teamSlotPrefab;
        [SerializeField] Transform rosterPickerContainer;
        [SerializeField] Button confirmButton;
        [SerializeField] Button closeButton;

        PartyRoster roster;
        int selectedSlot;

        void Awake()
        {
            if (root == null)
                root = gameObject;
            Hide();
            roster = PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            roster ??= PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            RefreshSlots();
        }

        void RefreshSlots()
        {
            if (slotsContainer == null || teamSlotPrefab == null || roster == null)
                return;

            for (var i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);

            for (var i = 0; i < 4; i++)
            {
                var go = Instantiate(teamSlotPrefab, slotsContainer);
                var slot = go.GetComponent<UITeamSlot>();
                var member = roster.GetSlot(i);
                var definition = member != null ? roster.GetCharacterDefinition(member.characterId) : null;
                var displayName = !string.IsNullOrEmpty(definition?.displayName) ? definition.displayName : member?.displayName;
                slot?.Setup(i, displayName, definition?.portrait, OnSlotClicked);
            }
        }

        void OnSlotClicked(int index)
        {
            selectedSlot = index;
            ShowRosterPicker();
        }

        void ShowRosterPicker()
        {
            if (rosterPickerContainer == null || roster == null)
                return;

            for (var i = rosterPickerContainer.childCount - 1; i >= 0; i--)
                Destroy(rosterPickerContainer.GetChild(i).gameObject);

            foreach (var member in roster.GetUnlockedRosterMembers())
            {
                if (member == null)
                    continue;

                var definition = roster.GetCharacterDefinition(member.characterId);
                var displayName = !string.IsNullOrEmpty(definition?.displayName) ? definition.displayName : member.displayName;
                BESUIHelper.CreatePickerButton(rosterPickerContainer, displayName, () =>
                {
                    roster.AssignSlot(selectedSlot, member);
                    RefreshSlots();
                    for (var c = rosterPickerContainer.childCount - 1; c >= 0; c--)
                        Destroy(rosterPickerContainer.GetChild(c).gameObject);
                });
            }
        }

        void OnConfirm()
        {
            GameManager.Instance?.SaveGame();
            FindAnyObjectByType<PartyStripUI>()?.Refresh();
            Hide();
        }
    }

    static class BESUIHelper
    {
        public static GameObject CreatePickerButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject("PickerBtn");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 36);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.13f, 0.22f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(textRect);
            var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }
    }
}
