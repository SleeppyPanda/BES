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
        CharacterDatabase characterDatabase;
        int activeCharacterIndex;

        public int ActiveCharacterIndex => Mathf.Clamp(activeCharacterIndex, 0, MaxPartySize - 1);
        public int MemberCount => MaxPartySize;

        public string ActiveCharacterId
        {
            get
            {
                var slot = GetSlot(ActiveCharacterIndex);
                return slot != null && !string.IsNullOrEmpty(slot.characterId) ? slot.characterId : "hero_01";
            }
        }

        public CharacterDefinition ActiveCharacter => GetCharacterDefinition(ActiveCharacterId);

        public PartyMemberSlot GetSlot(int index)
        {
            if (index < 0 || index >= MaxPartySize)
                return null;

            return members[index];
        }

        public PartyMemberSlot GetMember(int index) => GetSlot(index);

        public CharacterDefinition GetCharacterDefinition(string characterId)
        {
            characterDatabase ??= CharacterDatabaseLoader.Load();
            return characterDatabase?.Get(characterId);
        }

        public bool IsCharacterUnlocked(string characterId) =>
            !string.IsNullOrEmpty(characterId) && unlockedCharacterIds.Contains(characterId);

        public void UnlockCharacter(string characterId, string displayName)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            unlockedCharacterIds.Add(characterId);
            for (var i = 0; i < MaxPartySize; i++)
            {
                if (members[i] == null || members[i].characterId != characterId)
                    continue;

                members[i].displayName = ResolveDisplayName(characterId, displayName);
                members[i].isUnlocked = true;
                GameEvents.RaisePartyChanged();
                return;
            }

            GameEvents.RaisePartyChanged();
        }

        public IEnumerable<PartyMemberSlot> GetUnlockedRosterMembers()
        {
            characterDatabase ??= CharacterDatabaseLoader.Load();
            var emitted = new HashSet<string>();

            if (characterDatabase?.Characters != null)
            {
                foreach (var character in characterDatabase.Characters)
                {
                    if (character == null || !IsCharacterUnlocked(character.characterId))
                        continue;

                    emitted.Add(character.characterId);
                    yield return CreateMemberFromId(character.characterId);
                }
            }

            foreach (var id in unlockedCharacterIds)
            {
                if (emitted.Contains(id))
                    continue;

                yield return CreateMemberFromId(id);
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

        public void SetSlot(int index, string characterId, string displayName)
        {
            if (index < 0 || index >= MaxPartySize || string.IsNullOrEmpty(characterId))
                return;

            members[index] ??= new PartyMemberSlot();
            members[index].characterId = characterId;
            members[index].displayName = ResolveDisplayName(characterId, displayName);
            members[index].isUnlocked = IsCharacterUnlocked(characterId);

            if (index == activeCharacterIndex)
                RefreshActiveBuild();

            GameEvents.RaisePartyChanged();
        }

        public void ResetToDefaults()
        {
            unlockedCharacterIds.Clear();
            characterDatabase = CharacterDatabaseLoader.Load();
            var defaults = GetDefaultPartyIds();

            for (var i = 0; i < MaxPartySize; i++)
            {
                var id = i < defaults.Count ? defaults[i] : $"hero_{i + 1:00}";
                members[i] ??= new PartyMemberSlot();
                members[i].characterId = id;
                members[i].displayName = GetDisplayNameForId(id);
                members[i].isUnlocked = true;
                unlockedCharacterIds.Add(id);
            }

            activeCharacterIndex = 0;
            RefreshActiveBuild();
            GameEvents.RaisePartyChanged();
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
            characterDatabase = CharacterDatabaseLoader.Load();
            EnsureDefaults();
        }

        void EnsureDefaults()
        {
            if (unlockedCharacterIds.Count > 0)
                return;

            var defaults = GetDefaultPartyIds();
            for (var i = 0; i < MaxPartySize; i++)
            {
                var id = i < defaults.Count ? defaults[i] : $"hero_{i + 1:00}";
                members[i] ??= new PartyMemberSlot();

                if (string.IsNullOrEmpty(members[i].characterId))
                    members[i].characterId = id;

                members[i].displayName = GetDisplayNameForId(members[i].characterId);
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
            GameEvents.RaisePartyChanged();
        }

        static void RefreshActiveBuild()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerBuildStats>(out var build))
                build.Refresh();
        }

        PartyMemberSlot CreateMemberFromId(string id) => new()
        {
            characterId = id,
            displayName = GetDisplayNameForId(id),
            isUnlocked = true
        };

        string ResolveDisplayName(string id, string fallback)
        {
            var name = GetDisplayNameForId(id);
            return !string.IsNullOrEmpty(name) ? name : fallback;
        }

        string GetDisplayNameForId(string id)
        {
            characterDatabase ??= CharacterDatabaseLoader.Load();
            return characterDatabase != null ? characterDatabase.GetDisplayName(id) : id;
        }

        IReadOnlyList<string> GetDefaultPartyIds()
        {
            characterDatabase ??= CharacterDatabaseLoader.Load();
            var defaults = characterDatabase?.GetDefaultPartyIds();
            if (defaults != null && defaults.Count > 0)
                return defaults;

            return new[] { "hero_01", "hero_02", "hero_03", "hero_04" };
        }
    }
}
