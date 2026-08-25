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
        public float currentHealth = -1f;
        public float maxHealth = -1f;
    }

    public class PartyRoster : MonoBehaviour
    {
        public static PartyRoster Instance { get; private set; }

        public const int MaxPartySize = 4;

        [SerializeField] PartyMemberSlot[] members = new PartyMemberSlot[MaxPartySize];

        readonly HashSet<string> unlockedCharacterIds = new();
        CharacterDatabase characterDatabase;
        int activeCharacterIndex;
        bool suppressHealthCapture;

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

        public bool IsCharacterUnlocked(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return false;

            foreach (var alias in CharacterIdentity.Aliases(characterId))
            {
                if (unlockedCharacterIds.Contains(alias))
                    return true;
            }

            return unlockedCharacterIds.Contains(characterId);
        }

        public void UnlockCharacter(string characterId, string displayName)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            var canonical = CharacterIdentity.Canonical(characterId);
            foreach (var alias in CharacterIdentity.Aliases(canonical))
                unlockedCharacterIds.Remove(alias);
            unlockedCharacterIds.Add(canonical);
            CharacterOwnership.SyncInventoryToken(canonical, true);

            for (var i = 0; i < MaxPartySize; i++)
            {
                if (members[i] == null || !CharacterIdentity.Same(members[i].characterId, canonical))
                    continue;

                members[i].characterId = canonical;
                members[i].displayName = ResolveDisplayName(canonical, displayName);
                members[i].isUnlocked = true;
                GameEvents.RaisePartyChanged();
                GameManager.Instance?.SaveGame();
                return;
            }

            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
        }

        public IEnumerable<PartyMemberSlot> GetUnlockedRosterMembers()
        {
            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in unlockedCharacterIds)
            {
                var canonical = CharacterIdentity.Canonical(id);
                if (string.IsNullOrEmpty(canonical) || !emitted.Add(canonical))
                    continue;
                yield return CreateMemberFromId(canonical);
            }
        }

        public void AssignSlot(int slotIndex, PartyMemberSlot member)
        {
            if (member == null || slotIndex < 0 || slotIndex >= MaxPartySize)
                return;
            if (!IsCharacterUnlocked(member.characterId))
                return;

            var canonical = CharacterIdentity.Canonical(member.characterId);
            SetSlot(slotIndex, canonical, member.displayName);
        }

        public void SetActiveSlot(int index)
        {
            if (index < 0 || index >= MaxPartySize)
                return;

            var slot = GetSlot(index);
            if (slot == null || !slot.isUnlocked || string.IsNullOrEmpty(slot.characterId))
                return;

            CaptureActiveHealth();
            activeCharacterIndex = index;
            suppressHealthCapture = true;
            RefreshActiveBuild();
            suppressHealthCapture = false;
            RestoreActiveHealth();
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
        }

        public void SetSlot(int index, string characterId, string displayName)
        {
            if (index < 0 || index >= MaxPartySize || string.IsNullOrEmpty(characterId))
                return;

            var canonical = CharacterIdentity.Canonical(characterId);
            members[index] ??= new PartyMemberSlot();
            members[index].characterId = canonical;
            members[index].displayName = ResolveDisplayName(canonical, displayName);
            members[index].isUnlocked = IsCharacterUnlocked(canonical);
            EnsureSlotHealth(index);

            if (index == activeCharacterIndex)
            {
                suppressHealthCapture = true;
                RefreshActiveBuild();
                suppressHealthCapture = false;
                RestoreActiveHealth();
            }

            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
        }

        public void ResetToDefaults()
        {
            unlockedCharacterIds.Clear();
            characterDatabase = CharacterDatabaseLoader.Load();
            ApplyStarterRoster(GetStarterCharacterIds());
            activeCharacterIndex = 0;
            suppressHealthCapture = true;
            RefreshActiveBuild();
            suppressHealthCapture = false;
            RestoreActiveHealth();
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

            ApplyStarterRoster(GetStarterCharacterIds());
        }

        void ApplyStarterRoster(IReadOnlyList<string> starterIds)
        {
            for (var i = 0; i < MaxPartySize; i++)
            {
                members[i] ??= new PartyMemberSlot();
                members[i].characterId = string.Empty;
                members[i].displayName = string.Empty;
                members[i].isUnlocked = false;
                members[i].currentHealth = -1f;
                members[i].maxHealth = -1f;
            }

            for (var i = 0; i < starterIds.Count && i < MaxPartySize; i++)
            {
                var id = CharacterIdentity.Canonical(starterIds[i]);
                unlockedCharacterIds.Add(id);
                CharacterOwnership.SyncInventoryToken(id, true);
                members[i].characterId = id;
                members[i].displayName = GetDisplayNameForId(id);
                members[i].isUnlocked = true;
                EnsureSlotHealth(i);
            }

            if (string.IsNullOrEmpty(CharacterOwnership.FocusedCharacterId) && starterIds.Count > 0)
                CharacterOwnership.Focus(starterIds[0]);
        }

        IReadOnlyList<string> GetStarterCharacterIds()
        {
            var starters = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var menu = CharacterIdentity.LoadMenuDatabase();

            foreach (var id in GetDefaultPartyIds())
            {
                var entry = CharacterIdentity.FindEntry(menu, id);
                if (entry == null || !seen.Add(entry.id))
                    continue;
                starters.Add(entry.id);
            }

            if (starters.Count > 0)
                return starters;

            if (menu?.characters != null)
            {
                foreach (var character in menu.characters)
                {
                    if (character == null || !character.playable || string.IsNullOrWhiteSpace(character.id) || !seen.Add(character.id))
                        continue;
                    starters.Add(character.id);
                    if (starters.Count >= MaxPartySize)
                        break;
                }
            }

            return starters.Count > 0 ? starters : new[] { "hero_01" };
        }

        void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += OnPlayerHealthChanged;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= OnPlayerHealthChanged;
        }

        void OnPlayerHealthChanged(float current, float max)
        {
            if (suppressHealthCapture)
                return;

            StoreHealth(ActiveCharacterIndex, current, max);
        }

        public void GetSlotHealth(int index, out float current, out float max)
        {
            current = 0f;
            max = 1f;

            var slot = GetSlot(index);
            if (slot == null)
                return;

            EnsureSlotHealth(index);
            current = Mathf.Clamp(slot.currentHealth, 0f, Mathf.Max(1f, slot.maxHealth));
            max = Mathf.Max(1f, slot.maxHealth);
        }

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;

            data.partySlotIds ??= new List<string>();
            data.partyHealth ??= new List<StringIntPair>();
            data.partyMaxHealth ??= new List<StringIntPair>();
            data.partySlotIds.Clear();
            data.partyHealth.Clear();
            data.partyMaxHealth.Clear();
            for (var i = 0; i < MaxPartySize; i++)
            {
                var slot = members[i];
                data.partySlotIds.Add(slot?.characterId ?? string.Empty);
                if (slot != null && !string.IsNullOrEmpty(slot.characterId))
                {
                    data.partyHealth.Add(new StringIntPair { key = slot.characterId, value = Mathf.CeilToInt(slot.currentHealth) });
                    data.partyMaxHealth.Add(new StringIntPair { key = slot.characterId, value = Mathf.CeilToInt(slot.maxHealth) });
                }
            }

            var uniqueOwned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in unlockedCharacterIds)
            {
                var canonical = CharacterIdentity.Canonical(id);
                if (!string.IsNullOrEmpty(canonical))
                    uniqueOwned.Add(canonical);
            }
            data.unlockedCharacterIds = new List<string>(uniqueOwned);
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
                {
                    var canonical = CharacterIdentity.Canonical(id);
                    if (!string.IsNullOrEmpty(canonical))
                    {
                        unlockedCharacterIds.Add(canonical);
                        CharacterOwnership.SyncInventoryToken(canonical, true);
                    }
                }
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
                    members[i].characterId = CharacterIdentity.Canonical(id);
                    members[i].displayName = GetDisplayNameForId(members[i].characterId);
                    members[i].isUnlocked = IsCharacterUnlocked(members[i].characterId);
                    EnsureSlotHealth(i);
                }
            }

            var savedHealth = data.partyHealth != null
                ? SaveDataUtility.FromPairs(data.partyHealth)
                : new Dictionary<string, int>();
            var savedMaxHealth = data.partyMaxHealth != null
                ? SaveDataUtility.FromPairs(data.partyMaxHealth)
                : new Dictionary<string, int>();
            for (var i = 0; i < MaxPartySize; i++)
            {
                var slot = members[i];
                if (slot == null || string.IsNullOrEmpty(slot.characterId))
                    continue;

                if (savedMaxHealth.TryGetValue(slot.characterId, out var max))
                    slot.maxHealth = Mathf.Max(1f, max);
                if (savedHealth.TryGetValue(slot.characterId, out var current))
                    slot.currentHealth = Mathf.Clamp(current, 0f, Mathf.Max(1f, slot.maxHealth));
            }

            activeCharacterIndex = Mathf.Clamp(data.activeCharacterIndex, 0, MaxPartySize - 1);
            suppressHealthCapture = true;
            RefreshActiveBuild();
            suppressHealthCapture = false;
            RestoreActiveHealth();
            GameEvents.RaisePartyChanged();
        }

        static void RefreshActiveBuild()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerBuildStats>(out var build))
                build.Refresh();
        }

        void CaptureActiveHealth()
        {
            var stats = FindPlayerStats();
            if (stats != null)
                StoreHealth(ActiveCharacterIndex, stats.CurrentHealth, stats.MaxHealth);
        }

        void StoreHealth(int index, float current, float max)
        {
            var slot = GetSlot(index);
            if (slot == null)
                return;

            slot.maxHealth = Mathf.Max(1f, max);
            slot.currentHealth = Mathf.Clamp(current, 0f, slot.maxHealth);
        }

        void RestoreActiveHealth()
        {
            var stats = FindPlayerStats();
            var slot = GetSlot(ActiveCharacterIndex);
            if (stats == null || slot == null)
                return;

            EnsureSlotHealth(ActiveCharacterIndex);
            var ratio = slot.maxHealth > 0f ? slot.currentHealth / slot.maxHealth : 1f;
            slot.maxHealth = Mathf.Max(1f, stats.MaxHealth);
            slot.currentHealth = Mathf.Clamp(slot.maxHealth * ratio, 0f, slot.maxHealth);
            stats.LoadState(slot.currentHealth, stats.CurrentMana);
        }

        void EnsureSlotHealth(int index)
        {
            var slot = GetSlot(index);
            if (slot == null)
                return;

            var definition = GetCharacterDefinition(slot.characterId);
            var max = Mathf.Max(1f, definition != null ? definition.baseHealth : 100f);
            if (slot.maxHealth <= 0f)
                slot.maxHealth = max;
            if (slot.currentHealth < 0f)
                slot.currentHealth = slot.maxHealth;
        }

        static PlayerStats FindPlayerStats()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null && player.TryGetComponent<PlayerStats>(out var stats) ? stats : null;
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
