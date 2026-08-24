using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BES.UI;
using BES.UI.Menu;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BES.EditorTools
{
    public static class CharacterInfoDatabaseImporter
    {
        const string MenuDatabasePath = "Assets/Scenes/MenuContentDatabase.asset";
        const string CharacterDatabasePath = "Assets/Resources/Data/CharacterDatabase.asset";
        public const string CharacterInfoAssetPath = "Assets/Resources/Character/info";
        const string ImportHashKey = "BES.CharacterInfoDatabaseImporter.SourceHash";

        [DidReloadScripts]
        static void AutoImportAfterScriptsReload()
        {
            EditorApplication.delayCall += ImportIfChanged;
        }

        [MenuItem("BES/Characters/Import Character Info To MenuContentDatabase")]
        public static void ImportNow()
        {
            Import(force: true);
        }

        public static void ImportIfChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Import(force: false);
        }

        static void Import(bool force)
        {
            if (!File.Exists(CharacterInfoAssetPath)) return;

            var hash = ComputeSourceHash();
            if (!force && EditorPrefs.GetString(ImportHashKey, string.Empty) == hash) return;

            var records = Parse(File.ReadAllText(CharacterInfoAssetPath, Encoding.UTF8));
            if (records.Count == 0) return;

            var database = AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(MenuDatabasePath);
            if (database == null)
            {
                Debug.LogWarning($"[BES] Cannot import character info. Missing database at {MenuDatabasePath}.");
                return;
            }

            database.characters ??= new List<CharacterEntry>();
            var existing = new Dictionary<string, CharacterEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var character in database.characters)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.id)) continue;
                existing[character.id] = character;
                if (!string.IsNullOrWhiteSpace(character.displayName))
                    existing[character.displayName] = character;
            }

            database.characters.Clear();
            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Id)) continue;
                existing.TryGetValue(record.Id, out var old);
                if (old == null && !string.IsNullOrWhiteSpace(record.DisplayName))
                    existing.TryGetValue(record.DisplayName, out old);

                var entry = new CharacterEntry
                {
                    id = record.Id,
                    displayName = string.IsNullOrWhiteSpace(record.DisplayName) ? record.Id : record.DisplayName,
                    description = record.Description,
                    faction = record.Faction,
                    playable = record.Playable,
                    element = record.Element,
                    weaponType = record.Weapon,
                    skillType = record.SkillType,
                    normalAttack = record.NormalAttack,
                    skillDescription = record.Skill,
                    passiveDescription = record.Passive,
                    portrait = old?.portrait,
                    cardBackground = old?.cardBackground,
                    fullBody = old?.fullBody,
                    chibi = old?.chibi,
                    attackEffectPrefabs = AttackEffectsForRecord(record, old),
                    attackEffectOffset = old?.attackEffectOffset ?? Vector3.zero,
                    attackEffectScale = ResolveEffectScale(old),
                    elementIcon = old?.elementIcon,
                    equippedArtifacts = old?.equippedArtifacts != null ? new List<Sprite>(old.equippedArtifacts) : new List<Sprite>(),
                    attributes = BuildAttributes(record),
                    revealVideoClip = old?.revealVideoClip,
                    rarity = Mathf.Clamp(record.Rarity <= 0 ? 3 : record.Rarity, 1, 6),
                    starLevel = old?.starLevel ?? 0,
                    level = old?.level > 0 ? old.level : 1,
                    combatPower = old?.combatPower ?? 0,
                    constellation = old?.constellation ?? 0,
                    quality = old?.quality > 0 ? old.quality : 1,
                    affinity = old?.affinity ?? 0,
                    maxHealth = old?.maxHealth > 0 ? old.maxHealth : DefaultHealth(record),
                    attack = old?.attack > 0 ? old.attack : DefaultAttack(record)
                };

                database.characters.Add(entry);
            }

            EditorUtility.SetDirty(database);
            SyncCharacterDatabase(records, database);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(ImportHashKey, hash);
            Debug.Log($"[BES] Imported {database.characters.Count} characters from Assets/Resources/Character/info into MenuContentDatabase.asset.");
        }

        static Vector3 ResolveEffectScale(CharacterEntry old)
        {
            if (old == null || old.attackEffectScale == Vector3.zero) return Vector3.one;
            return old.attackEffectScale;
        }

        static List<GameObject> AttackEffectsForRecord(CharacterRecord record, CharacterEntry old)
        {
            if (old?.attackEffectPrefabs != null && old.attackEffectPrefabs.Count > 0)
                return new List<GameObject>(old.attackEffectPrefabs);

            var result = new List<GameObject>();
            switch (record.Id)
            {
                case "elio":
                    AddEffect(result, "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR3 Hit Fire B (Air).prefab");
                    AddEffect(result, "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Bomb_Red.prefab");
                    break;
                case "aurelian":
                    AddEffect(result, "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Fire/CFXR4 Sword Hit FIRE (Cross).prefab");
                    AddEffect(result, "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_1_Bam.prefab");
                    break;
                case "khepraen":
                    AddEffect(result, "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/CFXR3 Hit Electric C (Air).prefab");
                    AddEffect(result, "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_1_Zap.prefab");
                    break;
            }
            return result;
        }

        static void AddEffect(List<GameObject> result, string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null) result.Add(prefab);
        }

        static void SyncCharacterDatabase(List<CharacterRecord> records, MenuContentDatabase menuDatabase)
        {
            var characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);
            if (characterDatabase == null)
            {
                EnsureFolder("Assets/Resources");
                EnsureFolder("Assets/Resources/Data");
                characterDatabase = ScriptableObject.CreateInstance<CharacterDatabase>();
                AssetDatabase.CreateAsset(characterDatabase, CharacterDatabasePath);
            }

            characterDatabase.characters ??= new List<CharacterDefinition>();
            var old = new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var character in characterDatabase.characters)
                if (character != null && !string.IsNullOrWhiteSpace(character.characterId))
                    old[character.characterId] = character;

            characterDatabase.characters.Clear();
            foreach (var record in records)
            {
                if (!record.Playable || string.IsNullOrWhiteSpace(record.Id)) continue;
                old.TryGetValue(record.Id, out var existing);
                var menuCharacter = menuDatabase.FindCharacter(record.Id);
                characterDatabase.characters.Add(new CharacterDefinition
                {
                    characterId = record.Id,
                    displayName = string.IsNullOrWhiteSpace(record.DisplayName) ? record.Id : record.DisplayName,
                    rarity = Mathf.Clamp(record.Rarity <= 0 ? 3 : record.Rarity, 3, 5),
                    level = existing?.level > 0 ? existing.level : 1,
                    baseAttack = existing?.baseAttack > 0 ? existing.baseAttack : DefaultAttack(record),
                    baseHealth = existing?.baseHealth > 0 ? existing.baseHealth : DefaultHealth(record),
                    baseDefense = existing?.baseDefense > 0 ? existing.baseDefense : DefaultDefense(record),
                    baseMana = existing?.baseMana > 0 ? existing.baseMana : 100f,
                    critRate = existing?.critRate > 0 ? existing.critRate : DefaultCritRate(record),
                    critDamage = existing?.critDamage > 0 ? existing.critDamage : 1.5f,
                    portrait = menuCharacter?.portrait,
                    gameplayPrefab = existing?.gameplayPrefab,
                    testVisualColor = existing?.testVisualColor ?? Color.white,
                    testVisualScale = existing?.testVisualScale ?? Vector3.one,
                    leftClickAttackId = existing?.leftClickAttackId,
                    rightClickAttackId = existing?.rightClickAttackId,
                    skill1Id = string.IsNullOrWhiteSpace(existing?.skill1Id) ? SanitizeId(record.SkillType) : existing.skill1Id,
                    skill2Id = existing?.skill2Id,
                    skill1Icon = existing?.skill1Icon,
                    skill2Icon = existing?.skill2Icon,
                    duplicateShardReward = existing?.duplicateShardReward > 0 ? existing.duplicateShardReward : record.Rarity >= 5 ? 5 : record.Rarity == 4 ? 3 : 1,
                    constellationShardCosts = existing?.constellationShardCosts != null && existing.constellationShardCosts.Count > 0
                        ? new List<int>(existing.constellationShardCosts)
                        : new List<int> { 1, 1, 1, 1, 1, 1 },
                    skillUnlocks = existing?.skillUnlocks != null ? new List<CharacterSkillUnlock>(existing.skillUnlocks) : new List<CharacterSkillUnlock>()
                });
            }

            characterDatabase.defaultPartyIds = BuildDefaultPartyIds(characterDatabase.characters);
            EditorUtility.SetDirty(characterDatabase);
        }

        static List<string> BuildDefaultPartyIds(List<CharacterDefinition> characters)
        {
            var result = new List<string>();
            foreach (var id in new[] { "elio", "aurelian", "sahure", "rashad" })
                if (characters.Exists(x => x != null && x.characterId == id))
                    result.Add(id);

            for (var i = 0; result.Count < 4 && i < characters.Count; i++)
                if (characters[i] != null && !result.Contains(characters[i].characterId))
                    result.Add(characters[i].characterId);
            return result;
        }

        static float DefaultDefense(CharacterRecord record)
        {
            if (record.SkillType.Contains("Tạo Khiên")) return record.Rarity >= 5 ? 14f : 11f;
            return record.Rarity >= 5 ? 8f : record.Rarity == 4 ? 7f : 5f;
        }

        static float DefaultCritRate(CharacterRecord record)
        {
            return record.SkillType.Contains("Bạo kích") ? 0.2f : 0.1f;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            var name = Path.GetFileName(folder);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static List<string> BuildAttributes(CharacterRecord record)
        {
            var result = new List<string>();
            if (record.Playable) result.Add("Playable");
            else result.Add("NonPlayable");
            AddIfValue(result, record.Faction);
            AddIfValue(result, record.Element);
            AddIfValue(result, ElementAlias(record.Element));
            AddIfValue(result, record.Weapon);
            AddIfValue(result, record.SkillType);
            if (record.SkillType.IndexOf("hồi", StringComparison.OrdinalIgnoreCase) >= 0) result.Add("Healer");
            if (record.SkillType.IndexOf("khiên", StringComparison.OrdinalIgnoreCase) >= 0) result.Add("Shielder");
            if (record.SkillType.IndexOf("khống", StringComparison.OrdinalIgnoreCase) >= 0) result.Add("Controller");
            if (record.SkillType.IndexOf("AOE", StringComparison.OrdinalIgnoreCase) >= 0) result.Add("AOE");
            if (record.Weapon.IndexOf("Cung", StringComparison.OrdinalIgnoreCase) >= 0 ||
                record.Weapon.IndexOf("Pháp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                record.Weapon.IndexOf("Thương", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Add("Ranged");
            return result;
        }

        static void AddIfValue(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "X" || value == "Non-Playable") return;
            if (!values.Contains(value)) values.Add(value);
        }

        static string ElementAlias(string element)
        {
            return element switch
            {
                "Hỏa" => "Fire",
                "Thủy" => "Water",
                "Phong" => "Wind",
                "Thảo" => "Grass",
                "Lôi" => "Lightning",
                _ => string.Empty
            };
        }

        static int DefaultHealth(CharacterRecord record)
        {
            if (!record.Playable) return 1;
            return record.SkillType switch
            {
                var x when x.Contains("Tạo Khiên") => 150,
                var x when x.Contains("Hồi máu") => 125,
                _ => record.Rarity >= 5 ? 140 : record.Rarity == 4 ? 120 : 100
            };
        }

        static int DefaultAttack(CharacterRecord record)
        {
            if (!record.Playable) return 1;
            return record.SkillType switch
            {
                var x when x.Contains("Tấn công") => record.Rarity >= 5 ? 34 : 28,
                var x when x.Contains("Bạo kích") => record.Rarity >= 5 ? 32 : 27,
                _ => record.Rarity >= 5 ? 24 : 20
            };
        }

        static string ComputeSourceHash()
        {
            using var md5 = MD5.Create();
            var bytes = File.ReadAllBytes(CharacterInfoAssetPath);
            return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        static List<CharacterRecord> Parse(string text)
        {
            var result = new List<CharacterRecord>();
            CharacterRecord current = null;
            using var reader = new StringReader(text);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.TrimEnd();
                if (line.StartsWith("NHÂN VẬT:", StringComparison.OrdinalIgnoreCase))
                {
                    AddCurrent();
                    current = new CharacterRecord { DisplayName = ValueAfterColon(line) };
                    continue;
                }

                if (current == null) continue;
                var colon = line.IndexOf(':');
                if (colon < 0) continue;
                var key = line.Substring(0, colon).Trim();
                var value = line.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "Id": current.Id = value; break;
                    case "Tên hiển thị": current.DisplayName = value; break;
                    case "Nhóm / Quốc gia": current.Faction = value; break;
                    case "Độ hiếm": int.TryParse(value, out current.Rarity); break;
                    case "Có thể chơi": current.Playable = !value.Equals("Không", StringComparison.OrdinalIgnoreCase); break;
                    case "Hệ": current.Element = value; break;
                    case "Vũ khí": current.Weapon = value; break;
                    case "Loại kỹ năng": current.SkillType = value; break;
                    case "Tấn công thường": current.NormalAttack = value; break;
                    case "Kỹ năng": current.Skill = value; break;
                    case "Nội tại": current.Passive = value; break;
                    case "Mô tả nhân vật": current.Description = value; break;
                }
            }
            AddCurrent();
            return result;

            void AddCurrent()
            {
                if (current == null) return;
                if (string.IsNullOrWhiteSpace(current.Id))
                    current.Id = SanitizeId(current.DisplayName);
                if (!string.IsNullOrWhiteSpace(current.Id))
                    result.Add(current);
            }
        }

        static string ValueAfterColon(string line)
        {
            var colon = line.IndexOf(':');
            return colon < 0 ? string.Empty : line.Substring(colon + 1).Trim();
        }

        static string SanitizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var builder = new StringBuilder();
            foreach (var c in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) builder.Append(c);
                else if (c == ' ' || c == '-' || c == '_') builder.Append('_');
            }
            return builder.ToString().Trim('_');
        }

        class CharacterRecord
        {
            public string Id = string.Empty;
            public string DisplayName = string.Empty;
            public string Faction = string.Empty;
            public int Rarity = 3;
            public bool Playable = true;
            public string Element = string.Empty;
            public string Weapon = string.Empty;
            public string SkillType = string.Empty;
            public string NormalAttack = string.Empty;
            public string Skill = string.Empty;
            public string Passive = string.Empty;
            public string Description = string.Empty;
        }
    }

    public class CharacterInfoAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (!string.Equals(path, CharacterInfoDatabaseImporter.CharacterInfoAssetPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                EditorApplication.delayCall += CharacterInfoDatabaseImporter.ImportIfChanged;
                break;
            }
        }
    }
}
