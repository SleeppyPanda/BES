# Beneath Enchanted Sky (BES) — Unity MVP

## Dev Update Log & Feature-to-Script Map

Cập nhật gần nhất: 2026-07-10. Mục này dùng cho dev/QA đối chiếu nhanh tính năng trong game với script đang phụ trách.

### Update mới đã code

| Nhóm tính năng | Trạng thái hiện tại | Script / Asset chính |
|---|---|---|
| Movement WASD | Player đọc trực tiếp `W/A/S/D` và arrow fallback, không phụ thuộc hoàn toàn InputAction | `Assets/_Project/Scripts/Gameplay/PlayerMotor.cs`, `PlayerInputReader.cs` |
| Camera third-person | Chuột xoay camera; giữ `Shift` để khóa camera, không xoay theo chuột | `Assets/_Project/Scripts/Gameplay/ThirdPersonCamera.cs` |
| Input fallback | Fallback keyboard cho move, jump, skill, interact, menu | `Assets/_Project/Scripts/Gameplay/PlayerInputReader.cs` |
| Player bootstrap | Nếu scene đã có `Player`, vẫn tự gắn đủ component gameplay cần thiết | `Assets/_Project/Scripts/Gameplay/GameplaySceneBootstrap.cs` |
| NPC "Người yêu cũ" | Đã thêm NPC test trong scene, interact bằng `F` hoặc `E` | `Assets/_Project/Scenes/Gameplay.unity`, `Assets/_Project/Scripts/Narrative/NPCInteractable.cs` |
| Dialogue "Người yêu cũ" | Đã có câu chào, nút Next, lựa chọn `Có/Không`, branch hội thoại | `Assets/_Project/Scripts/Narrative/DialogueSystem.cs`, `Assets/_Project/Scripts/UI/DialogueUI.cs`, `Assets/_Project/Resources/Dialogue/Node_NguoiYeuCu*.asset` |
| Dialogue UI fallback | Nếu prefab UI thiếu binding, runtime tự tạo panel hội thoại compact | `Assets/_Project/Scripts/UI/DialogueUI.cs` |
| Party 4 nhân vật | Đã có 4 nhân vật test, đổi bằng phím `1-4` | `Assets/_Project/Scripts/UI/Data/CharacterDatabase.cs`, `PartyRoster.cs`, `PartySwapController.cs` |
| Party visual test | Đổi party sẽ đổi visual capsule màu/scale để test nhanh | `Assets/_Project/Scripts/Gameplay/PartyCharacterVisualSwitcher.cs` |
| Skill theo nhân vật | Mỗi nhân vật có 2 skill riêng qua `skill1Id/skill2Id` | `Assets/_Project/Scripts/Gameplay/Combat/SkillController.cs`, `CharacterDatabase.asset` |
| Skill bar | Hiện cooldown skill/attack trên HUD | `Assets/_Project/Scripts/UI/SkillBarUI.cs`, `SkillBarDriver.cs` |
| Gacha/Wish | Roll x1/x10, pity, reward character/weapon/item local | `Assets/_Project/Scripts/UI/WishUI.cs`, `GachaBannerDefinition.cs`, `GachaPityState.cs`, `GachaRewardService.cs` |
| Mini map | Hiện player/objective marker theo world bounds | `Assets/_Project/Scripts/UI/MiniMapUI.cs` |
| Quest checker | Tự check tiến độ quest theo NPC, region, enemy, item, dialogue | `Assets/_Project/Scripts/Narrative/QuestObjectiveTracker.cs`, `QuestManager.cs` |
| Quest tracker/log | HUD tracker + quest log active/completed | `Assets/_Project/Scripts/UI/QuestTrackerUI.cs`, `QuestLogUI.cs` |
| QA checklist | Context menu runtime để check HUD, party, gacha, quest, save | `Assets/_Project/Scripts/Gameplay/OpenWorldSliceValidator.cs` |
| Animation gameplay | Chưa có clip/controller animation production; hiện mới có visual test capsule | Chưa có `.anim` / Animator Controller trong `Assets/_Project` |

### Tên NPC và nhân vật test

| Loại | ID / Slot | Tên hiển thị | Ghi chú |
|---|---|---|---|
| NPC | `npc_merchant` | `Người yêu cũ` | Object scene: `NPC_NguoiYeuCu`; dialogue node: `nguoi_yeu_cu_intro` |
| NPC | `npc_guard` | `City Guard` | Main quest guard cũ |
| Party slot 1 | `hero_01` | `Đau hơn NYC bạn` | Skill: `Void Slash`, `Guard Break` |
| Party slot 2 | `hero_02` | `Mất cô ấy rồi` | Skill: `Quick Cut`, `Flare Dash` |
| Party slot 3 | `hero_03` | `Anh là thằng tồi` | Skill: `Shield Wave`, `Ground Lock` |
| Party slot 4 | `hero_04` | `Nhìn em bên ai khác` | Skill: `Arc Bolt`, `Focus Shot` |

### Flow dialogue mẫu của `Người yêu cũ`

| Bước | Speaker | Nội dung / Lựa chọn | Next |
|---|---|---|---|
| 1 | Người yêu cũ | `Mừng vì anh đã quay lại...Lốp Trưởng!` | Nút `Tiếp tục` |
| 2 | Người yêu cũ | `Anh có nhớ em không?` | Lựa chọn `Có` / `Không` |
| 3A | Người yêu cũ | Nếu chọn `Có`: `Vậy hả, em cũng không nhớ anh...` | Kết thúc |
| 3B | Người yêu cũ | Nếu chọn `Không`: `Không thể ngừng nhớ em đúng không?` | Nút `Tiếp tục` |
| 4B | Nhân vật | `Đu...Đúng.........` | Kết thúc |

### Feature script map chi tiết

| Feature | Script/Asset | Vai trò dev |
|---|---|---|
| Scene gameplay setup | `GameplaySceneBootstrap.cs` | Tạo player/camera/system cần thiết nếu scene thiếu |
| Event bus | `GameEvents.cs` | Nối các hệ thống UI, quest, dialogue, party |
| Input gameplay | `PlayerInputReader.cs` | InputAction + fallback keyboard |
| Di chuyển | `PlayerMotor.cs` | WASD, jump, sprint/stamina, gravity |
| Camera | `ThirdPersonCamera.cs` | Third-person follow/orbit, Shift lock camera |
| Block input | `GameplayInputGate.cs` | Chặn combat/UI; movement chỉ bị chặn khi dialogue active |
| Basic attack | `BasicAttackController.cs` | Combo attack và damage hit check |
| Skill combat | `SkillController.cs` | Skill theo nhân vật, cooldown/mana/range/damage |
| Dodge | `DodgeController.cs` | Dash/dodge và i-frame |
| Damage formula | `DamageCalculator.cs` | Tính damage/crit |
| Enemy AI | `EnemyAI.cs` | Chase/attack target |
| Boss | `BossController.cs` | Boss phase/special attack |
| Party state | `PartyRoster.cs` | Slot party, active character, unlock/save |
| Character DB | `CharacterDatabase.cs`, `CharacterDatabaseLoader.cs`, `CharacterDatabase.asset` | Tên, stat, visual test, skill id, default party |
| Party UI | `PartyStripUI.cs`, `TeamSetupUI.cs`, `CharacterProfileUI.cs` | Hiện và đổi nhân vật trên UI |
| Party visual | `PartyCharacterVisualSwitcher.cs` | Đổi màu/scale capsule hoặc prefab gameplay theo character |
| NPC interact | `NPCInteractable.cs`, `QuestMarker.cs` | Range prompt, phím `F/E`, mở story dialogue |
| Dialogue graph | `DialogueNode.cs`, `DialogueSystem.cs` | Node, next, choice, branch, fallback built-in |
| Dialogue UI | `DialogueUI.cs` | Panel hội thoại, Next, choices, free chat fallback |
| AI chat | `AIDialogueService.cs`, `NPCMemoryStore.cs`, `RelationshipSystem.cs` | Response fallback/HTTP optional, memory, affinity |
| Quest data | `QuestDefinition.cs`, `QuestDatabase.cs` | ScriptableObject quest definitions |
| Quest runtime | `QuestManager.cs`, `QuestObjectiveTracker.cs` | Active quest, step progress, auto-check objective |
| Quest marker | `QuestMarker.cs` | Target id cho NPC/objective/minimap |
| Quest UI | `QuestTrackerUI.cs`, `QuestLogUI.cs` | HUD current quest + quest log |
| Mini map | `MiniMapUI.cs` | Player/objective marker |
| World map/teleport | `GameMapUI.cs`, `TeleportService.cs`, `TeleportPoint.cs`, `WorldRegion.cs` | Region, fast travel, discovery |
| Inventory | `InventorySystem.cs`, `ItemDatabase.cs`, `InventoryUI.cs` | Item bag, add/use/equip |
| Save/load | `SaveSystem.cs`, `SaveData.cs`, `GameAutoSave.cs` | JSON save/load meta + world state |
| Wallet | `PlayerWallet.cs` | Gems/coins local |
| Gacha | `WishUI.cs`, `GachaBannerDefinition.cs`, `GachaPityState.cs`, `GachaRewardService.cs` | Banner, pity, reward apply |
| Weapon | `WeaponDatabase.cs`, `WeaponDefinition.cs`, `EquippedWeaponState.cs`, `WeaponScreenUI.cs` | Equip/enhance/rank/refine state/UI |
| Artifact | `ArtifactDatabase.cs`, `ArtifactDefinition.cs`, `ArtifactsUI.cs` | Artifact equip/stat bonus |
| Event daily | `EventDefinition.cs`, `EventUI.cs` | Daily check-in local |
| HUD assets | `HUDSpriteManifest.cs`, `HUDSpriteManifestLoader.cs` | Icon/frame sprite map |
| Screen backgrounds | `UIScreenBackgroundManifest.cs`, `UIScreenBackgroundBootstrap.cs` | Background theo screen |
| UI navigation | `UINavigationController.cs`, `UIScreenBase.cs` | Mở/đóng các layer UI |
| UI setup/editor | `BESProjectSetup.cs`, `BESUIDataSetup.cs`, `BESUIPrefabBuilder.cs` | Tạo data/prefab/default setup trong Unity Editor |
| Runtime QA | `OpenWorldSliceValidator.cs` | Checklist nhanh trong scene |

---

Action RPG vertical slice: **Story + Quest + AI NPC + Open World demo** (backlog 3 tháng, 13 Epics / 70 User Stories).

---

## Quick Start

1. Mở project bằng **Unity 6000.3 LTS** (hiện tại: `6000.3.16f1`)
2. Menu: **BES → Setup Project (US-001 to US-006)**
3. Play scene `Assets/_Project/Scenes/MainMenu.unity`

**Build:** `MainMenu` → `Gameplay` → `PrototypeScene` (xem [docs/BUILD.md](docs/BUILD.md))

---

## Tài liệu kỹ thuật & thiết kế

| Tài liệu | Mục đích |
|----------|----------|
| [CONVENTIONS.md](docs/CONVENTIONS.md) | Coding & naming convention |
| [GIT_WORKFLOW.md](docs/GIT_WORKFLOW.md) | Branch `main` / `dev` / `feature/*` |
| [TECHNICAL.md](docs/TECHNICAL.md) | Stack, kiến trúc code, entry points |
| [BUILD.md](docs/BUILD.md) | Build Windows, performance target |
| [QA_CHECKLIST.md](docs/QA_CHECKLIST.md) | Checklist nghiệm thu chức năng |
| [WORLD_LORE.md](docs/design/WORLD_LORE.md) | Lore thế giới Guarem |
| [STORY_BIBLE.md](docs/design/STORY_BIBLE.md) | Cốt truyện, nhánh, NPC, boss |
| [CHARACTER_DESIGN.md](docs/design/CHARACTER_DESIGN.md) | Stats nhân vật / enemy / boss |
| [UI_UX_SCOPE.md](docs/design/UI_UX_SCOPE.md) | Trạng thái màn hình UI |
| [UI_LAYOUT.md](docs/design/UI_LAYOUT.md) | Layout Figma 1920×1080 |
| [META_UI_PHASE2.md](docs/design/META_UI_PHASE2.md) | Roadmap UI meta widget-based |
| [ART_PIPELINE.md](docs/design/ART_PIPELINE.md) | Pipeline art 3D/2D |

---

# Ghi chú hệ thống — Góc nhìn Business Analyst

> Tài liệu này tổng hợp toàn bộ phạm vi nghiệp vụ, luồng người dùng, entity dữ liệu, trạng thái triển khai và khoảng trống cần xử lý — dựa trên backlog 70 US, GDD, docs thiết kế và codebase thực tế.

---

## 1. Tổng quan sản phẩm

### 1.1 Vision

**BES (Beneath Enchanted Sky)** là game open-world action RPG lấy cảm hứng Genshin-like, tập trung vào:

- Trải nghiệm **cốt truyện có nhánh** và hậu quả lựa chọn
- **NPC thông minh** (AI dialogue + memory + affinity)
- **Vertical slice** chơi được: 3 vùng, combat, quest, save/load, HUD đầy đủ

### 1.2 Điểm khác biệt (USP — từ backlog)

| USP | Mô tả nghiệp vụ | Trạng thái MVP |
|-----|-----------------|----------------|
| AI-Driven NPC Dialogue | NPC trò chuyện tự nhiên, không chỉ script cố định | Fallback offline; LLM chưa tích hợp HTTP |
| Living Memory NPC | NPC ghi nhớ chủ đề / câu thoại trước | Có (`NPCMemoryStore`) |
| Dynamic Story Evolution | Cốt truyện thay đổi theo lựa chọn | Có (2 nhánh A/B, 2 ending) |
| Relationship System | Affinity → disposition ảnh hưởng phản hồi | Có (`RelationshipSystem`) |
| Persistent World | Quyết định lưu vào save, ảnh hưởng tiến trình | Có (JSON save) |

### 1.3 Phạm vi release MVP vs Full GDD

| Hạng mục | MVP (hiện tại) | Full GDD (Phase 2+) |
|----------|----------------|---------------------|
| Thế giới | 3 vùng blockout + teleport | Bản đồ mở rộng, nhiều quốc gia |
| Combat | Basic attack, 2 skill, dodge, 1 boss 2 phase | Element reaction, party combat |
| Narrative | Main quest 4 bước + 1 side quest + 2 ending | Nhiều arc, daily quest, dynamic side quest |
| AI NPC | Fallback + memory + affinity | LLM API thật, emotion-based quest |
| Meta systems | Gacha, weapon, artifact, team, event (local) | Server sync, monetization, battle pass |
| Art | Greybox + 830 PNG UI mockup Figma | Rigged character, environment 3D |
| Multiplayer | Không | Có trong GDD |

---

## 2. Stakeholder & Persona

| Persona | Mục tiêu | Touchpoint chính |
|---------|----------|------------------|
| **Player (Core)** | Khám phá Guarem, hoàn thành main quest, trải nghiệm combat | Gameplay scene, HUD, dialogue |
| **Player (Meta)** | Quản lý inventory, team, weapon, gacha | Meta UI screens |
| **QA / PO** | Nghiệm thu vertical slice, regression | QA checklist, `OpenWorldSliceValidator` |
| **Dev / Tech** | Mở rộng content qua ScriptableObject | Editor menu `BES → Setup Project` |
| **Designer (UI/UX)** | Pixel-parity Figma 1920×1080 | `UI_LAYOUT.md`, art `Art/UI/` |

---

## 3. Backlog & Epic Map

**Quy mô:** 13 Epics · 70 User Stories · ~532 Story Points · 3 tháng

| Epic | Tên | SP | Module chính |
|------|-----|-----|--------------|
| E01 | Project Setup & Infrastructure | 13 | Unity, Git, Input, Scene, Prototype |
| E02 | World Lore & Content Design | 29 | Lore, regions, factions |
| E03 | Story & Narrative Design | 34 | Main/branch story, Story Bible |
| E04 | Character & NPC Design | 39 | Hero, companion, enemy, boss |
| E05 | UI/UX Design | 34 | HUD, menu, dialogue, inventory |
| E06 | Art Production | 68 | Concept, model, rig, animation, UI art |
| E07 | Core Gameplay | 50 | Movement, camera, stamina, save, inventory |
| E08 | Combat System | 58 | Attack, skill, dodge, damage, boss |
| E09 | AI NPC System | 55 | Interaction, dialogue, memory, relationship |
| E10 | Story & Quest System | 47 | Dialogue graph, main/side quest, branching |
| E11 | Open World Integration | 55 | Map, NPC placement, collectibles, teleport |
| E12 | Functional Testing | 25 | Gameplay, combat, AI, quest, save QA |
| E13 | Optimization & Release | 26 | Performance, bug fix, build, docs |

**Roadmap 3 tháng:**

- **Tháng 1:** E01–E04 (nền tảng + thiết kế nội dung)
- **Tháng 2:** E05–E09 (UI/UX, art, gameplay, combat, AI)
- **Tháng 3:** E10–E13 (quest integration, open world, QA, release)

---

## 4. Kiến trúc nghiệp vụ (Business Capabilities)

```
┌─────────────────────────────────────────────────────────────┐
│                    PLAYER EXPERIENCE LAYER                   │
│  Main Menu → New Game / Continue → Gameplay Loop → Ending   │
└──────────────────────────┬──────────────────────────────────┘
                           │
     ┌─────────────────────┼─────────────────────┐
     ▼                     ▼                     ▼
┌─────────┐         ┌─────────────┐       ┌─────────────┐
│ Gameplay │         │  Narrative  │       │  Meta / UI  │
│ Movement │         │ Quest       │       │ Inventory   │
│ Combat   │         │ Dialogue    │       │ Gacha/Wish  │
│ World    │         │ AI NPC      │       │ Weapon/Team │
│ Save     │         │ Relationship│       │ Event/Map   │
└─────────┘         └─────────────┘       └─────────────┘
                           │
                    ┌──────┴──────┐
                    │  GameEvents  │  (Event Bus — decouple)
                    └─────────────┘
```

### 4.1 Entry points runtime

| Component | Vai trò nghiệp vụ |
|-----------|-------------------|
| `Bootstrapper` | Khởi tạo persistent systems trước scene đầu |
| `GameManager` | Orchestrator: NewGame, Continue, SaveGame |
| `SceneLoader` | Chuyển MainMenu ↔ Gameplay ↔ Prototype |
| `GameplaySceneBootstrap` | Spawn player, camera, apply save, start main quest |
| `UINavigationController` | Điều hướng 4 layer UI, chặn input gameplay |

---

## 5. Luồng người dùng (User Journeys)

### 5.1 First-time player

```
Main Menu → Click to Begin / New Game
  → [Optional] Server Picker (Asian default)
  → Loading Screen
  → Gameplay spawn tại Creation City
  → Auto-start quest "main_awakening"
  → Tương tác NPC_Guard (F) → dialogue → advance quest step 1
  → Di chuyển → Region_Ruins → step 2
  → Combat Boss_VoidGuardian → step 3
  → Dialogue ending_choice → Branch A hoặc B → step 4
  → Ending: ending_guardian_pact | ending_void_whisper
  → Reward: relic_shard
```

### 5.2 Returning player

```
Main Menu → Continue (nếu có bes_save.json)
  → Load SaveData → restore position, quests, inventory, meta state
  → Gameplay tiếp tục từ checkpoint
```

### 5.3 Meta loop (trong gameplay)

```
HUD Nav Bar / Hotkey
  → Inventory (Tab/I) | Character (C) | Map (M)
  → Weapon (F2) | Wish/Gacha (F3) | Team (F4)
  → Event check-in (F5) | Artifacts (F6)
  → ESC đóng menu → GameplayInputGate unblock
```

---

## 6. Yêu cầu chức năng theo module

### 6.1 Gameplay Core (E07)

| ID | Chức năng | Acceptance (tóm tắt) | Component |
|----|-----------|----------------------|-----------|
| FR-MOV-01 | Di chuyển WASD | Ổn định, không rơi map | `PlayerMotor` |
| FR-MOV-02 | Sprint tiêu stamina | Drain/regen đúng thiết kế | `StaminaSystem` |
| FR-MOV-03 | Jump | Nhảy + gravity | `PlayerMotor` |
| FR-MOV-04 | Camera third-person | Orbit theo look input | `ThirdPersonCamera` |
| FR-MOV-05 | Party swap 1–4 | Đổi nhân vật active | `PartySwapController` |
| FR-SAV-01 | Save JSON | Ghi `persistentDataPath/bes_save.json` | `SaveSystem` |
| FR-SAV-02 | Auto-save | Periodic + on quit | `GameAutoSave` |
| FR-INV-01 | Inventory add/use/equip | Quản lý item bag | `InventorySystem` |

**Baseline stats (MVP):** HP 100 · ATK 15 · DEF 5 · Mana 100 · Stamina 100

### 6.2 Combat (E08)

| ID | Chức năng | Input | Component |
|----|-----------|-------|-----------|
| FR-CBT-01 | Basic attack combo | Left Mouse | `BasicAttackController` |
| FR-CBT-02 | Skill 1 / 2 | Q / E | `SkillController` |
| FR-CBT-03 | Dodge + i-frames | Left Ctrl | `DodgeController` |
| FR-CBT-04 | Damage formula | ATK/DEF/crit | `DamageCalculator` |
| FR-CBT-05 | Enemy AI | Chase → melee | `EnemyAI` |
| FR-CBT-06 | Boss 2 phase | Special mỗi 6s | `BossController` |

**Enemy — Void Slime:** HP 50 · ATK 8 · DEF 2  
**Boss — Void Guardian:** HP demo ~50 · 2 phase · tied main quest climax

### 6.3 Narrative & Quest (E09, E10)

| ID | Chức năng | Mô tả |
|----|-----------|-------|
| FR-NAR-01 | Story dialogue graph | Node + choices + branchId |
| FR-NAR-02 | Main quest pipeline | 4 step types: Talk, Reach, Defeat, Choice |
| FR-NAR-03 | Side quest | Collect herbs ×3 |
| FR-NAR-04 | Branching ending | branch_a → ending_guardian_pact; branch_b → ending_void_whisper |
| FR-NAR-05 | Quest tracker HUD | Title + step + compass arrow |
| FR-NAR-06 | AI free chat | Input text → fallback response theo disposition |
| FR-NAR-07 | NPC memory | Lưu fact từ exchange |
| FR-NAR-08 | Affinity | Adjust ± → disposition label |

**Quest inventory (data):**

| questId | Loại | Mô tả |
|---------|------|-------|
| `main_awakening` | Main | Guard → Ruins → Boss → Choice |
| `side_collect_herbs` | Side | Thu 3 `herb_common` |
| `ending_guardian_pact` | Ending A | Path of Light |
| `ending_void_whisper` | Ending B | Path of Truth |

**Dialogue nodes:**

| nodeId | Speaker | Ghi chú |
|--------|---------|---------|
| `intro_guard` | City Guard | Trigger main quest — **cần chạy Setup** (asset hiện rỗng) |
| `intro_guard_lore` | City Guard | Lore Guarem (VI) |
| `ending_choice` | Spirit of Guarem | Chọn nhánh A/B |

### 6.4 Open World (E11)

| ID | Chức năng | Entity scene |
|----|-----------|--------------|
| FR-WLD-01 | Region discovery | `Region_CreationCity`, `Region_Forest`, `Region_Ruins` |
| FR-WLD-02 | Teleport | `Teleport_CityToRuins` → `Dest_Ruins` |
| FR-WLD-03 | Collectibles | `Collectible_Herb_0/1/2` → inventory |
| FR-WLD-04 | World map + fast travel | `GameMapUI` + `TeleportService` |
| FR-WLD-05 | NPC placement | `NPC_Guard` |
| FR-WLD-06 | World integration bootstrap | Side quest auto-start, starter materials |

**Regions (lore):**

| regionId | Tên | Vai trò |
|----------|-----|---------|
| `region_creation_city` | Creation City Outskirts | Hub, tutorial |
| `region_ruins` | Ancient Ruins | Boss, branch choice |
| `region_forest` | Whispering Forest | Side quest, herbs |

### 6.5 Meta Progression (Phase 2 trong GDD — đã có skeleton MVP)

| ID | Chức năng | Trạng thái |
|----|-----------|------------|
| FR-META-01 | Player wallet (coins/gems) | Implemented local |
| FR-META-02 | Party roster 4 slot | Implemented |
| FR-META-03 | Weapon equip/enhance/rank/refine | UI + state có; data cần Setup |
| FR-META-04 | Artifact equip | Implemented |
| FR-META-05 | Gacha banner x1/x10 | Implemented local + pity 90 |
| FR-META-06 | Daily check-in event 7 ngày | Implemented |
| FR-META-07 | Server picker | Shell UI + PlayerPrefs |

**Gacha banner (setup script):** Standard banner · 160 gems/pull · drops weapon/character/material

**Weapons (planned data):** Iron Sword (3★) · Void Edge (4★) · Bane of Flame and Water (5★)

### 6.6 UI/UX (E05)

**Canvas:** 1920×1080 · Shrink mode · `UICanvasFit`

**4 navigation layers** (`UINavigationController`):

| Layer | Màn hình | Hotkey |
|-------|----------|--------|
| 0 — HUD | HP/Stamina/Mana, MiniMap, QuestTracker, SkillBar, PartyStrip, NavBar | Always on |
| 1 — Overlay | Inventory, Character, Map, Weapon, Artifacts | Tab/I, C, M, F2, F6 |
| 2 — Meta | Team, Event, Wish | F4, F5, F3 |
| 3 — Modal | Dialogue, Loading, Weapon enhance flow, QuestLog | F, J, Esc |

**Screen status** (theo `UI_UX_SCOPE.md`):

| Screen | Functional | Visual |
|--------|------------|--------|
| Main Menu, Loading, HUD, Mini-map, Quest, Interact, Dialogue | Complete | Composite |
| Inventory, Character, Weapon flow, Map, Team, Event, Wish, Artifacts | Complete | Composite |
| Settings, Server Picker | Complete | Shell |

**Art UI:** 830 PNG mockup Figma tại `Assets/_Project/Art/UI/` — mapped qua `HUDSpriteManifest`, `UIScreenBackgroundManifest`, `CharacterPortraitManifest`.

---

## 7. Mô hình dữ liệu (Data Entities)

### 7.1 Save schema (`SaveData` v1.0)

| Nhóm | Fields | Mô tả nghiệp vụ |
|------|--------|-----------------|
| Player | health, mana, stamina, posX/Y/Z | Trạng thái combat & vị trí |
| World | currentRegionId, discoveredRegionIds, unlockedTeleportIds, collectedWorldObjectIds | Tiến trình khám phá |
| Quest | activeQuestIds, completedQuestIds, questStepProgress, storyBranch, endingId | Narrative state |
| Inventory | inventory (itemId→count) | Vật phẩm |
| Social | relationships, npcMemories | Affinity & AI memory |
| Meta | gems, coins, partySlotIds, unlockedCharacterIds, equippedWeaponId, weaponLevel, weaponRefinement, ownedWeaponIds, equippedArtifactId, ownedArtifactIds | Progression |
| Gacha/Event | gachaPullsSinceFiveStar, stardust, eventStreakDay, eventClaimedDays | Retention mechanics |

**Đường dẫn:** `Application.persistentDataPath/bes_save.json`

### 7.2 ScriptableObject content

| Asset | Path | Ghi chú |
|-------|------|---------|
| QuestDatabase | `Data/Quests/` + `Resources/Data/` | 4 quests |
| ItemDatabase | `Data/Items/` | **Rỗng — cần Setup** |
| DialogueNode ×3 | `Data/Dialogue/` | Intro guard rỗng |
| HUDSpriteManifest | `Data/UI/` | ~20 sprite slots |
| UIScreenBackgroundManifest | `Data/UI/` | 18 screen BG |
| CharacterPortraitManifest | `Resources/Data/` | 6 portraits |
| WeaponDatabase | Chưa on disk | Tạo bởi `BESUIDataSetup` |
| ArtifactDatabase | Chưa on disk | Tạo bởi setup |
| DefaultGachaBanner | Chưa on disk | Tạo bởi setup |
| DefaultEvent | Chưa on disk | Tạo bởi setup |

### 7.3 Item catalog (theo setup script — chưa populate asset)

| itemId | Loại | Mục đích |
|--------|------|----------|
| `herb_common` | Material | Side quest collect |
| `material_ore` | Material | Weapon enhance / gacha |
| `material_crystal` | Material | Crafting |
| `potion_heal` | Consumable | Heal 30 HP |
| `relic_shard` | Quest | Main quest reward |

---

## 8. Scene & Prefab Hierarchy (tóm tắt)

### 8.1 Scenes

| Scene | Root objects | Prefab instance |
|-------|--------------|-----------------|
| `MainMenu` | UI, Systems, Camera, Light | `MainMenuScreen` |
| `Gameplay` | Systems, UI, Entities, World, Environment, Camera, Light | `GameplayHUD` |
| `PrototypeScene` | Environment, Camera, Systems | — |

### 8.2 Gameplay world tree

```
World
├── Regions: CreationCity | Forest | Ruins
├── Teleports: CityToRuins → Dest_Ruins
├── Entities
│   ├── NPCs: NPC_Guard
│   ├── Enemies: Enemy_Slime
│   └── Bosses: Boss_VoidGuardian
└── Collectibles: Herb_0, Herb_1, Herb_2
```

### 8.3 GameplayHUD prefab (232 objects)

```
GameplayHUD
├── HUDLayer — bars, minimap, quest, skill, party, nav
├── OverlayLayer — inventory, character, map, weapon, artifacts
├── MetaLayer — team, event, wish
└── ModalLayer — dialogue, loading, weapon enhance/rank/refine
```

Chi tiết đầy đủ: file `.tmp_hierarchy.txt` trong project root.

---

## 9. Input & Controls

| Action | Key / Input |
|--------|-------------|
| Move | WASD |
| Look | Mouse |
| Jump | Space |
| Sprint | Left Shift |
| Attack | Left Mouse |
| Skill 1 / 2 | Q / E |
| Dodge | Left Ctrl |
| Interact | F |
| Inventory | Tab / I |
| Character | C |
| World Map | M |
| Weapon | F2 |
| Wish (Gacha) | F3 |
| Team | F4 |
| Event | F5 |
| Artifacts | F6 |
| Quest Log | J |
| Close menu | Esc |

**Input asset:** `Assets/InputSystem_Actions.inputactions`

---

## 10. NPC & Faction Inventory

### 10.1 NPCs

| npcId | Tên | Vai trò | Tính năng |
|-------|-----|---------|-----------|
| `npc_guard` | City Guard Lian | Main quest giver | Story dialogue |
| `npc_mercenary` | Mercenary Kael | Side quest + AI demo | Free chat |
| `npc_priest` | High Priestess Mira | Branch A | Story (design) |

### 10.2 Factions

| Faction | Mục tiêu | Alignment |
|---------|----------|-----------|
| Temple of Creation | Bảo vệ relic | Ally (Branch A) |
| Void Cult | Giải phóng Ma Thần | Antagonist |
| Free Companies | Mercenary explorers | Neutral |

### 10.3 Companions (design — chưa gameplay)

- **Mira** — support/healer
- **Kael** — scout/rogue

---

## 11. Yêu cầu phi chức năng (NFR)

| ID | Yêu cầu | Target | Ghi chú |
|----|---------|--------|---------|
| NFR-PERF-01 | FPS demo | ≥30 FPS @ 1080p iGPU | `PerformanceSettings`, US-066 |
| NFR-PERF-02 | vSync / target FPS | Configurable | `PerformanceSettings` |
| NFR-REL-01 | Save integrity | JSON roundtrip không mất quest/inventory | 5 unit tests |
| NFR-UX-01 | Resolution | 1920×1080 reference | `UICanvasFit` |
| NFR-UX-02 | Input block khi menu | Không di chuyển khi UI mở | `GameplayInputGate` |
| NFR-MAINT-01 | Event-driven decouple | Cross-module qua `GameEvents` | Architecture rule |
| NFR-MAINT-02 | Data-driven content | Quest/item/dialogue qua SO | Không hardcode trong code |
| NFR-SEC-01 | API key LLM | Không commit key; optional on component | `AIDialogueService` |

---

## 12. Trạng thái triển khai & Gap Analysis

### 12.1 Đã hoàn thành (code + scene)

- [x] Bootstrap persistent systems
- [x] Scene flow MainMenu → Gameplay
- [x] Third-person movement, stamina, jump, dodge
- [x] Combat loop (attack, 2 skills, enemy, boss)
- [x] Quest system + objective tracker + branching
- [x] Dialogue system + ending choice
- [x] AI fallback dialogue + memory + affinity
- [x] 3 regions, teleport, collectibles
- [x] Save/load JSON (full meta state)
- [x] HUD 4-layer + 18+ UI screens (composite Figma)
- [x] Gacha, wallet, party, weapon, artifact, event (local)
- [x] Editor pipeline `BES → Setup Project`
- [x] Runtime QA validator

### 12.2 Chưa hoàn thiện / Blocker

| Gap | Impact | Hành động đề xuất |
|-----|--------|-------------------|
| `ItemDatabase.asset` rỗng | Inventory/gacha/reward broken | Chạy **BES → Setup Project** |
| `Node_IntroGuard` rỗng | Main quest step 1 không dialogue | Chạy Setup hoặc populate manual |
| Weapon/Artifact/Gacha SO chưa on disk | Meta screens thiếu data | Chạy Setup |
| `Art/Characters/`, `Art/Environment/` trống | Greybox only | E06 art pipeline |
| LLM HTTP chưa implement | US-047 chưa đạt AC đầy đủ | Phase 2 integration |
| Setup batch exit code 1 | Có thể setup chưa chạy thành công | Re-run Setup trong Unity Editor |
| HUD bar sprites null | HP/stamina/mana dùng solid color | Map sprites trong HUD manifest |
| Test coverage hạn chế | 5 unit tests | Bổ sung playmode/integration tests |
| `EventDefinition` dual persistence | Inconsistent PlayerPrefs fallback | Align với SaveSystem |

### 12.3 Phase 2 roadmap (từ GDD + META_UI_PHASE2)

- Element system & party swap gameplay
- Meta UI chuyển từ full-screen mockup → widget shell
- Live gacha server + wallet sync
- 3D character preview RenderTexture
- Multiplayer, battle pass, monetization
- Full open world map

---

## 13. Acceptance Criteria tổng hợp (E12)

| US | Module | Tiêu chí nghiệm thu |
|----|--------|---------------------|
| US-061 | Gameplay | Walk/sprint/jump/camera ổn định, không rơi map |
| US-062 | Combat | Attack/skill/dodge/boss phase hoạt động đúng |
| US-063 | AI NPC | Interact F, fallback in-character, memory, affinity |
| US-064 | Quest | Main quest auto-start, branch ghi nhận, reward + ending |
| US-065 | Save | JSON save/load restore inventory, quest, position |
| US-068 | Regression | Re-run checklist sau mỗi sprint fix |

**Runtime QA:** Component `OpenWorldSliceValidator` → Context Menu **Run QA Checklist**

---

## 14. Rủi ro & Phụ thuộc

| Rủi ro | Mức | Mitigation |
|--------|-----|------------|
| Content data chưa populate | High | Bắt buộc chạy Setup Project trước demo |
| Art 3D chưa có | Medium | Greybox acceptable cho MVP demo |
| AI LLM cost/latency | Medium | Fallback offline đủ cho MVP |
| Scope creep meta systems | Medium | Tách Phase 2 rõ trong GDD |
| UI mockup vs widget parity | Low | META_UI_PHASE2 plan |

**Phụ thuộc kỹ thuật:** Unity 6 · URP 17 · Input System 1.19 · TextMeshPro 4 · JSON via JsonUtility

---

## 15. Cấu trúc repository (asset)

```
Assets/_Project/
├── Art/UI/          # 830 PNG mockup Figma
├── Data/            # Quest, Dialogue, Items, UI manifests
├── Resources/       # Runtime-loadable copies
├── Scenes/          # MainMenu, Gameplay, PrototypeScene
├── Scripts/         # Core, Gameplay, Narrative, UI, Editor, AI
├── Tests/           # Unit tests (5 classes)
└── UI/Prefabs/      # MainMenuScreen, GameplayHUD, 13 atoms
```

**Thống kê `_Project`:** 128 CS · 19 asset · 15 prefab · 3 scene · 830 PNG

---

## 16. Monetization (GDD — chưa implement)

- Gacha (character/weapon/event banner)
- Battle Pass
- Gói nạp

→ Ngoài phạm vi MVP; chỉ có gacha local demo.

---

*Tài liệu BA này được tổng hợp từ: backlog 70 US, GDD extract, docs/design/*, QA checklist, và toàn bộ codebase + asset hierarchy. Cập nhật lần cuối: 2026-07-10.*
