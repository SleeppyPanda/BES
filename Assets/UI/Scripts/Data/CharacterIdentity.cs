using System;
using System.Collections.Generic;
using BES.UI.Menu;
using UnityEngine;

namespace BES.UI
{
    public static class CharacterIdentity
    {
        static readonly Dictionary<string, string> aliasToCanonical = new(StringComparer.OrdinalIgnoreCase);
        static bool mapsBuilt;

        public static string Canonical(string characterId, MenuContentDatabase menuDatabase = null)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return string.Empty;

            EnsureMaps(menuDatabase);
            var normalized = Normalize(characterId);
            if (aliasToCanonical.TryGetValue(normalized, out var mapped) && !string.IsNullOrEmpty(mapped))
                normalized = mapped;

            var menu = ResolveMenuDatabase(menuDatabase);
            var menuEntry = FindMenuEntryExact(menu, normalized) ?? FindMenuEntryExact(menu, characterId);
            if (menuEntry != null)
                return menuEntry.id;

            var combat = CharacterDatabaseLoader.Load()?.GetRaw(normalized) ?? CharacterDatabaseLoader.Load()?.GetRaw(characterId);
            if (combat != null)
            {
                var byName = FindMenuEntryByName(menu, combat.displayName);
                if (byName != null)
                    return byName.id;
                return combat.characterId;
            }

            return normalized;
        }

        public static string CombatId(string characterId, MenuContentDatabase menuDatabase = null)
        {
            var canonical = Canonical(characterId, menuDatabase);
            if (string.IsNullOrEmpty(canonical))
                return string.Empty;

            EnsureMaps(menuDatabase);
            var combatDb = CharacterDatabaseLoader.Load();
            if (combatDb?.GetRaw(canonical) != null)
                return canonical;

            foreach (var pair in aliasToCanonical)
            {
                if (!string.Equals(pair.Value, canonical, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (combatDb?.GetRaw(pair.Key) != null)
                    return pair.Key;
            }

            var menu = FindMenuEntryExact(ResolveMenuDatabase(menuDatabase), canonical);
            var byName = combatDb?.GetByDisplayName(menu?.displayName);
            return byName != null ? byName.characterId : canonical;
        }

        public static bool Same(string left, string right, MenuContentDatabase menuDatabase = null)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;
            return string.Equals(Canonical(left, menuDatabase), Canonical(right, menuDatabase), StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> Aliases(string characterId, MenuContentDatabase menuDatabase = null)
        {
            var canonical = Canonical(characterId, menuDatabase);
            if (string.IsNullOrEmpty(canonical))
                yield break;

            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { canonical };
            yield return canonical;

            EnsureMaps(menuDatabase);
            foreach (var pair in aliasToCanonical)
            {
                if (!string.Equals(pair.Value, canonical, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(pair.Key, canonical, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (emitted.Add(pair.Key))
                    yield return pair.Key;
                if (emitted.Add(pair.Value))
                    yield return pair.Value;
            }

            var combat = CombatId(canonical, menuDatabase);
            if (!string.IsNullOrEmpty(combat) && emitted.Add(combat))
                yield return combat;
        }

        public static CharacterEntry FindEntry(MenuContentDatabase menuDatabase, string characterId)
        {
            var menu = ResolveMenuDatabase(menuDatabase);
            if (menu?.characters == null || string.IsNullOrWhiteSpace(characterId))
                return null;

            var canonical = Canonical(characterId, menu);
            return FindMenuEntryExact(menu, canonical) ?? FindMenuEntryExact(menu, characterId);
        }

        public static MenuContentDatabase LoadMenuDatabase()
        {
            var database = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
            if (database == null)
                database = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            return database;
        }

        static void EnsureMaps(MenuContentDatabase menuDatabase)
        {
            if (mapsBuilt)
                return;

            mapsBuilt = true;
            Register("hero_01", "elio");
            Register("elio", "elio");
            Register("hero_02", "sahure");
            Register("sahure", "sahure");
            Register("hero_03", "luna");
            Register("luna", "luna");
            Register("hero_04", "sol");
            Register("sol", "sol");
            Register("char_limited_01", "char_limited_01");
            Register("hero_05", "hero_05");

            var menu = ResolveMenuDatabase(menuDatabase);
            if (menu?.characters != null)
            {
                foreach (var character in menu.characters)
                {
                    if (character == null || string.IsNullOrWhiteSpace(character.id))
                        continue;
                    Register(character.id, character.id);
                    if (!string.IsNullOrWhiteSpace(character.displayName))
                        Register(character.displayName, character.id);
                }
            }

            DropAliasesThatDoNotExistInMenu(menu);
        }

        static void DropAliasesThatDoNotExistInMenu(MenuContentDatabase menu)
        {
            if (menu?.characters == null)
                return;

            var invalid = new List<string>();
            foreach (var pair in aliasToCanonical)
            {
                if (FindMenuEntryExact(menu, pair.Value) != null)
                    continue;
                if (CharacterDatabaseLoader.Load()?.GetRaw(pair.Value) != null)
                    continue;
                invalid.Add(pair.Key);
            }

            foreach (var key in invalid)
                aliasToCanonical.Remove(key);
        }

        static void Register(string alias, string canonical)
        {
            if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(canonical))
                return;
            aliasToCanonical[Normalize(alias)] = Normalize(canonical);
        }

        static string Normalize(string value)
        {
            var id = value.Trim();
            if (id.StartsWith("wish_", StringComparison.OrdinalIgnoreCase))
                id = id[5..];
            if (id.StartsWith("character_", StringComparison.OrdinalIgnoreCase))
                id = id["character_".Length..];
            return id.Trim();
        }

        static MenuContentDatabase ResolveMenuDatabase(MenuContentDatabase menuDatabase) =>
            menuDatabase != null ? menuDatabase : LoadMenuDatabase();

        static CharacterEntry FindMenuEntryExact(MenuContentDatabase menu, string id)
        {
            if (menu?.characters == null || string.IsNullOrWhiteSpace(id))
                return null;
            return menu.characters.Find(character =>
                character != null && string.Equals(character.id?.Trim(), id.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        static CharacterEntry FindMenuEntryByName(MenuContentDatabase menu, string displayName)
        {
            if (menu?.characters == null || string.IsNullOrWhiteSpace(displayName))
                return null;
            return menu.characters.Find(character =>
                character != null && string.Equals(character.displayName?.Trim(), displayName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
