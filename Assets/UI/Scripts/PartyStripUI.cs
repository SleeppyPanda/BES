using BES.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class PartyStripUI : MonoBehaviour
    {
        [SerializeField] RectTransform[] slotRoots = new RectTransform[4];
        [SerializeField] Image[] slotFrames = new Image[4];
        [SerializeField] Image[] portraits = new Image[4];
        [SerializeField] Button[] slotButtons = new Button[4];
        [SerializeField] TMP_Text[] slotNames = new TMP_Text[4];
        [SerializeField] TMP_Text[] slotNumbers = new TMP_Text[4];
        [SerializeField] Slider[] healthBars = new Slider[4];
        [SerializeField] float activeScale = 1.2f;

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
            GameEvents.OnPlayerHealthChanged += OnPlayerHealthChanged;
            Refresh();
        }

        void OnDisable()
        {
            GameEvents.OnGameLoaded -= Refresh;
            GameEvents.OnPartyChanged -= Refresh;
            GameEvents.OnPlayerHealthChanged -= OnPlayerHealthChanged;
        }

        void OnPlayerHealthChanged(float _, float __) => Refresh();

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
                bool hasMember = member != null && member.isUnlocked && !string.IsNullOrEmpty(member.characterId);

                // Dưới góc độ thiết kế chuyên nghiệp, ẩn các slot trống không có nhân vật
                if (slotRoots != null && i < slotRoots.Length && slotRoots[i] != null)
                {
                    slotRoots[i].gameObject.SetActive(hasMember);
                    slotRoots[i].localScale = isActive ? Vector3.one * activeScale : Vector3.one;
                }

                if (slotNumbers != null && i < slotNumbers.Length && slotNumbers[i] != null)
                {
                    slotNumbers[i].gameObject.SetActive(hasMember);
                    slotNumbers[i].text = (i + 1).ToString();
                    slotNumbers[i].color = isActive ? new Color(1f, 0.92f, 0.55f) : new Color(1f, 1f, 1f, 0.85f);
                }

                if (!hasMember) continue;

                if (slotNames != null && i < slotNames.Length && slotNames[i] != null)
                {
                    var definition = roster.GetCharacterDefinition(member.characterId);
                    var displayName = !string.IsNullOrEmpty(definition?.displayName)
                        ? definition.displayName
                        : member?.displayName;
                    slotNames[i].text = displayName;
                    slotNames[i].color = isActive ? new Color(0.95f, 0.9f, 0.65f) : Color.white;
                }

                if (portraits != null && i < portraits.Length && portraits[i] != null)
                {
                    var definition = roster.GetCharacterDefinition(member.characterId);
                    var sprite = definition?.portrait != null
                        ? definition.portrait
                        : portraitManifest?.GetPortrait(member.characterId);
                    portraits[i].sprite = sprite;
                    portraits[i].preserveAspect = true;
                    portraits[i].color = isActive ? new Color(1f, 0.95f, 0.7f) : Color.white;
                }

                if (slotFrames != null && i < slotFrames.Length && slotFrames[i] != null)
                {
                    // Thiết lập màu tối trong suốt tinh tế thay vì màu trắng bệch thô kệch
                    slotFrames[i].color = isActive ? new Color(0.12f, 0.14f, 0.2f, 0.85f) : new Color(0.04f, 0.05f, 0.08f, 0.6f);
                    
                    var ring = slotFrames[i].transform.Find("ActiveRing")?.GetComponent<Image>();
                    if (ring != null)
                    {
                        // Vành hào quang vàng bao quanh ảnh đại diện khi active
                        ring.color = isActive ? new Color(1f, 0.82f, 0.35f, 1f) : new Color(0f, 0f, 0f, 0f);
                    }
                }

                if (healthBars != null && i < healthBars.Length && healthBars[i] != null)
                {
                    roster.GetSlotHealth(i, out var currentHealth, out var maxHealth);
                    healthBars[i].maxValue = maxHealth;
                    healthBars[i].value = currentHealth;
                    healthBars[i].gameObject.SetActive(true);
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
