using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BES.UI.Menu
{
    public static class ChapterOneStoryRuntime
    {
        const string ChapterId = "chapter_1";
        const string StoryResourcePath = "Main Story/chương 1";
        static readonly string[] SceneStoryResourcePaths =
        {
            "Main Story/Chương 1 cảnh 1",
            "Main Story/Chương 1 cảnh 2",
            "Main Story/Chương 1 cảnh 3",
            "Main Story/Chương 1 cảnh 4",
            "Main Story/Chương 1 cảnh 5",
            "Main Story/Chương 1 cảnh 6",
            "Main Story/Chương 1 cảnh 7"
        };
        const string CastConfigResourcePath = "Data/ChapterOneStoryCastConfig";

        static readonly Regex SpeakerLine = new(@"^\s*(?<speaker>[A-Za-zÀ-ỹ0-9?&\s]+)\s*:\s*(?<speech>.*)$", RegexOptions.Compiled);
        static readonly Regex TagLine = new(@"^\s*\[\s*(?<tag>combat|trigger|trigger end|ck)\s*\]\s*(?<note>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Dictionary<MenuContentDatabase, MenuContentDatabase> RuntimeCopies = new();
        static readonly HashSet<string> KnownSpeakers = new(StringComparer.OrdinalIgnoreCase)
        {
            "???", "Lunen", "Tinh Linh", "Tinh linh", "Elio", "Rashad", "Aurelian",
            "Sahure", "Nefru", "Bekhet", "Menkara", "Nephkar", "Khepraen",
            "Ramesses", "Kasim", "Vezkara"
        };

        public static MenuContentDatabase Apply(MenuContentDatabase database) => Apply(database, false);

        public static MenuContentDatabase Apply(MenuContentDatabase database, bool writeToSourceAsset)
        {
            if (database == null || database.storyChapters == null) return database;
            var castConfig = Resources.Load<ChapterOneStoryCastConfig>(CastConfigResourcePath);
            if (castConfig == null || !castConfig.autoGenerateChapterFromTextFiles) return database;

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
            var profiles = BuildCharacterProfiles(database, castConfig, leftFallback, rightFallback);
            var parsedScenes = new List<ParsedStory>();
            foreach (var sceneSource in sceneSources)
            {
                var parsedScene = ParseStory(sceneSource, chapter.background, profiles, leftFallback, rightFallback);
                if (parsedScene.AllBeats.Count == 0) continue;
                ApplyCastOverrides(parsedScene.AllBeats, profiles, castConfig);
                parsedScenes.Add(parsedScene);
            }
            if (parsedScenes.Count == 0) return database;

            chapter.title = "Chương 1 - Ngọn Lửa Dưới Mặt Trời Sa Mạc";
            chapter.summary = "Lunen lần theo chiếc vòng ngọc tới biên giới Akherat, gặp Elio, bị cuốn vào âm mưu truy sát vương tử, chứng kiến biến cố của Hỏa thần Aurelian và sự ra đời của vị vua con người đầu tiên của Akherat.";

            chapter.introDialogue = new DialogueSequence
            {
                id = "chapter_1_intro",
                title = "Mở đầu - Rừng biên giới Akherat",
                summary = "Lunen và Tinh Linh lần theo chiếc vòng ngọc, bị một con mèo đen cướp vòng và chạm mặt thiếu niên bí ẩn.",
                beats = parsedScenes.Count > 0 ? new List<DialogueBeat>(parsedScenes[0].IntroBeats) : new List<DialogueBeat>()
            };

            chapter.stages ??= new List<StageEntry>();
            while (chapter.stages.Count < parsedScenes.Count) chapter.stages.Add(new StageEntry());

            for (var i = 0; i < parsedScenes.Count; i++)
                ApplySceneToStage(chapter.stages[i], parsedScenes[i], i, database);

            if (chapter.stages.Count > parsedScenes.Count)
                chapter.stages.RemoveRange(parsedScenes.Count, chapter.stages.Count - parsedScenes.Count);
            return database;
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

        static string LoadStorySource()
        {
            var combined = LoadCombinedSceneSources();
            if (!string.IsNullOrWhiteSpace(combined)) return combined;

            var textAsset = Resources.Load<TextAsset>(StoryResourcePath);
            if (textAsset != null && !string.IsNullOrWhiteSpace(textAsset.text))
                return textAsset.text;

            var projectFile = Path.Combine(Application.dataPath, "Resources", "Main Story", "chương 1");
            if (File.Exists(projectFile))
                return File.ReadAllText(projectFile, Encoding.UTF8);

            return string.Empty;
        }

        static List<string> LoadSceneSources()
        {
            var result = new List<string>();
            for (var i = 1; i <= 7; i++)
            {
                var text = LoadSceneSourceByFileName($"Chương 1 cảnh {i}");
                if (!string.IsNullOrWhiteSpace(text))
                    result.Add(text);
            }
            if (result.Count > 0) return result;

            foreach (var resourcePath in SceneStoryResourcePaths)
            {
                var text = LoadSceneSource(resourcePath);
                if (!string.IsNullOrWhiteSpace(text))
                    result.Add(text);
            }
            return result;
        }

        static string LoadSceneSourceByFileName(string fileName)
        {
            var projectFile = Path.Combine(Application.dataPath, "Resources", "Main Story", fileName);
            if (File.Exists(projectFile))
                return File.ReadAllText(projectFile, Encoding.UTF8);

            var textAsset = Resources.Load<TextAsset>("Main Story/" + fileName);
            return textAsset != null ? textAsset.text : string.Empty;
        }

        static string LoadSceneSource(string resourcePath)
        {
            var projectFile = Path.Combine(Application.dataPath, "Resources", resourcePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(projectFile))
                return File.ReadAllText(projectFile, Encoding.UTF8);

            var textAsset = Resources.Load<TextAsset>(resourcePath);
            return textAsset != null ? textAsset.text : string.Empty;
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

        static StageEntry EnsureFirstStage(StoryChapterEntry chapter)
        {
            chapter.stages ??= new List<StageEntry>();
            while (chapter.stages.Count == 0) chapter.stages.Add(new StageEntry());
            return chapter.stages[0];
        }

        static void ApplySceneToStage(StageEntry stage, ParsedStory parsed, int sceneIndex, MenuContentDatabase database)
        {
            var sceneNumber = sceneIndex + 1;
            stage.id = $"chapter_1_stage_{sceneNumber}";
            stage.title = SceneTitle(sceneIndex);
            stage.description = SceneDescription(sceneIndex);
            stage.preBattleDialogue = new DialogueSequence
            {
                id = $"chapter_1_stage_{sceneNumber}_story",
                title = stage.title,
                summary = stage.description,
                beats = parsed.IntroBeats
            };
            stage.victoryDialogue = new DialogueSequence
            {
                id = $"chapter_1_stage_{sceneNumber}_after",
                title = $"Sau cảnh {sceneNumber}",
                summary = stage.description,
                beats = parsed.VictoryBeats
            };

            ApplyCombatBlocks(stage, parsed, database);
        }

        static string SceneTitle(int sceneIndex)
        {
            return sceneIndex switch
            {
                0 => "Rừng biên giới Akherat",
                1 => "Con đường vào Hoàng cung",
                2 => "Bữa tiệc hoàng gia",
                3 => "Câu chuyện của ngọn lửa ngoại lai",
                4 => "Đại lễ tế trời",
                5 => "Khi ngọn lửa không còn sự bảo hộ",
                6 => "Mong ngọn lửa vĩnh hằng vĩnh viễn bảo hộ chúng ta",
                _ => $"Cảnh {sceneIndex + 1}"
            };
        }

        static string SceneDescription(int sceneIndex)
        {
            return sceneIndex switch
            {
                0 => "Lunen và Elio từ hiểu lầm ban đầu buộc phải cùng chiến đấu khi ma vật xuất hiện.",
                1 => "Rashad đưa Lunen và Tinh Linh vào hoàng cung Akherat, giới thiệu lịch sử Hỏa thần và hé lộ sự căng thẳng trong cung.",
                2 => "Lunen và Tinh Linh dự yến tiệc hoàng gia, diện kiến Aurelian và chứng kiến mâu thuẫn âm ỉ trong Akherat.",
                3 => "Lunen và Tinh Linh tới Đại Thư Khố, gặp Menkara và tiếp tục lần theo manh mối về chiếc vòng.",
                4 => "Đại lễ tế trời bị ma vật tập kích, Aurelian ra tay cứu Elio rồi nghi lễ chuyển thành biến cố nguy hiểm.",
                5 => "Aurelian bất tỉnh, Elio gánh lấy Akherat, rồi biến cố Nephkar đẩy ngọn lửa kế vị tới thử thách sinh tử.",
                6 => "Lunen rời Akherat trong lời tiễn biệt của Elio và những người bạn mới, khép lại chương đầu của ngọn lửa sa mạc.",
                _ => "Một phân đoạn tiếp theo của Chương 1."
            };
        }

        static void ApplyCombatDialogue(StageEntry stage, List<DialogueBeat> allBeats, int battleStart, int forestCalm)
        {
            stage.combatDialogueTriggers ??= new List<CombatDialogueTrigger>();
            stage.combatDialogueTriggers.Clear();

            stage.combatDialogueTriggers.Add(new CombatDialogueTrigger
            {
                id = "chapter_1_combat_start",
                triggerType = CombatDialogueTriggerType.BattleStart,
                pauseCombat = true,
                dialogue = new DialogueSequence
                {
                    id = "chapter_1_combat_start_dialogue",
                    title = "Combat - Ma vật xuất hiện",
                    summary = "Pop-up mở combat theo tuyến truyện.",
                    beats = CopyRange(allBeats, battleStart, Mathf.Min(battleStart + 4, allBeats.Count))
                }
            });

            stage.combatDialogueTriggers.Add(new CombatDialogueTrigger
            {
                id = "chapter_1_boss_half_hp",
                triggerType = CombatDialogueTriggerType.BossHealthBelowPercent,
                healthPercent = 50,
                unitId = "akherat_pursuer_boss",
                pauseCombat = true,
                dialogue = new DialogueSequence
                {
                    id = "chapter_1_boss_half_hp_dialogue",
                    title = "Combat - Truy sát vương tử",
                    summary = "Ma vật để lộ mục tiêu thật sự.",
                    beats = FindWindow(allBeats, battleStart, forestCalm, "truy sát... vương tử", 5)
                }
            });

            stage.combatDialogueTriggers.Add(new CombatDialogueTrigger
            {
                id = "chapter_1_before_victory",
                triggerType = CombatDialogueTriggerType.BeforeVictory,
                pauseCombat = true,
                dialogue = new DialogueSequence
                {
                    id = "chapter_1_before_victory_dialogue",
                    title = "Combat - Đòn kết thúc",
                    summary = "Lunen và Elio phối hợp hạ ma vật cuối cùng.",
                    beats = FindWindow(allBeats, battleStart, forestCalm, "Con ma vật cuối cùng lao tới", 5)
                }
            });
        }

        static void ApplyEnemyNames(StageEntry stage)
        {
            stage.enemyLevel = Mathf.Max(stage.enemyLevel, 2);
            stage.enemies ??= new List<BattleUnitDefinition>();
            for (var i = 0; i < stage.enemies.Count; i++)
            {
                var enemy = stage.enemies[i];
                if (enemy == null) continue;
                enemy.id = $"akherat_pursuer_{i + 1}";
                enemy.displayName = "Ma Vật Truy Sát";
            }

            if (stage.boss != null)
            {
                stage.boss.id = "akherat_pursuer_boss";
                stage.boss.displayName = "Ma Vật Hắc Hỏa";
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

        static Dictionary<string, RuntimeCharacterProfile> BuildCharacterProfiles(MenuContentDatabase database, ChapterOneStoryCastConfig castConfig, Sprite leftFallback, Sprite rightFallback)
        {
            var profiles = new Dictionary<string, RuntimeCharacterProfile>(StringComparer.OrdinalIgnoreCase);

            Add("Lunen", "Lunen", "lunen", leftFallback, true);
            Add("Tinh Linh", "Tinh Linh", "tinh_linh", rightFallback, false);
            Add("???", "???", "elio_unknown", ResolveCharacterSprite(database, "Elio", rightFallback), false);

            AddFromDatabase("Elio", false);
            AddFromDatabase("Sahure", false);

            Add("Rashad", "Rashad", "rashad", rightFallback, false);
            Add("Aurelian", "Aurelian", "aurelian", rightFallback, false);
            Add("Nefru", "Nefru", "nefru", rightFallback, false);
            Add("Bekhet", "Bekhet", "bekhet", rightFallback, false);
            Add("Menkara", "Menkara", "menkara", rightFallback, false);
            Add("Nephkar", "Nephkar", "nephkar", rightFallback, false);
            Add("Khepraen", "Khepraen", "khepraen", rightFallback, false);
            Add("Ramesses", "Ramesses", "ramesses", rightFallback, false);
            Add("Kasim", "Kasim", "kasim", rightFallback, false);
            Add("Vezkara", "Vezkara", "vezkara", rightFallback, false);

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

            void AddFromDatabase(string speaker, bool leftSide)
            {
                Add(speaker, speaker, ResolveDatabaseCharacterId(database, speaker, NormalizeCharacterId(speaker)),
                    ResolveCharacterSprite(database, speaker, leftSide ? leftFallback : rightFallback), leftSide);
            }

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
        }

        static void ApplyCastOverrides(List<DialogueBeat> beats, Dictionary<string, RuntimeCharacterProfile> profiles, ChapterOneStoryCastConfig castConfig)
        {
            ApplyDefaultProfiles(beats, profiles);

            if (castConfig?.beatOverrides == null) return;
            var textMatched = new HashSet<ChapterOneBeatCastOverride>();
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

        static void ApplyBeatOverride(DialogueBeat beat, Dictionary<string, RuntimeCharacterProfile> profiles, ChapterOneBeatCastOverride beatOverride)
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

        static Sprite ResolveCharacterSprite(MenuContentDatabase database, string characterIdOrName, Sprite fallback)
        {
            if (database?.characters != null)
            {
                foreach (var character in database.characters)
                {
                    if (character == null) continue;
                    var matchesId = string.Equals(character.id?.Trim(), characterIdOrName, StringComparison.OrdinalIgnoreCase);
                    var matchesName = string.Equals(character.displayName?.Trim(), characterIdOrName, StringComparison.OrdinalIgnoreCase);
                    if (!matchesId && !matchesName) continue;
                    if (character.fullBody != null) return character.fullBody;
                    if (character.portrait != null) return character.portrait;
                    if (character.chibi != null) return character.chibi;
                }
            }
            return fallback;
        }

        static string ResolveDatabaseCharacterId(MenuContentDatabase database, string characterIdOrName, string fallback)
        {
            if (database?.characters != null)
            {
                foreach (var character in database.characters)
                {
                    if (character == null) continue;
                    var matchesId = string.Equals(character.id?.Trim(), characterIdOrName, StringComparison.OrdinalIgnoreCase);
                    var matchesName = string.Equals(character.displayName?.Trim(), characterIdOrName, StringComparison.OrdinalIgnoreCase);
                    if ((matchesId || matchesName) && !string.IsNullOrWhiteSpace(character.id))
                        return character.id;
                }
            }
            return fallback;
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
            var checkpointNextBeat = false;

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
                        checkpointNextBeat = true;
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
                if (match.Success)
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
            foreach (var beat in parsed.IntroBeats) parsed.AllBeats.Add(beat);
            foreach (var beat in parsed.PreBattleBeats) parsed.AllBeats.Add(beat);
            foreach (var combat in parsed.CombatBlocks)
            {
                foreach (var beat in combat.startBeats) parsed.AllBeats.Add(beat);
                foreach (var trigger in combat.triggers)
                    foreach (var beat in trigger.beats)
                        parsed.AllBeats.Add(beat);
            }
            foreach (var beat in parsed.VictoryBeats) parsed.AllBeats.Add(beat);
            return parsed;

            void FlushSpeech()
            {
                if (string.IsNullOrEmpty(pendingSpeaker) || speech.Length == 0) return;
                var beat = CreateBeat(pendingSpeaker, speech.ToString(), false, background, profiles, leftFallback, rightFallback);
                ApplyCheckpointIfNeeded(beat);
                CurrentTarget().Add(beat);
                pendingSpeaker = null;
                speech.Clear();
            }

            void FlushSceneText()
            {
                if (sceneText.Length == 0) return;
                var beat = CreateBeat(string.Empty, sceneText.ToString(), true, background, profiles, leftFallback, rightFallback);
                ApplyCheckpointIfNeeded(beat);
                CurrentTarget().Add(beat);
                sceneText.Clear();
            }

            void ApplyCheckpointIfNeeded(DialogueBeat beat)
            {
                if (!checkpointNextBeat || beat == null) return;
                beat.fadeToBlackCheckpoint = true;
                checkpointNextBeat = false;
            }
        }

        static void ApplyCombatBlocks(StageEntry stage, ParsedStory parsed, MenuContentDatabase database)
        {
            stage.combatDialogueTriggers ??= new List<CombatDialogueTrigger>();
            stage.combatDialogueTriggers.Clear();
            stage.battlePhases ??= new List<BattlePhaseEntry>();
            stage.battlePhases.Clear();

            if (parsed == null || parsed.CombatBlocks.Count == 0)
            {
                stage.enemies ??= new List<BattleUnitDefinition>();
                stage.enemies.Clear();
                stage.boss = null;
                return;
            }

            for (var i = 0; i < parsed.CombatBlocks.Count; i++)
            {
                var block = parsed.CombatBlocks[i];
                var hasSummonBoss = ContainsAny(block.note, "boss tên là nephkar", "nephkar");
                var phase = new BattlePhaseEntry
                {
                    id = $"chapter_1_scene_1_phase_{i + 1}",
                    title = i == 0 ? "Ma vật trong rừng" : $"Combat {i + 1}",
                    description = block.note,
                    enemyLevel = Mathf.Max(1, stage.enemyLevel),
                    allies = BuildFixedAllies(block.note, database),
                    enemies = hasSummonBoss
                        ? BuildEnemies(block.note, 2)
                        : BuildEnemies(block.note, Mathf.Max(1, InferEnemyCount(block.note, i == 0 ? 4 : 4))),
                    boss = hasSummonBoss ? BuildBoss("Nephkar") : null,
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
            if (ContainsAny(note, "elio", "chọn sẵn"))
                allies.Add(CharacterToBattleDefinition(database, "Elio", "elio_story_guest", "Elio"));
            if (ContainsAny(note, "lunen"))
                allies.Add(CharacterToBattleDefinition(database, "Lunen", "lunen_story_guest", "Lunen"));
            if (ContainsAny(note, "khepraen"))
                allies.Add(CharacterToBattleDefinition(database, "Khepraen", "khepraen_story_guest", "Khepraen"));
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
                portrait = character?.portrait,
                battlefieldSprite = character?.chibi != null ? character.chibi : character?.portrait,
                attackEffectPrefabs = character?.attackEffectPrefabs != null ? new List<GameObject>(character.attackEffectPrefabs) : new List<GameObject>(),
                attackEffectOffset = character?.attackEffectOffset ?? Vector3.zero,
                attackEffectScale = character?.attackEffectScale ?? Vector3.one,
                maxHealth = Mathf.Max(1, character?.maxHealth ?? 140),
                attack = Mathf.Max(1, character?.attack ?? 18),
                isRanged = character?.attributes != null && (character.attributes.Contains("Ranged") || character.attributes.Contains("tầm xa")),
                skills = new List<BattleSkillDefinition> { new BattleSkillDefinition { id = "attack", displayName = "Tấn Công", powerMultiplier = 1f } }
            };
        }

        static List<BattleUnitDefinition> BuildEnemies(string note, int count)
        {
            var result = new List<BattleUnitDefinition>();
            for (var i = 0; i < count; i++)
            {
                result.Add(BuildSmallEnemy(i)); /*
                {
                    id = $"chapter_1_ma_vat_{i + 1}",
                    displayName = "Ma Vật Truy Sát",
                    maxHealth = 90,
                    attack = 12,
                    skills = new List<BattleSkillDefinition> { new BattleSkillDefinition { id = "attack", displayName = "Tấn Công", powerMultiplier = 1f } }
                */
            }
            return result;
        }

        static BattleUnitDefinition BuildSmallEnemy(int index)
        {
            var type = Mathf.Abs(index) % 4;
            switch (type)
            {
                case 0:
                    return NewEnemy($"chapter_1_cat_xoay_cat_{index + 1}", "Cát Xoáy Sa Mạc", "enemy_sand_random_5_percent", "Lao Tới", LoadEnemySprite("sprite-sheet-2frames (2).png"), 95, 10, 5, 13);
                case 1:
                    return NewEnemy($"chapter_1_lua_linh_hon_{index + 1}", "Lửa Linh Hồn", "enemy_blue_heal_20_percent", "Hồi Máu Đồng Đội", LoadEnemySprite("sprite-sheet-2frames (3).png"), 80, 1, 3, 9);
                case 2:
                    return NewEnemy($"chapter_1_quan_tai_khien_{index + 1}", "Quan Tài Hộ Vệ", "enemy_coffin_shield_2000_once", "Tạo Khiên", LoadEnemySprite("sprite-sheet-2frames (4).png"), 110, 1, 8, 8);
                default:
                    return NewEnemy($"chapter_1_thu_lua_nho_{index + 1}", "Thú Lửa Nhỏ", "enemy_fire_aoe_5_percent", "Lửa Lan", LoadEnemySprite("sprite-sheet-2frames (5).png"), 90, 8, 4, 11);
            }
        }

        static BattleUnitDefinition NewEnemy(string id, string displayName, string skillId, string skillName, Sprite sprite, int maxHealth, int attack, int defense, int speed)
        {
            return new BattleUnitDefinition
            {
                id = id,
                displayName = displayName,
                portrait = sprite,
                battlefieldSprite = sprite,
                attackEffectPrefabs = TestEffectsForEnemy(id),
                attackEffectScale = Vector3.one,
                maxHealth = maxHealth,
                attack = attack,
                defense = defense,
                speed = speed,
                skills = new List<BattleSkillDefinition> { new BattleSkillDefinition { id = skillId, displayName = skillName, powerMultiplier = 1f } }
            };
        }

        static Sprite LoadEnemySprite(string fileName)
        {
#if UNITY_EDITOR
            const string folder = "Assets/Art Ui/Game Việt hóa mới/enemy/";
            var path = folder + fileName;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
                if (asset is Sprite found)
                    return found;
#endif
            return null;
        }

        static List<GameObject> TestEffectsForEnemy(string id)
        {
            var result = new List<GameObject>();
            if (id.IndexOf("cat_xoay", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddEffect(result, "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab");
                AddEffect(result, "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_1_Woa_Yellow.prefab");
            }
            else if (id.IndexOf("lua_linh", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddEffect(result, "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Light/CFXR3 Hit Light B (Air).prefab");
                AddEffect(result, "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_Blue.prefab");
            }
            else if (id.IndexOf("thu_lua", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddEffect(result, "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR3 Hit Fire B (Air).prefab");
                AddEffect(result, "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Bomb_Red.prefab");
            }
            return result;
        }

        static void AddEffect(List<GameObject> result, string assetPath)
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null) result.Add(prefab);
#endif
        }

        static BattleUnitDefinition BuildBoss(string displayName)
        {
            var id = NormalizeCharacterId(displayName);
            return new BattleUnitDefinition
            {
                id = string.IsNullOrWhiteSpace(id) ? "story_boss" : id,
                displayName = displayName,
                maxHealth = 260,
                attack = 1,
                defense = 8,
                speed = 12,
                skills = new List<BattleSkillDefinition>
                {
                    new BattleSkillDefinition { id = "summon", displayName = "Triệu Hồi", powerMultiplier = 0.1f }
                }
            };
        }

        static CombatDialogueTrigger BuildCombatTrigger(string phaseId, int index, TriggerBlock block, int phaseIndex, int phaseCount)
        {
            var condition = block.condition ?? string.Empty;
            var action = block.endAction ?? string.Empty;
            var trigger = new CombatDialogueTrigger
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
                    title = $"Combat - {condition}",
                    summary = action,
                    beats = block.beats
                }
            };
            return trigger;
        }

        static CombatDialogueTriggerType InferTriggerType(string condition)
        {
            if (ContainsAny(condition, "combat kết thúc", "kết thúc combat", "win", "thắng", "hết enemy", "hết quái"))
                return CombatDialogueTriggerType.PhaseVictory;
            if (ContainsAny(condition, "ngay khi kết thúc hoạt ảnh", "kết thúc hoạt ảnh", "ánh sáng đỏ bên trên"))
                return CombatDialogueTriggerType.PhaseVictory;
            if (ContainsAny(condition, "đòn đầu tiên", "đánh xong đòn đầu", "toàn bộ đánh xong"))
                return CombatDialogueTriggerType.RoundStart;
            if (ContainsAny(condition, "quái con chết", "quái con được triệu hồi bị tiêu diệt", "quái được triệu hồi đã tiêu diệt"))
                return CombatDialogueTriggerType.EnemyCountAtOrBelow;
            if (ContainsAny(condition, "còn") && ContainsAny(condition, "enemy", "quái", "ma vật"))
                return CombatDialogueTriggerType.EnemyCountAtOrBelow;
            if (ContainsAny(condition, "%", "phần trăm", "nửa máu", "nửa hp", "half hp"))
                return ContainsAny(condition, "tổng", "toàn bộ", "enemy", "quái", "ma vật")
                    ? CombatDialogueTriggerType.TotalEnemyHealthBelowPercent
                    : CombatDialogueTriggerType.BossHealthBelowPercent;
            if (ContainsAny(condition, "round", "lượt"))
                return CombatDialogueTriggerType.RoundStart;
            return CombatDialogueTriggerType.PhaseStart;
        }

        static CombatTriggerActionType InferTriggerAction(string action, int phaseIndex, int phaseCount)
        {
            if (ContainsAny(action, "giết tất cả enemy", "giết toàn bộ enemy", "giết tất cả quái", "giết toàn bộ quái", "không hiện win/lose", "thực hiện trigger tiếp theo"))
                return CombatTriggerActionType.KillAllEnemiesAndPlayPhaseVictory;
            if (ContainsAny(action, "kéo xuống còn 10% hp", "xuống còn 10%hp", "10% hp", "10%hp"))
                return CombatTriggerActionType.SetElioHealthToTenPercentAndPlayPhaseVictory;
            if (ContainsAny(action, "hồi hp của elio lên 35%", "hồi hp elio lên 35%", "35%"))
                return CombatTriggerActionType.HealElioToThirtyFivePercent;
            if (ContainsAny(action, "đồng minh mới là aurelian", "aurelian trong team", "aurelian"))
                return CombatTriggerActionType.AddAurelianAlly;
            if (ContainsAny(action, "không cần hiển thị win/lose", "không hiện win/lose", "quay trở về story panel"))
                return CombatTriggerActionType.ReturnToStoryWithoutResult;
            var convert = ContainsAny(action, "về đội", "vào đội", "đồng hành", "chuyển phe");
            var nextPhase = ContainsAny(action, "combat tiếp", "combat lần", "phase", "trận tiếp", "tiếp tục combat mới");
            if (!nextPhase && phaseIndex + 1 < phaseCount && convert) nextPhase = true;
            if (convert && nextPhase) return CombatTriggerActionType.ConvertUnitToAllyAndStartNextPhase;
            if (convert) return CombatTriggerActionType.ConvertUnitToAlly;
            if (nextPhase) return CombatTriggerActionType.StartNextPhase;
            return CombatTriggerActionType.None;
        }

        static int InferRound(string text)
        {
            if (ContainsAny(text, "đòn đầu tiên", "đánh xong đòn đầu", "toàn bộ đánh xong"))
                return 2;
            var match = Regex.Match(text ?? string.Empty, @"(?:round|lượt)\s*(\d+)", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Mathf.Max(1, value) : 1;
        }

        static bool ShouldEndTriggerAtCheckpoint(TriggerBlock trigger)
        {
            if (trigger == null) return false;
            return ContainsAny(trigger.condition, "trận đấu kết thúc") && trigger.beats.Count > 0;
        }

        static int InferEnemyCount(string text, int fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            if (ContainsAny(text, "2 con quái con chết", "2 con quái con được triệu hồi bị tiêu diệt", "2 con quái được triệu hồi đã tiêu diệt"))
                return 1;
            var match = Regex.Match(text, @"(?:còn|có)\s*(\d+)\s*(?:con\s*)?(?:enemy|quái|ma vật)", RegexOptions.IgnoreCase);
            if (!match.Success) match = Regex.Match(text, @"\d+\s*v\s*(\d+)", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Mathf.Max(0, value) : fallback;
        }

        static int InferPercent(string text)
        {
            if (ContainsAny(text, "nửa máu", "nửa hp", "half hp")) return 50;
            var match = Regex.Match(text ?? string.Empty, @"(\d+)\s*%");
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Mathf.Clamp(value, 1, 100) : 50;
        }

        static bool ContainsAny(string text, params string[] needles)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            foreach (var needle in needles)
                if (!string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
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
                hideAllCharacters = isSceneText,
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

        static List<DialogueBeat> FindWindow(List<DialogueBeat> source, int start, int end, string needle, int count)
        {
            var index = FindBeatIndex(source, needle, start, end);
            if (index < 0) index = start;
            return CopyRange(source, index, Mathf.Min(index + Mathf.Max(1, count), end));
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

        static bool IsDivider(string line) => line.StartsWith("___", StringComparison.Ordinal) || line == " ";
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
            return line.Trim().Trim('“', '”', '"').Trim();
        }

        static bool IsQuotedLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            line = line.Trim();
            return (line.StartsWith("\"", StringComparison.Ordinal) && line.EndsWith("\"", StringComparison.Ordinal)) ||
                   (line.StartsWith("“", StringComparison.Ordinal) && line.EndsWith("”", StringComparison.Ordinal));
        }

        static void AppendLine(StringBuilder builder, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line);
        }
    }
}
