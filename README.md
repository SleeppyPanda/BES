# BES

BES là dự án Unity kết hợp gameplay 2D/3D, với giao diện trung tâm theo hướng game nhập vai/gacha. Luồng khởi động hiện tại là:

```text
MainMenu -> Loading -> menuhub
```

Tài liệu này mô tả cấu trúc dự án, scene, prefab, dữ liệu và các controller UI đang được sử dụng.

## Môi trường

- Unity: `6000.3.16f1` — Unity 6.3 LTS.
- Render Pipeline: Universal Render Pipeline `17.3.0`.
- Input: Input System `1.19.0`.
- UI: Unity UI/uGUI và TextMesh Pro.
- Navigation: AI Navigation `2.0.12`.
- Scene UI mục tiêu: độ phân giải tham chiếu `1920 x 1080`.

Mở project bằng Unity Hub với đúng phiên bản trên để hạn chế việc Unity tự nâng cấp hoặc ghi lại asset.

## Build Settings và scene flow

Các scene đang được bật trong `ProjectSettings/EditorBuildSettings.asset`, theo đúng thứ tự:

| Index | Scene | Vai trò |
| --- | --- | --- |
| 0 | `Assets/Scenes/MainMenu.unity` | Màn hình khởi động/đăng nhập chính. |
| 1 | `Assets/Scenes/Loading.unity` | Màn hình loading trung gian. |
| 2 | `Assets/Scenes/menuhub.unity` | Menu Hub chứa Story Mode và Play Mode. |
| 3 | `Assets/Scenes/SampleScene.unity` | Scene gameplay/sample đang được giữ lại. |
| 4 | `Assets/Scenes/BES_Island_GameReady.unity` | Scene bản đồ 3D chính. |

Luồng menu được thiết kế:

```text
MainMenu
   |
   v
Loading
   |
   v
menuhub
   |---- Story Mode
   |---- Play Mode
   |---- Modal panels
   `---- Gameplay scenes
```

## Cấu trúc thư mục

```text
BES/
|-- Assets/
|   |-- Art Ui/                 # Ảnh giao diện đang dùng
|   |   |-- Story Mode/
|   |   |-- Play Mode/
|   |   |-- Tranning Mode/
|   |   `-- Mới/
|   |-- Map/                    # Asset/map được di chuyển thủ công
|   |-- Prefabs/                # Ghi chú và prefab cấp dự án
|   |-- Scenes/                 # 5 scene chính và MenuContentDatabase
|   |-- Settings/               # URP/render/input settings
|   |-- UI/
|   |   |-- Scripts/            # Toàn bộ runtime UI code
|   |   `-- Editor/             # Builder và migration UI
|   |-- _Project/
|   |   |-- Art/
|   |   |-- Data/
|   |   |-- Scripts/            # Core/gameplay/world/save/narrative
|   |   `-- UI/Prefabs/         # Prefab UI chính
|   |-- TextMesh Pro/
|   `-- TutorialInfo/
|-- Packages/
|-- ProjectSettings/
|-- UserSettings/
`-- README.md
```

### Quy tắc đối với `Assets/Map`

`Assets/Map` chứa các package môi trường, vegetation, water, VFX, props và các file được di chuyển thủ công trước khi đồng bộ Git.

- Không tự động xóa, đổi tên hoặc di chuyển file trong `Assets/Map`.
- Không chạy cleanup hàng loạt lên thư mục này.
- Không dùng demo scene trong `Assets/Map` làm scene build chính.
- Mọi thay đổi map cần được thực hiện có chủ đích trong Unity Editor.

## Prefab UI

### Prefab chính

| Prefab | Công dụng |
| --- | --- |
| `Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab` | Toàn bộ giao diện Menu Hub. |
| `Assets/_Project/UI/Prefabs/Screens/MainMenuScreen.prefab` | UI Main Menu. |
| `Assets/_Project/UI/Prefabs/Atoms/UIDayCheckInSlot.prefab` | Slot điểm danh. |
| `Assets/_Project/UI/Prefabs/Atoms/UIServerOption.prefab` | Lựa chọn server. |
| `Assets/_Project/UI/Prefabs/Atoms/UISettingsRow.prefab` | Một dòng setting. |

`Assets/Scenes/menuhub.unity` chứa instance của `MenuHub.prefab`. Khi chỉnh layout, ưu tiên chỉnh prefab hoặc mở Prefab Mode để tránh override khó kiểm soát.

## Menu Hub

`MenuHub.prefab` có ba lớp chức năng chính:

```text
MenuHub
|-- HomePanel
|   |-- Profile/currency/fixed navigation
|   |-- StoryModeContent
|   `-- PlayModeContent
|-- StoryModePanel
|   |-- MainStoryPanel
|   `-- CharacterSelectionPanel
|-- PlayModePanel
`-- Modal panels
```

### Home Panel

Phần cố định bên trái:

- Avatar nhân vật hiện tại.
- Tên, ID và level tài khoản.
- Setting.
- Letter.
- Event.
- Inventory/Bag.
- Chat.
- Rank Up và trạng thái sao.
- Ảnh nhân vật 2D hiện tại.

Phần cố định bên phải:

- Energy.
- Gem.
- Coin.
- Mission.
- Battle Pass.
- Cash Shop.

Phần thay đổi bên phải dùng `HomeModeSwitcher`:

- `StoryModeContent`.
- `PlayModeContent`.
- Có thể đổi bằng nút hoặc swipe.
- Hai mode dùng chung viewport/vùng nhìn thấy.

### Modal panels hiện có

- `LetterPanel`
- `EventPanel`
- `InventoryPanel`
- `ChatPanel`
- `RankUpPanel`
- `SettingsPanel`
- `MissionPanel`
- `BattlePassPanel`
- `CashShopPanel`
- `WishPanel`
- `CharacterInfoPanel`
- `GalleryPanel`
- `GatheringValePanel`

Các modal đơn giản sử dụng `SimpleModalPanel`. Panel được mở ngay từ lần bấm đầu tiên và được đưa lên sibling cuối để không bị UI khác che.

## Story Mode

Story Mode hiện chỉ có hai panel:

```text
StoryModePanel
|-- MainStoryPanel
`-- CharacterSelectionPanel
```

`ConfirmedPartyPanel` đã bị loại bỏ hoàn toàn.

### MainStoryPanel

Chứa:

- Background chương.
- Hiệu ứng trên background.
- Currency.
- Back button.
- Thông tin chương và mô tả.
- Bốn `PartySlot_0..3`.
- `StoryRequirement`.
- `ActiveButton`.

### Chọn đội

Controller: `Assets/UI/Scripts/Menu/StoryModePanelController.cs`.

Quy tắc:

- Đội có đúng 4 slot cố định.
- Bấm một party slot sẽ mở `CharacterSelectionPanel`.
- Slot được bấm trở thành slot đích.
- Chọn card nhân vật sẽ gán nhân vật vào đúng slot đích.
- Một nhân vật không thể tồn tại ở hai slot; chọn lại sẽ chuyển nhân vật sang slot mới.
- `EmptyStateImage` chỉ hiện khi slot chưa có nhân vật.
- Portrait, element icon, tên và level chỉ hiện khi slot có nhân vật.

### CharacterSelectionPanel

Danh sách nhân vật:

```text
RosterPanel (ScrollRect)
`-- RosterViewport (RectMask2D)
    `-- RosterContent
        |-- RosterCard_0
        |-- RosterCard_1
        `-- ...
```

- Scroll theo chiều dọc.
- `GridLayoutGroup` cố định 3 cột.
- `ContentSizeFitter` tự tăng chiều cao.
- `RosterCard_0` là card mẫu về hình ảnh và layout.
- Các card còn lại được tạo theo cấu trúc của `RosterCard_0`.

### StoryRequirement

`StoryRequirement` thay cho tên cũ `StoryProgress`.

```text
StoryRequirement
|-- Requirement_0
|   `-- RequirementImage
|-- Requirement_1
|   `-- RequirementImage
|-- Requirement_2
|   `-- RequirementImage
|-- Requirement_3
|   `-- RequirementImage
`-- Requirement_4
    `-- RequirementImage
```

- Mỗi requirement là một điều kiện đội hình bằng hình ảnh.
- `RequirementImage.sprite` lấy từ `StageEntry.partyRequirements[].icon`.
- Requirement có `attributeId` và `minimumCount`.
- Nhân vật có danh sách `CharacterEntry.attributes`.
- So sánh attribute không phân biệt chữ hoa/chữ thường.

Ví dụ:

```text
Character attributes: Fire, Healer
Stage requirements:
- Fire, minimumCount = 2
- Healer, minimumCount = 1
```

### ActiveButton

- Là `Image + Button`, sprite được gán trong Inspector.
- Ẩn hoàn toàn khi điều kiện chưa đủ.
- Chỉ xuất hiện khi:
  - Đội có đúng 4 nhân vật.
  - Mọi `partyRequirements` đều đạt `minimumCount`.
- Khi bấm, controller phát `onPartyConfirmed`.

## Play Mode

`PlayModePanel` là panel chung cho bốn mode:

| Main tab | Content |
| --- | --- |
| `Tab_0_Resonance Sanctum` | `Content_0_Resonance Sanctum` |
| `Tab_1_Sanctum of Lost Echoes` | `Content_1_Sanctum of Lost Echoes` |
| `Tab_2_Rift of the Hunt` | `Content_2_Rift of the Hunt` |
| `Tab_3_Divine Remnant` | `Content_3_Divine Remnant` |

Controller: `PlayModePanelController`.

- Bốn nút ngoài Home mở cùng một panel nhưng chọn đúng main tab.
- Bốn main tab không sử dụng hover.
- Main tab dùng `SelectedState` nếu object này tồn tại.

### Content_0 — Resonance Sanctum

Có bốn subtab:

| Nút | Nội dung |
| --- | --- |
| `SubTab_0` | `TabList_0_Sanctum of Lost Echoes` |
| `SubTab_1` | `TabList_1_Sanctum of Ascension` |
| `SubTab_2` | `TabList_2_Sanctum of Insight` |
| `SubTab_3` | `TabList_3_Sanctum of Forging` |

Controller: `ResonanceSubTabController`.

- Controller tự tìm button/list theo chỉ số tên, không phụ thuộc reference cũ.
- Reference Unity bị xóa được kiểm tra bằng `!= null`, tránh fake-null `NullReferenceException`.
- Mỗi lần chỉ một `TabList` được active.

#### Lost Echoes

- Mỗi achievement có bốn relic/drop item.
- Có nút tiêu thụ energy.
- Các vật phẩm luôn hiển thị alpha `1.0`.
- Trạng thái discovered vẫn có thể lưu trong dữ liệu nhưng không làm mờ ảnh.

#### Ascension và Insight

- Mỗi domain có bốn reward slot.
- Có nút Enter.
- Có energy cost.
- Sprite vật phẩm được gán trong Inspector hoặc từ database.

### Content_2 — Rift of the Hunt

- Các stage card.
- Nút Play.
- Buff panel.
- Timer.
- Layout được để serialized để chỉnh trong Unity.

### Content_3 — Divine Remnant

- Danh sách section quái dạng ngang.
- Có thể kéo trái/phải, không phải đổi tab.
- Mỗi section có ảnh quái và ba drop slot.
- Hai kiểu hiển thị section có thể xen kẽ.
- Controller: `DivineRemnantCarousel`.

## Battle turn-based

`BattlePanel` nằm trong `MenuHub.prefab` và được mở bằng `ActiveButton` của Story sau khi đội hình thỏa requirement.

Cấu trúc chính:

```text
BattlePanel
|-- BattleHeader
|   |-- RoundText
|   |-- CurrentActorText
|   |-- SpeedButton
|   |-- AutoButton
|   `-- PauseButton
|-- TurnOrderRail
|   `-- TurnOrderEntry_0..7
|-- Ally_0..3
|-- Enemy_0..3
|-- SkillPanel
|   `-- SkillButton_0..3
`-- PauseOverlay
```

Controller: `Assets/UI/Scripts/Menu/TurnBattleUI.cs`.

- Có bốn ally và bốn enemy, tùy chỉnh hoàn toàn trong Inspector.
- Mỗi unit có HP, Attack, Defense, Speed, portrait, battlefield sprite, Animator và skill riêng.
- Turn order sắp xếp Speed giảm dần.
- Khi Speed bằng nhau, ally/player được ưu tiên trước enemy.
- Lượt player yêu cầu chọn skill, sau đó chọn enemy mục tiêu.
- Enemy tự chọn ally có tỷ lệ HP thấp nhất.
- Queue bên trái hiển thị thứ tự hành động còn lại trong round.
- `2X` nhân tốc độ delay và Animator của battle lên hai lần.
- Auto tự chọn skill đầu tiên và enemy còn sống đầu tiên.
- Pause dừng battle coroutine và Animator mà không đổi global `Time.timeScale`.
- Battle reset mỗi lần panel được mở cho một nhiệm vụ Story mới.

Dữ liệu mẫu:

| Unit | HP | ATK | DEF | SPD |
| --- | ---: | ---: | ---: | ---: |
| Astra | 130 | 25 | 10 | 18 |
| Blaze | 110 | 32 | 7 | 25 |
| Terra | 180 | 20 | 18 | 10 |
| Zephyr | 95 | 40 | 5 | 30 |
| Wyrmling | 120 | 22 | 8 | 14 |
| Golem | 150 | 25 | 12 | 11 |
| Shade | 90 | 35 | 4 | 23 |
| Titan | 210 | 28 | 16 | 8 |

Các sprite/icon/Animator mẫu đang để trống để gán bằng Unity Inspector.
## Dữ liệu Menu

Asset chính:

```text
Assets/Scenes/MenuContentDatabase.asset
```

Kiểu dữ liệu được định nghĩa trong:

```text
Assets/UI/Scripts/Menu/MenuContentDatabase.cs
```

Các nhóm dữ liệu:

- `CurrencyEntry`
- `CharacterEntry`
- `RewardEntry`
- `PartyAttributeRequirement`
- `StageEntry`
- `StoryChapterEntry`

Trạng thái hiện tại của database có thể chưa có character/story entry. UI roster sẽ chỉ hiển thị card tương ứng với các nhân vật thực sự tồn tại trong `characters`.

### CharacterEntry

Các trường quan trọng:

- `id`
- `displayName`
- `description`
- `portrait`
- `fullBody`
- `chibi`
- `elementIcon`
- `attributes`
- `rarity`
- `level`
- `maxHealth`
- `attack`

### StageEntry

- `id`
- `title`
- `description`
- `preview`
- `energyCost`
- `rewards`
- `partyRequirements`

## Kiến Trúc Codebase & Các Hệ Thống Core

Codebase của dự án **BES** được chia làm 4 module chính chạy trên Unity, đảm bảo sự tách biệt rõ ràng giữa quản lý hệ thống (Core), logic gameplay (Gameplay), cốt truyện/nhiệm vụ/AI (Narrative), và giao diện hiển thị (UI).

### 1. Module Core (`BES.Core`)
Module quản lý vòng đời hệ thống, chuyển cảnh, cấu hình hiệu năng toàn cục và phân phối sự kiện giữa các thành phần khác nhau.

- **[Bootstrapper.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/Bootstrapper.cs)**: Khởi tạo hệ thống tự động trước khi bất kỳ scene nào được load (`RuntimeInitializeOnLoadMethod`). Tạo game object trung tâm `[BES] GameSystems` chứa các service chính như [GameManager.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/GameManager.cs), [SceneLoader.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/SceneLoader.cs), [PartyRoster.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Data/PartyRoster.cs), [PlayerWallet.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Data/PlayerWallet.cs), [MetaProgressState.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Data/MetaProgressState.cs) và [GachaPityState.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Data/GachaPityState.cs).
- **[GameManager.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/GameManager.cs)**: Lớp Singleton điều phối vòng đời dữ liệu game. Quản lý tham chiếu trực tiếp đến các manager cốt lõi ([SaveSystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Save/SaveSystem.cs), [QuestManager.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/QuestManager.cs), [InventorySystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Inventory/InventorySystem.cs), [RelationshipSystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/RelationshipSystem.cs)) và định nghĩa luồng cho game mới (`NewGame`), tiếp tục (`ContinueGame`), và lưu trữ trạng thái (`SaveGame`).
- **[GameEvents.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/GameEvents.cs)**: Sân ga sự kiện trung tâm (Event Bus) của game. Định nghĩa các delegate `event Action` để liên kết các hệ thống tách biệt mà không gây coupling (ví dụ: thay đổi HP/Mana/Stamina, cập nhật nhiệm vụ, thay đổi hảo cảm NPC, kích hoạt/kết thúc hội thoại, nhặt vật phẩm, tiêu diệt quái vật).
- **[SceneLoader.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/SceneLoader.cs)** & **[SceneNames.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/SceneNames.cs)**: Thực hiện tải scene bất đồng bộ thông qua scene trung gian `Loading`. Quản lý hiệu ứng chuyển cảnh mượt mà bằng overlay Canvas fade-in/out (`SceneFadeCanvas`).
- **[PerformanceSettings.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/PerformanceSettings.cs)**: Thiết lập chất lượng đồ họa và giới hạn framerate (FPS target).
- **[RuntimeResourceLoader.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Core/RuntimeResourceLoader.cs)**: Tự động tải trước các tài nguyên nhân vật và asset UI từ thư mục `Resources`.

### 2. Module Gameplay & World (`BES.Gameplay`)
Module kiểm soát vật lý, combat, nhân vật, hệ thống túi đồ, điểm dịch chuyển và cơ chế lưu trữ game.

- **Hệ thống nhân vật & chỉ số**:
  - **[PlayerStats.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/PlayerStats.cs)** & **[StaminaSystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/StaminaSystem.cs)**: Quản lý lượng máu, năng lượng (Mana) và thể lực (Stamina) của người chơi. Stamina tiêu thụ khi chạy nhanh (Dash) hoặc thực hiện né tránh.
  - **[PlayerMotor.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/PlayerMotor.cs)** & **[PlayerInputReader.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/PlayerInputReader.cs)** & **[GameplayInputGate.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/GameplayInputGate.cs)**: Điều khiển chuyển động góc nhìn thứ ba, đọc dữ liệu từ Unity Input System mới và hỗ trợ khóa phím di chuyển khi đang mở menu UI.
  - **[ThirdPersonCamera.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/ThirdPersonCamera.cs)**: Camera bám theo nhân vật chính với khoảng cách và chiều cao tối ưu cho việc quan sát thế giới 3D.
  - **[PartySwapController.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/PartySwapController.cs)** & **[PartyCharacterVisualSwitcher.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/PartyCharacterVisualSwitcher.cs)**: Cơ chế đổi nhanh nhân vật active bằng phím `1–4` (Genshin-style). Tự động hoán đổi và khởi tạo visual model (hoặc capsule màu thay thế nếu thiếu prefab).
  - **[PlayerBuildStats.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/PlayerBuildStats.cs)**: Tính toán tổng hợp chỉ số (HP, ATK, DEF, Crit Rate, Crit DMG) dựa trên cấp độ nhân vật đang chọn, chỉ số vũ khí trang bị và dòng thuộc tính của thánh di vật (Artifact) kèm các hiệu ứng nội tại.
- **Hệ thống Combat (Chiến đấu)**:
  - **[CombatManager.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/CombatManager.cs)** & **[DamageCalculator.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/DamageCalculator.cs)**: Quản lý kích hoạt tấn công, combo đòn đánh, tính toán lượng sát thương thực tế gây ra dựa trên chỉ số công/thủ và chí mạng.
  - **[DodgeController.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/DodgeController.cs)** & **[BasicAttackController.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/BasicAttackController.cs)** & **[SkillController.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/SkillController.cs)**: Xử lý hoạt ảnh né tránh (iframe), combo kiếm thường và kích hoạt skill nhân vật (cooldown/năng lượng tiêu thụ).
  - **[EnemyHealth.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/EnemyHealth.cs)** & **[EnemyHealthBar.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/EnemyHealthBar.cs)** & **[EnemyDamageFeedback.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/EnemyDamageFeedback.cs)** & **[WorldDamagePopup.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/WorldDamagePopup.cs)**: Quản lý máu của quái vật, hiển thị thanh máu trên đầu, nhấp nháy đỏ khi trúng đòn và hiển thị sát thương dạng số bay (floating text).
  - **[BossController.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Combat/BossController.cs)**: Quản lý AI chiến đấu và chuyển tiếp các phase của boss lớn.
- **Hệ thống Thế giới & Sinh quái (World & Spawning)**:
  - **[EnemySpawnRegion.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/World/EnemySpawnRegion.cs)**: Tự động sinh quái vật theo đợt ngẫu nhiên dựa trên phân vùng NavMesh hoặc các điểm spawn cố định, đi kèm cơ chế đếm ngược thời gian hồi sinh (respawn).
  - **[Collectible.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/World/Collectible.cs)**: Vật phẩm có thể nhặt được trên bản đồ thông qua raycast hoặc kích hoạt trigger gần.
  - **[TeleportPoint.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/World/TeleportPoint.cs)** & **[TeleportService.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/World/TeleportService.cs)**: Hệ thống điểm dịch chuyển tức thời, di chuyển tọa độ transform của player.
  - **[WorldIntegrationManager.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/World/WorldIntegrationManager.cs)**: Tích hợp thiết lập vùng bản đồ khi bắt đầu scene, cập nhật tên vùng lên HUD, trao nguyên liệu khởi đầu cho người chơi mới.
  - **[OpenWorldSliceValidator.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/OpenWorldSliceValidator.cs)**: Thực hiện kiểm tra tính hợp lệ của bản đồ mở (QA logging) tại runtime.
- **Hệ thống Lưu trữ (Save System)**:
  - **[SaveSystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Save/SaveSystem.cs)** & **[SaveData.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Save/SaveData.cs)** & **[GameAutoSave.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Save/GameAutoSave.cs)**: Ghi và đọc file JSON (`bes_save.json`) lưu trữ tọa độ người chơi, thanh máu/năng lượng, ví tiền, túi đồ, tiến độ nhiệm vụ, hảo cảm NPC, ký ức AI và trạng thái bảo hiểm gacha (pity). Hỗ trợ mô phỏng đồng bộ hóa đám mây (Cloud Sync) thông qua `PlayerPrefs` theo tài khoản đăng nhập.
- **Hệ thống Túi đồ (Inventory)**:
  - **[InventorySystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Inventory/InventorySystem.cs)**: Quản lý số lượng vật phẩm trong kho.
  - **[ItemDatabase.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Inventory/ItemDatabase.cs)** & **[ItemDefinition.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Gameplay/Inventory/ItemDefinition.cs)**: ScriptableObject định nghĩa thông tin cơ bản, icon và phân loại của các vật phẩm trong game.

### 3. Module Narrative & AI (`BES.Narrative`)
Module xử lý hội thoại cốt truyện tĩnh, hệ thống trò chuyện AI tự do với NPC, quản lý hảo cảm và hệ thống theo dõi tiến trình nhiệm vụ.

- **Hệ thống hội thoại cốt truyện (Static Dialogue)**:
  - **[DialogueSystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/DialogueSystem.cs)** & **[DialogueNode.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/DialogueNode.cs)**: Quản lý luồng đối thoại có sẵn được viết dưới dạng cây phân nhánh (ScriptableObjects). Người chơi đưa ra lựa chọn có thể dẫn tới thay đổi chỉ số hảo cảm, phân nhánh nhiệm vụ hoặc hoàn thành các kết cục (ending) cụ thể.
  - **[NPCInteractable.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/NPCInteractable.cs)**: Thành phần gắn trên NPC để phát hiện người chơi tới gần và kích hoạt hội thoại tĩnh (nếu có cấu hình) hoặc mở giao diện chat AI tự do.
- **Hệ thống Trò chuyện AI tự do (AI NPC Chatbot)**:
  - **[AIDialogueService.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/AIDialogueService.cs)** & **[NPCMemoryStore.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/NPCMemoryStore.cs)**: Sử dụng API OpenAI (`gpt-4o-mini`) để tạo hội thoại động thời gian thực dựa trên tên NPC, trạng thái tình cảm (disposition) và các ký ức đã lưu của người chơi.
  - *Cơ chế Fallback*: Nếu không có API Key, service sẽ tự tạo câu trả lời offline dựa trên ngữ cảnh nhiệm vụ hiện tại, khu vực người chơi đang đứng và ký ức cuối cùng được ghi nhớ.
- **Hệ thống hảo cảm (Affinity System)**:
  - **[RelationshipSystem.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/RelationshipSystem.cs)**: Quản lý điểm hảo cảm từ `-100` đến `100` cho từng NPC. Phân cấp trạng thái quan hệ thành: Trusted (>=50), Friendly (>=20), Cold (<=-20), Hostile (<=-50), và Neutral (bình thường).
- **Hệ thống Nhiệm vụ (Quest System)**:
  - **[QuestManager.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/QuestManager.cs)** & **[QuestDefinition.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/QuestDefinition.cs)** & **[QuestDatabase.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/QuestDatabase.cs)**: Quản lý danh sách nhiệm vụ chính (Main) và nhiệm vụ phụ (Side commission). Quản lý tiến trình qua các bước nhiệm vụ: Reach (Đến điểm), Talk (Trò chuyện), Defeat (Tiêu diệt quái), Collect (Thu thập vật phẩm) và Choice (Phân nhánh).
  - **[QuestObjectiveTracker.cs](file:///c:/Users/Admin/Documents/BES/Assets/_Project/Scripts/Narrative/QuestObjectiveTracker.cs)**: Bộ lắng nghe sự kiện toàn cục để tự động đẩy tiến độ của nhiệm vụ khi người chơi nhặt vật phẩm, tiêu diệt quái vật hoặc nói chuyện với đúng mục tiêu.

### 4. Module UI & Panels (`BES.UI`)
Module hiển thị giao diện, điều hướng màn hình, xử lý đăng nhập, nâng cấp trang bị, hệ thống gacha, và màn chơi Turn-Based giả lập.

- **Khung UI chính (Framework)**:
  - **[UIRootController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/UIRootController.cs)** & **[UINavigationController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/UINavigationController.cs)** & **[UICanvasFit.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/UICanvasFit.cs)**: Đăng ký màn hình, điều hướng tiến lùi giữa các layout canvas và khóa tỷ lệ màn hình chuẩn `1920x1080` bất chấp kích thước cửa sổ thực tế.
  - **[SimpleModalPanel.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/SimpleModalPanel.cs)** & **[UIPanelTransition.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/UIPanelTransition.cs)** & **[HoverSpriteButton.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/HoverSpriteButton.cs)**: Hỗ trợ tạo hoạt ảnh mở/đóng modal, đưa modal lên sibling cuối để tránh bị che khuất và xử lý hiệu ứng hover cho các nút bấm.
  - **[MainMenuController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/MainMenuController.cs)** & **[LoadingScreenUI.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/LoadingScreenUI.cs)**: UI cho màn hình bắt đầu game và màn hình loading tiến trình tải thế giới.
- **Hệ thống Giao diện Menu Hub (`MenuHub`)**:
  - **[MenuNavigator.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/MenuNavigator.cs)** & **[MenuHomeController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/MenuHomeController.cs)**: Điều khiển màn hình trung tâm, kết nối các nút tắt dẫn đến Mail, Cửa hàng, Setting, Sự kiện, Trò chuyện nhóm, Cấp bậc sao.
  - **[HomeModeSwitcher.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/HomeModeSwitcher.cs)**: Xử lý vuốt màn hình (Swipe/Drag) để hoán đổi khu vực làm việc giữa chế độ Story Mode và Play Mode.
  - **[StoryModePanelController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/StoryModePanelController.cs)** & **[StoryPartyController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/StoryPartyController.cs)**: Chọn đội hình 4 nhân vật, kiểm tra thuộc tính nguyên tố để thỏa mãn yêu cầu của màn chơi cốt truyện.
  - **[PlayModePanelController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/PlayModePanelController.cs)** & **[ResonanceSubTabController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/ResonanceSubTabController.cs)** & **[DivineRemnantCarousel.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/DivineRemnantCarousel.cs)**: Hiển thị các hoạt động phụ bản, relic thưởng, danh sách quái vật cuộn ngang trong Divine Remnant.
- **Màn đấu Turn-Based giả lập**:
  - **[TurnBattleUI.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/TurnBattleUI.cs)**: Màn chơi giả lập chiến đấu theo lượt mở ra từ chế độ Story Mode. Tính toán lượt đi dựa trên chỉ số Speed (độ ưu tiên cho phe người chơi nếu bằng nhau), cho phép chọn skill và mục tiêu quái vật, có chế độ Auto-battle và tăng tốc hoạt ảnh `2X`.
- **Hệ thống Đăng nhập / Xác thực (Auth)**:
  - **[AuthManager.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/AuthManager.cs)**: Hệ thống quản lý tài khoản hỗ trợ đăng ký, đăng nhập thông qua Firebase REST API. Tích hợp tính năng khôi phục mật khẩu gửi mã xác thực OTP qua máy chủ SMTP thực tế (`smtp.mailersend.net`). Hỗ trợ chế độ offline (`IsTesting = true`) để duyệt qua luồng đăng nhập nhanh bằng PlayerPrefs.
- **Nâng cấp trang bị & Kho đồ**:
  - **[BagPanelController.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/BagPanelController.cs)** & **[EquipmentUI.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/EquipmentUI.cs)** & **[WeaponScreenUI.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/WeaponScreenUI.cs)**: Giao diện hiển thị danh sách trang bị, nâng cấp cấp độ vũ khí (Enhance), nâng bậc tinh luyện (Refine) và đột phá cấp sao (Rank Up).
  - **[WishUI.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/WishUI.cs)**: Giao diện gacha biểu thị các banner nhân vật/vũ khí, hiển thị kết quả mở thưởng thẻ bài.
- **Hệ thống hội thoại (Dialogue Box)**:
  - **[DialogueUI.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/DialogueUI.cs)** & **[DialogueSequenceUI.cs](file:///c:/Users/Admin/Documents/BES/Assets/UI/Scripts/Menu/DialogueSequenceUI.cs)**: Điều khiển bóng thoại hiển thị văn bản hội thoại cốt truyện tĩnh hoặc khung trò chuyện chat tự do với AI.

> [!IMPORTANT]
> Trước khi sửa hoặc xóa một script thuộc các module trên, cần kiểm tra toàn bộ reference trong bốn scene chính, prefab chính và ScriptableObject để tránh làm mất liên kết dữ liệu trong dự án Unity.

## Ảnh UI

Các nhóm chính:

- `Assets/Art Ui/Story Mode`
- `Assets/Art Ui/Play Mode`
- `Assets/Art Ui/Tranning Mode`
- `Assets/Art Ui/Mới`

Toàn bộ PNG trong `Assets/Art Ui/Mới` đã được cấu hình:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
```

Vì vậy có thể kéo trực tiếp vào `Image -> Source Image`.

Nếu ảnh mới không gán được:

1. Chọn ảnh trong Project.
2. Đặt `Texture Type = Sprite (2D and UI)`.
3. Đặt `Sprite Mode = Single`, trừ khi thật sự dùng sprite sheet.
4. Bấm Apply.

## Editor tool và migration

Các tool nằm trong `Assets/UI/Editor`.

| Tool | Vai trò |
| --- | --- |
| `BESMenuHubBuilder` | Builder ban đầu của MenuHub. |
| `HomeModeSwitcherViewportMigration` | Chuẩn hóa viewport cho swipe. |
| `PlayModePanelMigration` | Tạo/wire PlayModePanel. |
| `PlayModeContentLayoutMigration` | Tạo layout content Play Mode. |
| `PlayModeTabBehaviorMigration` | Ánh xạ subtab và bỏ hover main tab. |
| `ResonanceContentMigration` | Tạo entry Resonance. |
| `StoryModePanelMigration` | Tạo cấu trúc Story cơ bản. |
| `StoryPartySlotEmptyImageMigration` | Đổi empty slot thành Image. |
| `StoryRosterScrollAndProgressMigration` | Tạo scroll grid roster; tên cũ còn giữ để tránh đổi GUID. |
| `StoryRequirementMigration` | Chuyển StoryProgress cũ thành StoryRequirement. |
| `UnusedPanelCleanup` | Xóa modal không còn reference. |

### Lưu ý quan trọng về builder

`MenuHub.prefab` đã được chỉnh thủ công nhiều lần. Không chạy lại builder tổng nếu không có ý định dựng lại toàn bộ prefab.

Ưu tiên:

- Migration nhỏ, có phạm vi rõ ràng.
- Chỉnh trực tiếp component cần thiết.
- Giữ nguyên GUID và reference.
- Backup/commit trước thay đổi hierarchy lớn.

## Quy ước đặt tên

- Main content: `Content_<index>_<name>`.
- Main tab: `Tab_<index>_<name>`.
- Subtab: `SubTab_<index>`.
- Subtab content: `TabList_<index>_<name>`.
- Party slot: `PartySlot_<index>`.
- Character card: `RosterCard_<index>`.
- Requirement: `Requirement_<index>`.
- Vùng ảnh có thể gán: nên dùng tiền tố `Assignable`.

Các controller theo chỉ số dựa vào quy ước này. Nếu đổi tên, phải giữ nguyên phần index hoặc cập nhật controller/migration tương ứng.

## Checklist khi chỉnh MenuHub

1. Mở `MenuHub.prefab` trong Prefab Mode.
2. Không chạy Play Mode khi Unity đang compile.
3. Kiểm tra `EventSystem` và `GraphicRaycaster`.
4. Với button:
   - Kiểm tra `Target Graphic`.
   - Kiểm tra object che raycast.
   - Kiểm tra `CanvasGroup.blocksRaycasts`.
5. Với panel:
   - Chỉ active panel cần hiển thị.
   - Đưa modal lên sibling cuối khi mở.
6. Với tab:
   - Tên index của button phải khớp content.
   - Không để reference tới object đã xóa.
7. Với ảnh:
   - Sprite Mode phải phù hợp.
   - Alpha của `Image.color` phải là `1`.
8. Apply prefab và lưu scene.
9. Chạy từ `MainMenu` để kiểm tra toàn bộ flow.

## Git và file sinh tự động

Không commit các thư mục sinh bởi Unity:

- `Library/`
- `Temp/`
- `Logs/`
- `.vs/`

Nên commit:

- `Assets/` và `.meta`.
- `Packages/manifest.json`.
- `Packages/packages-lock.json`.
- `ProjectSettings/`.
- README và tài liệu dự án.

Mọi asset Unity phải đi kèm file `.meta`; không xóa `.meta` nếu muốn giữ GUID/reference.

## Trạng thái cần lưu ý

- `MenuContentDatabase.asset` cần được điền character, chapter, stage, reward và requirement để UI có dữ liệu thật.
- Sprite và layout phần lớn được thiết kế để gán/chỉnh bằng Inspector.
- Các panel không dùng đã được cleanup theo reference.
- `ConfirmedPartyPanel` không còn tồn tại.
- Main Play Mode tab không dùng hover.
- Resonance subtab tự ánh xạ theo index để tránh chuyển nhầm panel.
- `Assets/Map` là khu vực được bảo toàn, không chỉnh tự động.
