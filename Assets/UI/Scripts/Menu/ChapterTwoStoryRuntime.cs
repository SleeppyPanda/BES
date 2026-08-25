using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BES.UI.Menu
{
    public static class ChapterTwoStoryRuntime
    {
        const string ChapterId = "chapter_2";
        const string StoryResourcePath = "Main Story/Chương 2";
        static readonly string[] SceneStoryResourcePaths =
        {
            "Main Story/Chương 2 cảnh 1",
            "Main Story/Chương 2 cảnh 2",
            "Main Story/Chương 2 cảnh 3",
            "Main Story/Chương 2 cảnh 4",
            "Main Story/Chương 2 cảnh 5",
            "Main Story/Chương 2 cảnh 6"
        };
        const string GenericCastConfigResourcePath = "Data/StoryCastConfig";

        static readonly Regex SpeakerLine = new(@"^\s*(?<speaker>[^:\[]+)\s*:\s*(?<speech>.*)$", RegexOptions.Compiled);
        static readonly HashSet<string> KnownSpeakers = new(StringComparer.OrdinalIgnoreCase)
        {
            "???", "Lunen", "Tinh Linh", "Tinh linh", "Lila", "Farzan", "Zareen", "Temir",
            "Seyran", "Mireya", "Nayel", "Kharzek", "Ilyen", "Kaivan", "Mireya",
            "Eiren", "Neris", "Zareen", "Nabir"
        };

        static readonly Dictionary<MenuContentDatabase, MenuContentDatabase> RuntimeCopies = new();

        public static MenuContentDatabase Apply(MenuContentDatabase database) => Apply(database, false);

        public static MenuContentDatabase Apply(MenuContentDatabase database, bool writeToSourceAsset)
        {
            if (database == null || database.storyChapters == null) return database;

            if (!writeToSourceAsset)
                database = GetRuntimeDatabase(database);

            var chapter = database.storyChapters.Find(x => x != null && x.id == ChapterId);
            if (chapter == null) return database;

            var sceneSources = LoadSceneSources();
            if (sceneSources.Count == 0)
            {
                var fallbackSource = LoadStorySource();
                if (!string.IsNullOrWhiteSpace(fallbackSource))
                    sceneSources.Add(fallbackSource);
            }
            if (sceneSources.Count == 0) return database;

            var leftFallback = ResolveLeftSprite(chapter);
            var rightFallback = ResolveRightSprite(chapter);

            // Fallback to Chapter 1's assets if Chapter 2 does not have them yet (so Lunen/Tinh Linh are shown correctly)
            if (leftFallback == null || rightFallback == null)
            {
                var ch1 = database.storyChapters.Find(x => x != null && x.id == "chapter_1");
                if (ch1 != null)
                {
                    if (leftFallback == null) leftFallback = ResolveLeftSprite(ch1);
                    if (rightFallback == null) rightFallback = ResolveRightSprite(ch1);
                }
            }

            var profiles = BuildCharacterProfiles(database, null, leftFallback, rightFallback);
            var parsedScenes = new List<ParsedStory>();
            foreach (var sceneSource in sceneSources)
            {
                var parsedScene = ParseStory(sceneSource, chapter.background, profiles, leftFallback, rightFallback);
                if (parsedScene.AllBeats.Count == 0) continue;
                parsedScenes.Add(parsedScene);
            }
            if (parsedScenes.Count == 0) return database;

            chapter.title = "Chương 2 — Khúc Ca Của Bão Tố";
            chapter.summary = "Lunen rời Akherat, gặp đoàn thương nhân Rihara và bước vào biến cố bão tố tại Talvera.";

            chapter.introDialogue = new DialogueSequence
            {
                id = "chapter_2_intro",
                title = "Mở đầu - Lạc lối sa mạc",
                summary = "Lunen cùng Tinh Linh thảo luận về Naevira và bị lạc trên sa mạc phía Tây.",
                beats = new List<DialogueBeat>()
            };

            chapter.stages ??= new List<StageEntry>();
            while (chapter.stages.Count < parsedScenes.Count) chapter.stages.Add(new StageEntry());

            for (var i = 0; i < parsedScenes.Count; i++)
                ApplySceneToStage(chapter.stages[i], parsedScenes[i], i, database);

            if (chapter.stages.Count > parsedScenes.Count)
                chapter.stages.RemoveRange(parsedScenes.Count, chapter.stages.Count - parsedScenes.Count);

            return database;
        }

        static List<string> LoadSceneSources()
        {
            var result = new List<string>();
            foreach (var resourcePath in SceneStoryResourcePaths)
            {
                var text = LoadSceneSource(resourcePath);
                if (!string.IsNullOrWhiteSpace(text))
                    result.Add(text);
            }
            return result;
        }

        static string LoadSceneSource(string resourcePath)
        {
            var projectFile = Path.Combine(Application.dataPath, "Resources", resourcePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(projectFile))
                return File.ReadAllText(projectFile, Encoding.UTF8);

            var textAsset = Resources.Load<TextAsset>(resourcePath);
            return textAsset != null ? textAsset.text : string.Empty;
        }

        static void ApplySceneToStage(StageEntry stage, ParsedStory parsed, int sceneIndex, MenuContentDatabase database)
        {
            var sceneNumber = sceneIndex + 1;
            stage.id = $"chapter_2_stage_{sceneNumber}";
            stage.title = SceneTitle(sceneIndex);
            stage.description = SceneDescription(sceneIndex);
            stage.preBattleDialogue = new DialogueSequence
            {
                id = $"chapter_2_stage_{sceneNumber}_story",
                title = stage.title,
                summary = stage.description,
                beats = parsed.IntroBeats
            };
            stage.victoryDialogue = new DialogueSequence
            {
                id = $"chapter_2_stage_{sceneNumber}_after",
                title = $"Sau phần {sceneNumber}",
                summary = stage.description,
                beats = parsed.VictoryBeats
            };

            ApplyCombatBlocks(stage, parsed, database);
        }

        static string SceneTitle(int sceneIndex)
        {
            switch (sceneIndex)
            {
                case 0: return "Phần I — Người Lữ Hành Giữa Sa Mạc";
                case 1: return "Phần II — Nơi Ngọn Gió Gọi Là Nhà";
                case 2: return "Phần III — Bóng Tối Phía Sau Cơn Bão";
                case 3: return "Phần IV — Ba Chìa Khóa Của Lời Nguyện Ước";
                case 4: return "Phần V — Ngọn Gió Cuối Cùng";
                case 5: return "Phần VI — Lời Chúc Của Ngọn Gió";
                default: return $"Phần {sceneIndex + 1}";
            }
        }

        static string SceneDescription(int sceneIndex)
        {
            switch (sceneIndex)
            {
                case 0: return "Hành trình lạc lối sa mạc Tây, gặp gỡ Lila và đoàn thương nhân Rihara.";
                case 1: return "Đặt chân tới Talvera, làm quen với Seyran và Mireya.";
                case 2: return "Bất ổn dâng cao quanh Talvera, âm mưu của Kharzek và Tộc Bão Gió.";
                case 3: return "Nayel tiến sát vùng đất thiêng để thực hiện nghi thức thức tỉnh linh thú.";
                case 4: return "Chiến đấu chống lại linh thú Vorash bão tố cổ đại tại tâm bão.";
                case 5: return "Sau cơn bão, khôi phục Talvera và chuẩn bị hành trình mới.";
                default: return string.Empty;
            }
        }

        static void ApplyCombatBlocks(StageEntry stage, ParsedStory parsed, MenuContentDatabase database)
        {
            stage.battlePhases ??= new List<BattlePhaseEntry>();
            stage.battlePhases.Clear();

            if (parsed.CombatBlocks.Count == 0)
            {
                stage.enemies = new List<BattleUnitDefinition>();
                stage.boss = null;
                return;
            }

            for (var i = 0; i < parsed.CombatBlocks.Count; i++)
            {
                var block = parsed.CombatBlocks[i];
                var hasSummonBoss = ContainsAny(block.note, "vorash", "linh thu");
                var phase = new BattlePhaseEntry
                {
                    id = $"{stage.id}_phase_{i + 1}",
                    title = i == 0 ? "Chiến binh bão tố" : $"Combat {i + 1}",
                    description = block.note,
                    enemyLevel = Mathf.Max(1, stage.enemyLevel),
                    allies = BuildFixedAllies(block.note, database),
                    enemies = hasSummonBoss
                        ? BuildEnemies(block.note, 1)
                        : BuildEnemies(block.note, 4),
                    boss = hasSummonBoss ? BuildBoss("Vorash") : null,
                    combatDialogueTriggers = new List<CombatDialogueTrigger>()
                };

                if (block.startBeats.Count > 0)
                {
                    phase.combatDialogueTriggers.Add(new CombatDialogueTrigger
                    {
                        id = $"{phase.id}_start",
                        triggerType = CombatDialogueTriggerType.PhaseStart,
                        pauseCombat = true,
                        dialogue = new DialogueSequence
                        {
                            id = $"{phase.id}_start_dialogue",
                            title = "Combat - Mở đầu",
                            summary = block.note,
                            beats = block.startBeats
                        }
                    });
                }

                for (var j = 0; j < block.triggers.Count; j++)
                {
                    var triggerBlock = block.triggers[j];
                    if (triggerBlock.beats.Count == 0) continue;
                    phase.combatDialogueTriggers.Add(BuildCombatTrigger(phase.id, j, triggerBlock, i, parsed.CombatBlocks.Count));
                }

                stage.battlePhases.Add(phase);
            }

            var firstPhase = stage.battlePhases[0];
            stage.enemies = firstPhase.enemies;
            stage.boss = firstPhase.boss;
        }

        static List<BattleUnitDefinition> BuildFixedAllies(string note, MenuContentDatabase database)
        {
            var allies = new List<BattleUnitDefinition>();
            if (ContainsAny(note, "lila"))
                allies.Add(CharacterToBattleDefinition(database, "Lila", "lila_story_guest", "Lila"));
            if (ContainsAny(note, "kaivan"))
                allies.Add(CharacterToBattleDefinition(database, "Kaivan", "kaivan_story_guest", "Kaivan"));
            if (ContainsAny(note, "ilyen"))
                allies.Add(CharacterToBattleDefinition(database, "Ilyen", "ilyen_story_guest", "Ilyen"));
            if (ContainsAny(note, "lunen"))
                allies.Add(CharacterToBattleDefinition(database, "Lunen", "lunen_story_guest", "Lunen"));
            return allies;
        }

        static BattleUnitDefinition CharacterToBattleDefinition(MenuContentDatabase database, string characterName, string fallbackId, string fallbackName)
        {
            var character = database?.characters?.Find(x =>
                string.Equals(x.id, characterName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.displayName, characterName, StringComparison.OrdinalIgnoreCase));
            return new BattleUnitDefinition
            {
                id = !string.IsNullOrWhiteSpace(character?.id) ? character.id : fallbackId,
                displayName = !string.IsNullOrWhiteSpace(character?.displayName) ? character.displayName : fallbackName,
                element = !string.IsNullOrWhiteSpace(character?.element) ? character.element : "Phong",
                portrait = character?.portrait,
                battlefieldSprite = character?.chibi != null ? character.chibi : character?.portrait,
                maxHealth = Mathf.Max(1, character?.maxHealth ?? 620),
                attack = Mathf.Max(1, character?.attack ?? 90),
                defense = Mathf.Max(0, character?.defense ?? 42),
                speed = Mathf.Max(1, character?.speed ?? 11),
                isRanged = character?.attributes != null && (character.attributes.Contains("Ranged") || character.attributes.Contains("tầm xa")),
                skills = new List<BattleSkillDefinition> { new BattleSkillDefinition { id = "attack", displayName = "Tấn Công", powerMultiplier = 1f } }
            };
        }

        static List<BattleUnitDefinition> BuildEnemies(string note, int count)
        {
            var result = new List<BattleUnitDefinition>();
            for (var i = 0; i < count; i++)
            {
                result.Add(new BattleUnitDefinition
                {
                    id = $"chapter_2_ma_vat_{i + 1}",
                    displayName = "Phiến Quân Bão Gió",
                    element = "Phong",
                    maxHealth = 680,
                    attack = 104,
                    defense = 46,
                    speed = 12,
                    isRanged = false,
                    skills = new List<BattleSkillDefinition> { new BattleSkillDefinition { id = "attack", displayName = "Tấn Công", powerMultiplier = 1f } }
                });
            }
            return result;
        }

        static BattleUnitDefinition BuildBoss(string bossName)
        {
            return new BattleUnitDefinition
            {
                id = "vorash_boss",
                displayName = "Linh Thú Vorash",
                element = "Phong",
                maxHealth = 2600,
                attack = 138,
                defense = 96,
                speed = 13,
                isRanged = true,
                skills = new List<BattleSkillDefinition> { new BattleSkillDefinition { id = "storm", displayName = "Cơn Bão Cuồng Nộ", powerMultiplier = 1.5f } }
            };
        }

        static bool ContainsAny(string text, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var haystack = NormalizeSearchText(text);
            foreach (var keyword in keywords)
            {
                var candidate = NormalizeSearchText(keyword);
                if (!string.IsNullOrWhiteSpace(candidate) && haystack.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        static string NormalizeSearchText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            value = value.Replace('đ', 'd').Replace('Đ', 'D');
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    builder.Append(ch);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        static CombatDialogueTrigger BuildCombatTrigger(string phaseId, int index, TriggerBlock block, int phaseIndex, int phaseCount)
        {
            var condition = block.condition ?? string.Empty;
            var action = block.endAction ?? string.Empty;
            return new CombatDialogueTrigger
            {
                id = $"{phaseId}_trigger_{index + 1}",
                triggerType = InferTriggerType(condition),
                round = InferRound(condition),
                healthPercent = InferPercent(condition),
                enemyCount = InferEnemyCount(condition, 0),
                pauseCombat = true,
                actionAfterDialogue = InferTriggerAction(action, phaseIndex, phaseCount),
                dialogue = new DialogueSequence
                {
                    id = $"{phaseId}_trigger_{index + 1}_dialogue",
                    title = "Combat - Đối thoại",
                    summary = condition,
                    beats = block.beats
                }
            };
        }

        static CombatDialogueTriggerType InferTriggerType(string condition)
        {
            if (ContainsAny(condition, "combat ket thuc", "ket thuc combat", "win", "thang", "het enemy", "het quai"))
                return CombatDialogueTriggerType.PhaseVictory;
            if (ContainsAny(condition, "thua", "lose", "defeat", "that bai", "toan doi guc", "tat ca dong minh chet"))
                return CombatDialogueTriggerType.AllAlliesDefeated;
            if (ContainsAny(condition, "ngay khi ket thuc hoat anh", "ket thuc hoat anh"))
                return CombatDialogueTriggerType.PhaseVictory;
            if (ContainsAny(condition, "don dau tien", "danh xong don dau", "toan bo danh xong"))
                return CombatDialogueTriggerType.RoundStart;
            if (ContainsAny(condition, "quai con chet", "quai con duoc trieu hoi bi tieu diet", "quai duoc trieu hoi da tieu diet"))
                return CombatDialogueTriggerType.EnemyCountAtOrBelow;
            if (ContainsAny(condition, "con") && ContainsAny(condition, "enemy", "quai", "ma vat"))
                return CombatDialogueTriggerType.EnemyCountAtOrBelow;
            if (ContainsAny(condition, "%", "phan tram", "nua mau", "nua hp", "half hp"))
                return ContainsAny(condition, "tong", "toan bo", "enemy", "quai", "ma vat")
                    ? CombatDialogueTriggerType.TotalEnemyHealthBelowPercent
                    : CombatDialogueTriggerType.BossHealthBelowPercent;
            if (ContainsAny(condition, "round", "luot"))
                return CombatDialogueTriggerType.RoundStart;
            return CombatDialogueTriggerType.PhaseStart;
        }

        static CombatTriggerActionType InferTriggerAction(string action, int phaseIndex, int phaseCount)
        {
            if (ContainsAny(action, "giet tat ca enemy", "giet toan bo enemy", "giet tat ca quai", "giet toan bo quai", "khong hien win/lose", "thuc hien trigger tiep theo"))
                return CombatTriggerActionType.KillAllEnemiesAndPlayPhaseVictory;
            if (ContainsAny(action, "khong can hien thi win/lose", "khong hien win/lose", "quay tro ve story panel"))
                return CombatTriggerActionType.ReturnToStoryWithoutResult;
            var convert = ContainsAny(action, "ve doi", "vao doi", "dong hanh", "chuyen phe");
            var nextPhase = ContainsAny(action, "combat tiep", "combat lan", "phase", "tran tiep", "tiep tuc combat moi");
            if (!nextPhase && phaseIndex + 1 < phaseCount && convert) nextPhase = true;
            if (convert && nextPhase) return CombatTriggerActionType.ConvertUnitToAllyAndStartNextPhase;
            if (convert) return CombatTriggerActionType.ConvertUnitToAlly;
            if (nextPhase) return CombatTriggerActionType.StartNextPhase;
            return CombatTriggerActionType.None;
        }

        static int InferRound(string text)
        {
            if (ContainsAny(text, "don dau tien", "danh xong don dau", "toan bo danh xong"))
                return 2;
            var match = Regex.Match(NormalizeSearchText(text), @"(?:round|luot)\s*(\d+)", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Mathf.Max(1, value) : 1;
        }

        static int InferEnemyCount(string text, int fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            var match = Regex.Match(NormalizeSearchText(text), @"(?:con|co)\s*(\d+)\s*(?:con\s*)?(?:enemy|quai|ma vat)", RegexOptions.IgnoreCase);
            if (!match.Success) match = Regex.Match(text, @"\d+\s*v\s*(\d+)", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Mathf.Max(0, value) : fallback;
        }

        static int InferPercent(string text)
        {
            if (ContainsAny(text, "nua mau", "nua hp", "half hp")) return 50;
            var match = Regex.Match(text ?? string.Empty, @"(\d+)\s*%");
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Mathf.Clamp(value, 1, 100) : 50;
        }

        static string LoadStorySource()
        {
            var combined = LoadCombinedSceneSources();
            if (!string.IsNullOrWhiteSpace(combined)) return combined;

            var textAsset = Resources.Load<TextAsset>(StoryResourcePath);
            if (textAsset != null && !string.IsNullOrWhiteSpace(textAsset.text))
                return textAsset.text;

            return string.Empty;
        }

        static string LoadCombinedSceneSources()
        {
            var builder = new StringBuilder();
            foreach (var resourcePath in SceneStoryResourcePaths)
            {
                var projectFile = Path.Combine(Application.dataPath, "Resources", resourcePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(projectFile))
                {
                    var text = File.ReadAllText(projectFile, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        AppendSource(text);
                        continue;
                    }
                }

                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset != null && !string.IsNullOrWhiteSpace(textAsset.text))
                    AppendSource(textAsset.text);
            }
            return builder.ToString();

            void AppendSource(string value)
            {
                if (builder.Length > 0) builder.AppendLine().AppendLine();
                builder.Append(value);
            }
        }

        class RuntimeCharacterProfile
        {
            public string id;
            public string displayName;
            public Sprite sprite;
            public bool leftSide;
            public int defaultSlotIndex = -1;
            public bool defaultDimWhenNotSpeaking = true;
        }

        static Dictionary<string, RuntimeCharacterProfile> BuildCharacterProfiles(
            MenuContentDatabase database, 
            ChapterCastConfig castConfig, 
            Sprite leftFallback, 
            Sprite rightFallback)
        {
            var profiles = new Dictionary<string, RuntimeCharacterProfile>(StringComparer.OrdinalIgnoreCase);

            // Add standard default profiles
            Add("Lunen", "Lunen", "lunen", leftFallback, true);
            Add("Tinh Linh", "Tinh Linh", "tinh_linh", rightFallback, false);
            
            // Dynamic characters from Chapter 2
            AddWithDatabaseFallback(database, "Lila", "lila", rightFallback, false);
            AddWithDatabaseFallback(database, "Farzan", "farzan", rightFallback, false);
            AddWithDatabaseFallback(database, "Zareen", "zareen", rightFallback, false);
            AddWithDatabaseFallback(database, "Temir", "temir", rightFallback, false);
            AddWithDatabaseFallback(database, "Seyran", "seyran", rightFallback, false);
            AddWithDatabaseFallback(database, "Mireya", "mireya", rightFallback, false);
            AddWithDatabaseFallback(database, "Nayel", "nayel", rightFallback, false);
            AddWithDatabaseFallback(database, "Kharzek", "kharzek", rightFallback, false);
            AddWithDatabaseFallback(database, "Ilyen", "ilyen", rightFallback, false);
            AddWithDatabaseFallback(database, "Kaivan", "kaivan", rightFallback, false);
            AddWithDatabaseFallback(database, "Nabir", "nabir", rightFallback, false);
            AddWithDatabaseFallback(database, "???", "unknown", rightFallback, false);

            if (castConfig?.characterProfiles != null)
            {
                foreach (var configProfile in castConfig.characterProfiles)
                {
                    if (configProfile == null || string.IsNullOrWhiteSpace(configProfile.speaker)) continue;
                    var speaker = NormalizeSpeaker(configProfile.speaker);
                    profiles.TryGetValue(speaker, out var existing);
                    profiles[speaker] = new RuntimeCharacterProfile
                    {
                        id = !string.IsNullOrWhiteSpace(configProfile.characterId) ? configProfile.characterId.Trim() : existing?.id ?? NormalizeCharacterId(speaker),
                        displayName = !string.IsNullOrWhiteSpace(configProfile.displayName) ? configProfile.displayName.Trim() : existing?.displayName ?? speaker,
                        sprite = configProfile.sprite != null ? configProfile.sprite : existing?.sprite ?? rightFallback,
                        leftSide = existing?.leftSide ?? IsLeftSpeaker(speaker),
                        defaultSlotIndex = configProfile.defaultSlotIndex,
                        defaultDimWhenNotSpeaking = configProfile.defaultDimWhenNotSpeaking
                    };
                }
            }

            return profiles;

            void Add(string speaker, string displayName, string id, Sprite sprite, bool leftSide)
            {
                profiles[NormalizeSpeaker(speaker)] = new RuntimeCharacterProfile
                {
                    id = id,
                    displayName = displayName,
                    sprite = sprite,
                    leftSide = leftSide,
                    defaultSlotIndex = -1,
                    defaultDimWhenNotSpeaking = true
                };
            }

            void AddWithDatabaseFallback(MenuContentDatabase db, string speaker, string id, Sprite fallback, bool leftSide)
            {
                var dbChar = db?.FindCharacter(id) ?? db?.characters.Find(x => string.Equals(x.displayName, speaker, StringComparison.OrdinalIgnoreCase));
                Sprite charSprite = fallback;
                if (dbChar != null)
                {
                    if (dbChar.fullBody != null) charSprite = dbChar.fullBody;
                    else if (dbChar.portrait != null) charSprite = dbChar.portrait;
                    else if (dbChar.chibi != null) charSprite = dbChar.chibi;
                }
                else
                {
                    // Create temporary fallback CharacterEntry to prevent crash in UI panels
                    var tempChar = new CharacterEntry
                    {
                        id = id,
                        displayName = speaker,
                        description = "Nhân vật cốt truyện Chương 2",
                        portrait = fallback,
                        rarity = 4,
                        level = 1
                    };
                    db?.characters.Add(tempChar);
                }

                Add(speaker, dbChar?.displayName ?? speaker, dbChar?.id ?? id, charSprite, leftSide);
            }
        }

        static void ApplyCastOverrides(List<DialogueBeat> beats, Dictionary<string, RuntimeCharacterProfile> profiles, ChapterCastConfig castConfig)
        {
            ApplyDefaultProfiles(beats, profiles);

            if (castConfig?.beatOverrides == null) return;
            var textMatched = new HashSet<StoryBeatCastOverride>();
            for (var i = 0; i < beats.Count; i++)
            {
                var beat = beats[i];
                if (beat == null) continue;
                foreach (var beatOverride in castConfig.beatOverrides)
                {
                    if (beatOverride == null) continue;
                    var indexMatch = beatOverride.globalBeatIndex == i;
                    var textMatch = !string.IsNullOrWhiteSpace(beatOverride.textContains) &&
                                    !string.IsNullOrEmpty(beat.text) &&
                                    beat.text.IndexOf(beatOverride.textContains, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!indexMatch && !textMatch) continue;
                    if (textMatch && !beatOverride.applyToAllTextMatches && textMatched.Contains(beatOverride)) continue;

                    ApplyBeatOverride(beat, profiles, beatOverride);
                    if (textMatch) textMatched.Add(beatOverride);
                }
            }
        }

        static void ApplyDefaultProfiles(List<DialogueBeat> beats, Dictionary<string, RuntimeCharacterProfile> profiles)
        {
            if (profiles == null) return;
            foreach (var beat in beats)
            {
                if (beat == null || string.IsNullOrWhiteSpace(beat.speaker)) continue;
                if (!profiles.TryGetValue(NormalizeSpeaker(beat.speaker), out var profile) || profile == null) continue;
                if (profile.defaultSlotIndex < 0) continue;

                beat.layoutMode = DialogueLayoutMode.CustomSlots;
                beat.characterPlacements ??= new List<DialogueCharacterPlacement>();
                beat.characterVisuals ??= new List<DialogueCharacterVisualOverride>();

                if (!HasPlacement(beat, profile.id, profile.defaultSlotIndex))
                {
                    beat.characterPlacements.Add(new DialogueCharacterPlacement
                    {
                        characterId = profile.id,
                        slotIndex = profile.defaultSlotIndex,
                        sprite = profile.sprite,
                        show = true,
                        instant = beat.instantLayout
                    });
                }

                if (!HasVisual(beat, profile.id, profile.defaultSlotIndex))
                {
                    beat.characterVisuals.Add(new DialogueCharacterVisualOverride
                    {
                        characterId = profile.id,
                        slotIndex = profile.defaultSlotIndex,
                        show = true,
                        dim = false
                    });
                }
            }
        }

        static void ApplyBeatOverride(DialogueBeat beat, Dictionary<string, RuntimeCharacterProfile> profiles, StoryBeatCastOverride beatOverride)
        {
            beat.layoutMode = beatOverride.layoutMode;
            beat.instantLayout = beatOverride.instantLayout;
            beat.characterPlacements = new List<DialogueCharacterPlacement>();
            beat.characterVisuals = new List<DialogueCharacterVisualOverride>();
            beat.characterMovements = beatOverride.movements != null
                ? new List<DialogueCharacterMovement>(beatOverride.movements)
                : new List<DialogueCharacterMovement>();

            if (beatOverride.characters == null) return;
            var listedSlots = new HashSet<int>();
            foreach (var state in beatOverride.characters)
            {
                if (state == null) continue;
                var speaker = NormalizeSpeaker(state.speaker);
                RuntimeCharacterProfile profile = null;
                if (profiles != null) profiles.TryGetValue(speaker, out profile);
                var characterId = !string.IsNullOrWhiteSpace(state.characterId)
                    ? state.characterId.Trim()
                    : profile?.id ?? NormalizeCharacterId(speaker);
                var sprite = state.sprite != null ? state.sprite : profile?.sprite;
                if (state.slotIndex >= 0) listedSlots.Add(state.slotIndex);

                beat.characterPlacements.Add(new DialogueCharacterPlacement
                {
                    characterId = characterId,
                    slotIndex = state.slotIndex,
                    sprite = sprite,
                    pose = state.applyPose ? state.pose : null,
                    show = state.show,
                    instant = state.instant
                });

                beat.characterVisuals.Add(new DialogueCharacterVisualOverride
                {
                    characterId = characterId,
                    slotIndex = state.slotIndex,
                    show = state.show,
                    dim = state.dim,
                    dimAlpha = state.dimAlpha,
                    litAlpha = state.litAlpha,
                    dimScale = state.dimScale,
                    litScale = state.litScale
                });

                if (!string.IsNullOrWhiteSpace(beat.speaker) &&
                    string.Equals(beat.speaker.Trim(), profile?.displayName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(state.characterId))
                    beat.speaker = profile.displayName;
            }

            if (!beatOverride.hideUnlistedSlots) return;
            var count = Mathf.Max(1, beatOverride.controlledSlotCount);
            for (var slotIndex = 0; slotIndex < count; slotIndex++)
            {
                if (listedSlots.Contains(slotIndex)) continue;
                beat.characterVisuals.Add(new DialogueCharacterVisualOverride
                {
                    slotIndex = slotIndex,
                    show = false
                });
            }
        }

        static bool HasPlacement(DialogueBeat beat, string characterId, int slotIndex)
        {
            if (beat?.characterPlacements == null) return false;
            foreach (var placement in beat.characterPlacements)
                if (placement != null && placement.slotIndex == slotIndex &&
                    string.Equals(placement.characterId?.Trim(), characterId?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        static bool HasVisual(DialogueBeat beat, string characterId, int slotIndex)
        {
            if (beat?.characterVisuals == null) return false;
            foreach (var visual in beat.characterVisuals)
                if (visual != null && visual.slotIndex == slotIndex &&
                    string.Equals(visual.characterId?.Trim(), characterId?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        class ParsedStory
        {
            public readonly List<DialogueBeat> IntroBeats = new();
            public readonly List<DialogueBeat> PreBattleBeats = new();
            public readonly List<DialogueBeat> VictoryBeats = new();
            public readonly List<CombatBlock> CombatBlocks = new();
            public readonly List<DialogueBeat> AllBeats = new();
        }

        class CombatBlock
        {
            public string note;
            public readonly List<DialogueBeat> startBeats = new();
            public readonly List<TriggerBlock> triggers = new();
        }

        class TriggerBlock
        {
            public string condition;
            public string endAction;
            public readonly List<DialogueBeat> beats = new();
        }

        static readonly Regex TagLine = new(@"^\s*\[\s*(?<tag>combat|trigger|trigger end|ck)\s*\]\s*(?<note>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static bool IsQuotedLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            return line.StartsWith("“", StringComparison.Ordinal) ||
                   line.StartsWith("”", StringComparison.Ordinal) ||
                   line.StartsWith("\"", StringComparison.Ordinal) ||
                   line.StartsWith("'", StringComparison.Ordinal);
        }

        static bool ShouldEndTriggerAtCheckpoint(TriggerBlock trigger)
        {
            if (trigger == null || string.IsNullOrWhiteSpace(trigger.condition)) return false;
            return ContainsAny(trigger.condition, "combat ket thuc", "tran dau ket thuc");
        }

        static ParsedStory ParseStory(
            string source,
            Sprite background,
            Dictionary<string, RuntimeCharacterProfile> profiles,
            Sprite leftFallback,
            Sprite rightFallback)
        {
            var parsed = new ParsedStory();
            var sceneText = new StringBuilder();
            string pendingSpeaker = null;
            var speech = new StringBuilder();
            CombatBlock currentCombat = null;
            TriggerBlock currentTrigger = null;

            List<DialogueBeat> CurrentTarget()
            {
                if (currentTrigger != null) return currentTrigger.beats;
                if (currentCombat != null) return currentCombat.startBeats;
                return parsed.CombatBlocks.Count == 0 ? parsed.IntroBeats : parsed.VictoryBeats;
            }

            foreach (var raw in source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || IsDivider(line))
                {
                    FlushSpeech();
                    FlushSceneText();
                    continue;
                }

                var tagMatch = TagLine.Match(line);
                if (tagMatch.Success)
                {
                    FlushSpeech();
                    FlushSceneText();
                    var tag = tagMatch.Groups["tag"].Value.Trim().ToLowerInvariant();
                    var note = tagMatch.Groups["note"].Value.Trim();

                    if (tag == "combat")
                    {
                        currentTrigger = null;
                        currentCombat = new CombatBlock { note = note };
                        parsed.CombatBlocks.Add(currentCombat);
                    }
                    else if (tag == "trigger")
                    {
                        if (currentCombat == null)
                        {
                            currentCombat = new CombatBlock();
                            parsed.CombatBlocks.Add(currentCombat);
                        }
                        currentTrigger = new TriggerBlock { condition = note };
                        currentCombat.triggers.Add(currentTrigger);
                    }
                    else if (tag == "trigger end")
                    {
                        if (currentTrigger != null) currentTrigger.endAction = note;
                        currentTrigger = null;
                    }
                    else if (tag == "ck")
                    {
                        if (currentTrigger != null && ShouldEndTriggerAtCheckpoint(currentTrigger))
                        {
                            currentTrigger.endAction = string.IsNullOrWhiteSpace(currentTrigger.endAction)
                                ? "không hiện win/lose panel. quay trở về story panel và tiếp tục tuyến truyện"
                                : currentTrigger.endAction;
                            currentTrigger = null;
                            currentCombat = null;
                        }
                    }
                    continue;
                }

                var match = SpeakerLine.Match(line);
                if (match.Success && IsKnownSpeaker(match.Groups["speaker"].Value))
                {
                    FlushSpeech();
                    FlushSceneText();
                    pendingSpeaker = NormalizeSpeaker(match.Groups["speaker"].Value);
                    var speechText = match.Groups["speech"].Value.Trim();
                    if (!string.IsNullOrEmpty(speechText))
                    {
                        AppendLine(speech, CleanLine(speechText));
                    }
                    continue;
                }

                var clean = CleanLine(line);
                var quoted = IsQuotedLine(line);
                if (!string.IsNullOrEmpty(pendingSpeaker) && quoted)
                {
                    AppendLine(speech, clean);
                    continue;
                }

                if (!string.IsNullOrEmpty(pendingSpeaker) && !quoted)
                    FlushSpeech();

                AppendLine(sceneText, clean);
            }

            FlushSpeech();
            FlushSceneText();
            return parsed;

            void FlushSpeech()
            {
                if (string.IsNullOrEmpty(pendingSpeaker) || speech.Length == 0) return;
                var beat = CreateBeat(pendingSpeaker, speech.ToString(), false, background, profiles, leftFallback, rightFallback);
                CurrentTarget().Add(beat);
                parsed.AllBeats.Add(beat);
                pendingSpeaker = null;
                speech.Clear();
            }

            void FlushSceneText()
            {
                if (sceneText.Length == 0) return;
                var beat = CreateBeat(string.Empty, sceneText.ToString(), true, background, profiles, leftFallback, rightFallback);
                CurrentTarget().Add(beat);
                parsed.AllBeats.Add(beat);
                sceneText.Clear();
            }
        }

        static DialogueBeat CreateBeat(
            string speaker,
            string text,
            bool isSceneText,
            Sprite background,
            Dictionary<string, RuntimeCharacterProfile> profiles,
            Sprite leftFallback,
            Sprite rightFallback)
        {
            profiles ??= new Dictionary<string, RuntimeCharacterProfile>(StringComparer.OrdinalIgnoreCase);
            profiles.TryGetValue(NormalizeSpeaker(speaker), out var profile);
            profile ??= new RuntimeCharacterProfile
            {
                id = NormalizeCharacterId(speaker),
                displayName = NormalizeSpeaker(speaker),
                sprite = IsLeftSpeaker(speaker) ? leftFallback : rightFallback,
                leftSide = IsLeftSpeaker(speaker)
            };

            isSceneText = isSceneText || string.IsNullOrWhiteSpace(speaker);
            var leftActive = !isSceneText && profile.leftSide;
            var rightActive = !isSceneText && !leftActive;
            return new DialogueBeat
            {
                speaker = isSceneText ? string.Empty : profile.displayName,
                text = text,
                background = background,
                leftCharacter = isSceneText ? null : leftActive ? profile.sprite : leftFallback,
                rightCharacter = isSceneText ? null : rightActive ? profile.sprite : rightFallback,
                dimLeft = isSceneText || !leftActive,
                dimRight = isSceneText || !rightActive,
                layoutMode = DialogueLayoutMode.Auto,
                instantLayout = false,
                castActions = new List<DialogueCastAction>(),
                characterPlacements = new List<DialogueCharacterPlacement>(),
                characterMovements = new List<DialogueCharacterMovement>(),
                characterVisuals = new List<DialogueCharacterVisualOverride>(),
                onBeatStarted = new UnityEngine.Events.UnityEvent()
            };
        }

        static List<DialogueBeat> CopyRange(List<DialogueBeat> source, int start, int end)
        {
            start = Mathf.Clamp(start, 0, source.Count);
            end = Mathf.Clamp(end, start, source.Count);
            var result = new List<DialogueBeat>();
            for (var i = start; i < end; i++) result.Add(source[i]);
            return result;
        }

        static int FindBeatIndex(List<DialogueBeat> source, string needle, int start = 0, int end = -1)
        {
            if (string.IsNullOrWhiteSpace(needle)) return -1;
            if (end < 0) end = source.Count;
            for (var i = Mathf.Clamp(start, 0, source.Count); i < Mathf.Clamp(end, 0, source.Count); i++)
                if (!string.IsNullOrEmpty(source[i]?.text) && source[i].text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            return -1;
        }

        static Sprite ResolveLeftSprite(StoryChapterEntry chapter)
        {
            if (chapter?.introDialogue?.beats != null)
            {
                foreach (var beat in chapter.introDialogue.beats)
                    if (beat?.leftCharacter != null) return beat.leftCharacter;
            }
            return null;
        }

        static Sprite ResolveRightSprite(StoryChapterEntry chapter)
        {
            if (chapter?.introDialogue?.beats != null)
            {
                foreach (var beat in chapter.introDialogue.beats)
                    if (beat?.rightCharacter != null) return beat.rightCharacter;
            }
            return null;
        }

        static bool IsKnownSpeaker(string speaker) => KnownSpeakers.Contains(NormalizeSpeaker(speaker));
        static bool IsDivider(string line) => line.StartsWith("___", StringComparison.Ordinal) || line == "\u2003";
        static bool IsLeftSpeaker(string speaker) => string.Equals(speaker, "Lunen", StringComparison.OrdinalIgnoreCase);

        static string NormalizeSpeaker(string speaker)
        {
            speaker = (speaker ?? string.Empty).Trim();
            return string.Equals(speaker, "Tinh linh", StringComparison.OrdinalIgnoreCase) ? "Tinh Linh" : speaker;
        }

        static string NormalizeCharacterId(string speaker)
        {
            speaker = NormalizeSpeaker(speaker);
            if (string.IsNullOrWhiteSpace(speaker) || speaker == "???") return string.Empty;
            return speaker.Replace(" ", "_").ToLowerInvariant();
        }

        static string CleanLine(string line)
        {
            return TrimDialogueQuotes(line);
        }

        static string TrimDialogueQuotes(string line)
        {
            line = (line ?? string.Empty).Trim();
            if (line.Length >= 2)
            {
                if ((line.StartsWith("\"", StringComparison.Ordinal) && line.EndsWith("\"", StringComparison.Ordinal)) ||
                    (line.StartsWith("“", StringComparison.Ordinal) && line.EndsWith("”", StringComparison.Ordinal)))
                {
                    line = line.Substring(1, line.Length - 2).Trim();
                }
            }

            return line;
        }

        static void AppendLine(StringBuilder builder, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line);
        }

        static MenuContentDatabase GetRuntimeDatabase(MenuContentDatabase database)
        {
            if (database == null) return null;
            if (database.name.EndsWith(" Runtime", StringComparison.OrdinalIgnoreCase)) return database;
            if (RuntimeCopies.TryGetValue(database, out var cached) && cached != null) return cached;
            var copy = UnityEngine.Object.Instantiate(database);
            copy.name = database.name + " Runtime";
            RuntimeCopies[database] = copy;
            return copy;
        }
    }
}


