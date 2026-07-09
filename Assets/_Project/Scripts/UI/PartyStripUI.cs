using BES.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class PartyStripUI : MonoBehaviour
    {
        [SerializeField] Image[] slotFrames = new Image[4];
        [SerializeField] Image[] portraits = new Image[4];
        [SerializeField] Button[] slotButtons = new Button[4];
        [SerializeField] TMP_Text[] slotNames = new TMP_Text[4];
        [SerializeField] TMP_Text[] slotNumbers = new TMP_Text[4];

        PartyRoster roster;
        CharacterPortraitManifest portraitManifest;

        void Awake()
        {
            roster = PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            portraitManifest = CharacterPortraitManifestLoader.Load();

            for (var i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null)
                    continue;
                var index = i;
                slotButtons[i].onClick.AddListener(() =>
                {
                    roster?.SetActiveSlot(index);
                    Refresh();
                });
            }
        }

        void OnEnable()
        {
            GameEvents.OnGameLoaded += Refresh;
            GameEvents.OnPartyChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameEvents.OnGameLoaded -= Refresh;
            GameEvents.OnPartyChanged -= Refresh;
        }

        public void Refresh()
        {
            roster ??= PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            portraitManifest ??= CharacterPortraitManifestLoader.Load();
            if (roster == null)
                return;

            for (var i = 0; i < 4; i++)
            {
                var member = roster.GetSlot(i);
                var isActive = i == roster.ActiveCharacterIndex;

                if (slotNumbers != null && i < slotNumbers.Length && slotNumbers[i] != null)
                {
                    slotNumbers[i].text = (i + 1).ToString();
                    slotNumbers[i].color = isActive ? new Color(1f, 0.92f, 0.55f) : new Color(1f, 1f, 1f, 0.85f);
                }

                if (slotNames != null && i < slotNames.Length && slotNames[i] != null)
                {
                    var definition = member != null ? roster.GetCharacterDefinition(member.characterId) : null;
                    var displayName = !string.IsNullOrEmpty(definition?.displayName)
                        ? definition.displayName
                        : member?.displayName;
                    slotNames[i].text = member != null && member.isUnlocked && !string.IsNullOrEmpty(displayName)
                        ? displayName
                        : "Character name";
                    slotNames[i].color = isActive ? new Color(0.95f, 0.9f, 0.65f) : Color.white;
                }

                if (portraits != null && i < portraits.Length && portraits[i] != null)
                {
                    if (member != null && member.isUnlocked && !string.IsNullOrEmpty(member.characterId))
                    {
                        var definition = roster.GetCharacterDefinition(member.characterId);
                        var sprite = definition?.portrait != null
                            ? definition.portrait
                            : portraitManifest?.GetPortrait(member.characterId);
                        portraits[i].sprite = sprite;
                        portraits[i].preserveAspect = true;
                        portraits[i].color = isActive ? new Color(1f, 0.95f, 0.7f) : Color.white;
                    }
                    else
                    {
                        portraits[i].sprite = null;
                        portraits[i].color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
                    }
                }

                if (slotFrames != null && i < slotFrames.Length && slotFrames[i] != null)
                {
                    slotFrames[i].color = isActive ? new Color(1f, 0.92f, 0.55f) : Color.white;
                    var ring = slotFrames[i].transform.Find("ActiveRing")?.GetComponent<Image>();
                    if (ring != null)
                        ring.color = isActive ? new Color(1f, 0.92f, 0.55f, 0.95f) : new Color(1f, 0.92f, 0.55f, 0f);
                }
            }
        }

        public void SetFrameSprites(Sprite frame)
        {
            if (slotFrames == null)
                return;

            foreach (var img in slotFrames)
            {
                if (img == null) continue;
                HUDPrimitiveStyles.ApplySolidPanel(img, HUDPrimitiveStyles.PartyPillBackground);
                if (frame != null)
                    HUDPrimitiveStyles.TryApplySmallFrame(img, frame);
            }
        }
    }
}
