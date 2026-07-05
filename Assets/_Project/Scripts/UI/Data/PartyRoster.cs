using System;
using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    [Serializable]
    public class PartyMemberSlot
    {
        public string characterId;
        public string displayName;
        public bool isUnlocked = true;
    }

    public class PartyRoster : MonoBehaviour
    {
        public static PartyRoster Instance { get; private set; }

        public const int MaxPartySize = 4;

        [SerializeField] PartyMemberSlot[] members = new PartyMemberSlot[MaxPartySize];
        readonly HashSet<string> unlockedCharacterIds = new();
        int activeCharacterIndex;

        public int ActiveCharacterIndex => Mathf.Clamp(activeCharacterIndex, 0, MaxPartySize - 1);

        public string ActiveCharacterId
        {
            get
            {
                var slot = GetSlot(ActiveCharacterIndex);
                return slot != null && !string.IsNullOrEmpty(slot.characterId) ? slot.characterId : "hero_01";
            }
        }

        public PartyMemberSlot GetSlot(int index)
        {
            if (index < 0 || index >= MaxPartySize)
                return null;
            return members[index];
        }

        public int MemberCount => MaxPartySize;

        public PartyMemberSlot GetMember(int index) => GetSlot(index);

        public bool IsCharacterUnlocked(string characterId) =>
            !string.IsNullOrEmpty(characterId) && unlockedCharacterIds.Contains(characterId);

        public void UnlockCharacter(string characterId, string displayName)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            unlockedCharacterIds.Add(characterId);
            for (var i = 0; i < MaxPartySize; i++)
            {
                if (members[i] != null && members[i].characterId == characterId)
                {
                    members[i].isUnlocked = true;
                    return;
                }
            }
        }

        public IEnumerable<PartyMemberSlot> GetUnlockedRosterMembers()
        {
            foreach (var id in unlockedCharacterIds)
            {
                yield return new PartyMemberSlot
                {
                    characterId = id,
                    displayName = GetDisplayNameForId(id),
                    isUnlocked = true
                };
            }
        }

        public void AssignSlot(int slotIndex, PartyMemberSlot member)
        {
            if (member == null || slotIndex < 0 || slotIndex >= MaxPartySize)
                return;
            if (!IsCharacterUnlocked(member.characterId))
                return;
            SetSlot(slotIndex, member.characterId, member.displayName);
        }

        public void SetActiveSlot(int index)
        {
            if (index < 0 || index >= MaxPartySize)
                return;

            var slot = GetSlot(index);
            if (slot == null || !slot.isUnlocked || string.IsNullOrEmpty(slot.characterId))
                return;

            activeCharacterIndex = index;
            RefreshActiveBuild();
            GameEvents.RaisePartyChanged();
        }

        static void RefreshActiveBuild()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerBuildStats>(out var build))
                build.Refresh();
        }

        public void SetSlot(int index, string characterId, string displayName)
        {
            if (index < 0 || index >= MaxPartySize)
                return;

            members[index] ??= new PartyMemberSlot();
            members[index].characterId = characterId;
            members[index].displayName = displayName;
            members[index].isUnlocked = IsCharacterUnlocked(characterId);
        }

        public void ResetToDefaults()
        {
            unlockedCharacterIds.Clear();
            var defaults = new[] { "hero_01", "hero_02", "hero_03", "hero_04" };
            var names = new[] { "Main Character", "Ally A", "Ally B", "Ally C" };
            for (var i = 0; i < MaxPartySize; i++)
            {
                members[i] ??= new PartyMemberSlot();
                members[i].characterId = defaults[i];
                members[i].displayName = names[i];
                members[i].isUnlocked = true;
                unlockedCharacterIds.Add(defaults[i]);
            }
            activeCharacterIndex = 0;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDefaults();
        }

        void EnsureDefaults()
        {
            if (unlockedCharacterIds.Count > 0)
                return;

            var defaults = new[] { "hero_01", "hero_02", "hero_03", "hero_04" };
            var names = new[] { "Main Character", "Ally A", "Ally B", "Ally C" };
            for (var i = 0; i < MaxPartySize; i++)
            {
                members[i] ??= new PartyMemberSlot();
                if (string.IsNullOrEmpty(members[i].characterId))
                {
                    members[i].characterId = defaults[i];
                    members[i].displayName = names[i];
                }

                members[i].isUnlocked = true;
                unlockedCharacterIds.Add(members[i].characterId);
            }
        }

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;

            data.partySlotIds.Clear();
            for (var i = 0; i < MaxPartySize; i++)
            {
                var slot = members[i];
                data.partySlotIds.Add(slot?.characterId ?? string.Empty);
            }

            data.unlockedCharacterIds = new List<string>(unlockedCharacterIds);
            data.activeCharacterIndex = activeCharacterIndex;
        }

        public void ImportFromSave(SaveData data)
        {
            if (data == null)
                return;

            unlockedCharacterIds.Clear();
            if (data.unlockedCharacterIds != null && data.unlockedCharacterIds.Count > 0)
            {
                foreach (var id in data.unlockedCharacterIds)
                    unlockedCharacterIds.Add(id);
            }
            else
            {
                EnsureDefaults();
            }

            if (data.partySlotIds != null && data.partySlotIds.Count > 0)
            {
                for (var i = 0; i < MaxPartySize && i < data.partySlotIds.Count; i++)
                {
                    var id = data.partySlotIds[i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    members[i] ??= new PartyMemberSlot();
                    members[i].characterId = id;
                    members[i].displayName = GetDisplayNameForId(id);
                    members[i].isUnlocked = IsCharacterUnlocked(id);
                }
            }

            activeCharacterIndex = Mathf.Clamp(data.activeCharacterIndex, 0, MaxPartySize - 1);
            RefreshActiveBuild();
        }

        static string GetDisplayNameForId(string id) => id switch
        {
            "hero_01" => "Main Character",
            "hero_02" => "Ally A",
            "hero_03" => "Ally B",
            "hero_04" => "Ally C",
            "char_limited_01" => "Limited Hero",
            _ => id
        };
    }
}
