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

## Runtime UI scripts

### `Assets/UI/Scripts/Menu`

| Script | Vai trò |
| --- | --- |
| `MenuNavigator` | Chuyển giữa Home, Story và các screen cấp cao. |
| `MenuHomeController` | Gắn button Home với modal/mode/action. |
| `HomeModeSwitcher` | Swipe/chuyển Story Mode và Play Mode. |
| `SimpleModalPanel` | Mở/đóng modal. |
| `PlayModePanelController` | Quản lý bốn main tab Play Mode. |
| `PlayModeLaunchButton` | Mở Play Mode đúng tab từ Home. |
| `ResonanceSubTabController` | Quản lý bốn subtab Resonance. |
| `StoryModePanelController` | Chọn đội, requirement và ActiveButton. |
| `HoverSpriteButton` | Sprite/text hover cho button được phép hover. |
| `DivineRemnantCarousel` | Scroll ngang Divine Remnant. |
| `LostEchoAchievementEntry` | Entry relic Lost Echoes. |
| `DiscoverableRelicSlot` | Slot relic; hiện không giảm alpha. |
| `SanctumDomainEntry` | Entry domain Ascension/Insight. |
| `RiftStageCardView` | Stage card Rift. |
| `DialogueSequenceUI` | Chuỗi hội thoại UI. |
| `TurnBattleUI` | UI battle theo lượt. |
| `UIPanelTransition` | Hiệu ứng alpha panel. |

### `Assets/UI/Scripts/Data`

Chứa database và runtime state:

- Character.
- Artifact.
- Weapon.
- Party.
- Wallet.
- Gacha.
- HUD sprite/background manifest.

### `Assets/UI/Scripts/Core`

Chứa:

- Layout token.
- Anchor preset.
- Theme.
- Primitive style.
- Screen registry.
- Các widget HUD dùng chung.

### Các UI gameplay khác

`Assets/UI/Scripts` còn chứa UI cho:

- Inventory.
- Equipment.
- Weapon upgrade/refine/rank up.
- Gacha/Wish.
- Battle Pass.
- Quest.
- Map/Minimap.
- Dialogue.
- Team setup.
- Character profile.
- HUD và skill bar.
- Main Menu và Loading.

## Gameplay và core code

`Assets/_Project/Scripts` được chia thành:

```text
AI/
Core/
Editor/
Gameplay/
|-- Combat/
|-- Inventory/
|-- Save/
`-- World/
Narrative/
```

Đây là code ngoài UI cho combat, inventory, save, world và narrative. Trước khi xóa một script, cần kiểm tra reference trong bốn scene chính, prefab chính và ScriptableObject.

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
