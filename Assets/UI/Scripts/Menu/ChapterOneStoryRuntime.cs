using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

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
            "Main Story/Chương 1 cảnh 6"
        };
        const string CastConfigResourcePath = "Data/ChapterOneStoryCastConfig";

        static readonly Regex SpeakerLine = new(@"^\s*(?<speaker>[A-Za-zÀ-ỹ0-9?&\s]+)\s*:\s*$", RegexOptions.Compiled);
        static readonly HashSet<string> KnownSpeakers = new(StringComparer.OrdinalIgnoreCase)
        {
            "???", "Lunen", "Tinh Linh", "Tinh linh", "Elio", "Rashad", "Aurelian",
            "Sahure", "Nefru", "Bekhet", "Menkara", "Nephkar", "Khepraen",
            "Ramesses", "Kasim", "Vezkara"
        };

        public static void Apply(MenuContentDatabase database)
        {
            if (database == null || database.storyChapters == null) return;
            var castConfig = Resources.Load<ChapterOneStoryCastConfig>(CastConfigResourcePath);
            if (castConfig == null || !castConfig.autoGenerateChapterFromTextFiles) return;

            var chapter = database.storyChapters.Find(x => x != null && x.id == ChapterId);
            if (chapter == null) return;

            var source = LoadStorySource();
            if (string.IsNullOrWhiteSpace(source)) return;

            var leftFallback = ResolveLeftSprite(chapter);
            var rightFallback = ResolveRightSprite(chapter);
            var profiles = BuildCharacterProfiles(database, castConfig, leftFallback, rightFallback);
            var allBeats = ParseBeats(source, chapter.background, profiles, leftFallback, rightFallback);
            if (allBeats.Count == 0) return;
            ApplyCastOverrides(allBeats, profiles, castConfig);

            chapter.title = "Chương 1 - Ngọn Lửa Dưới Mặt Trời Sa Mạc";
            chapter.summary = "Lunen lần theo chiếc vòng ngọc tới biên giới Akherat, gặp Elio, bị cuốn vào âm mưu truy sát vương tử, chứng kiến biến cố của Hỏa thần Aurelian và sự ra đời của vị vua con người đầu tiên của Akherat.";

            var battleStart = FindBeatIndex(allBeats, "Đám ma vật đồng loạt lao tới");
            var forestCalm = FindBeatIndex(allBeats, "Khu rừng cuối cùng cũng trở lại yên tĩnh");
            var firstWarning = FindBeatIndex(allBeats, "Ta không thích phải nói lần thứ hai");

            if (firstWarning < 0) firstWarning = Mathf.Min(24, allBeats.Count);
            if (battleStart < 0) battleStart = Mathf.Min(firstWarning + 35, allBeats.Count);
            if (forestCalm < 0 || forestCalm <= battleStart) forestCalm = Mathf.Min(battleStart + 18, allBeats.Count);

            chapter.introDialogue = new DialogueSequence
            {
                id = "chapter_1_intro",
                title = "Mở đầu - Rừng biên giới Akherat",
                summary = "Lunen và Tinh Linh lần theo chiếc vòng ngọc, bị một con mèo đen cướp vòng và chạm mặt thiếu niên bí ẩn.",
                beats = CopyRange(allBeats, 0, firstWarning)
            };

            var stage = EnsureFirstStage(chapter);
            stage.id = "chapter_1_stage_1";
            stage.title = "Rừng biên giới Akherat";
            stage.description = "Lunen và Elio từ hiểu lầm ban đầu buộc phải cùng chiến đấu khi ma vật xuất hiện, để lộ mục tiêu thật sự của chúng là vương tử Akherat.";
            stage.preBattleDialogue = new DialogueSequence
            {
                id = "chapter_1_stage_1_prebattle",
                title = "Trước trận - Vương tử bị truy sát",
                summary = "Cuộc chạm trán với thiếu niên bí ẩn chuyển thành trận chiến thật sự khi ma vật nhắm thẳng vào cậu.",
                beats = CopyRange(allBeats, firstWarning, battleStart)
            };
            stage.victoryDialogue = new DialogueSequence
            {
                id = "chapter_1_stage_1_victory",
                title = "Sau trận - Akherat và ngọn lửa kế vị",
                summary = "Elio đưa Lunen vào Akherat; hoàng cung, đại lễ, biến cố Aurelian, thử luyện Hỏa Ấn và lời tiễn biệt được mở ra theo đúng tuyến truyện.",
                beats = CopyRange(allBeats, forestCalm, allBeats.Count)
            };

            ApplyCombatDialogue(stage, allBeats, battleStart, forestCalm);
            ApplyEnemyNames(stage);
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

        static Dictionary<string, RuntimeCharacterProfile> BuildCharacterProfiles(MenuContentDatabase database, ChapterOneStoryCastConfig castConfig, Sprite leftFallback, Sprite rightFallback)
        {
            var profiles = new Dictionary<string, RuntimeCharacterProfile>(StringComparer.OrdinalIgnoreCase);

            Add("Lunen", "Lunen", "lunen", leftFallback, true);
            Add("Tinh Linh", "Tinh Linh", "tinh_linh", leftFallback, true);
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

        static List<DialogueBeat> ParseBeats(
            string source,
            Sprite background,
            Dictionary<string, RuntimeCharacterProfile> profiles,
            Sprite leftFallback,
            Sprite rightFallback)
        {
            var beats = new List<DialogueBeat>();
            var sceneText = new StringBuilder();
            string pendingSpeaker = null;
            var speech = new StringBuilder();

            foreach (var raw in source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || IsDivider(line))
                {
                    FlushSpeech();
                    continue;
                }

                var match = SpeakerLine.Match(line);
                if (match.Success && IsKnownSpeaker(match.Groups["speaker"].Value))
                {
                    FlushSpeech();
                    FlushSceneText();
                    pendingSpeaker = NormalizeSpeaker(match.Groups["speaker"].Value);
                    continue;
                }

                if (!string.IsNullOrEmpty(pendingSpeaker))
                {
                    AppendLine(speech, CleanLine(line));
                    continue;
                }

                AppendLine(sceneText, CleanLine(line));
            }

            FlushSpeech();
            FlushSceneText();
            return beats;

            void FlushSpeech()
            {
                if (string.IsNullOrEmpty(pendingSpeaker) || speech.Length == 0) return;
                beats.Add(CreateBeat(pendingSpeaker, speech.ToString(), false, background, profiles, leftFallback, rightFallback));
                pendingSpeaker = null;
                speech.Clear();
            }

            void FlushSceneText()
            {
                if (sceneText.Length == 0) return;
                beats.Add(CreateBeat(string.Empty, sceneText.ToString(), true, background, profiles, leftFallback, rightFallback));
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

        static bool IsKnownSpeaker(string speaker) => KnownSpeakers.Contains(NormalizeSpeaker(speaker));
        static bool IsDivider(string line) => line.StartsWith("___", StringComparison.Ordinal) || line == " ";
        static bool IsLeftSpeaker(string speaker) => string.Equals(speaker, "Lunen", StringComparison.OrdinalIgnoreCase) || string.Equals(speaker, "Tinh Linh", StringComparison.OrdinalIgnoreCase);

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

        static void AppendLine(StringBuilder builder, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line);
        }
    }
}
