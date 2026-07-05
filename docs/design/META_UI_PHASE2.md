# Meta UI Phase 2 — Real Screens (Không Full-Screen Mockup)

Sau khi Gameplay HUD dùng widget runtime thật (Phase 1), phase này chuyển các meta screen sang UI ghép từ component, giống loop Genshin/open-world RPG.

## Nguyên tắc

- **Background art** chỉ làm theme (gradient, pattern, vignette) — không phải toàn bộ layout.
- Mỗi screen = prefab với **header, tabs, grid, detail panel, footer** là widget độc lập.
- `UIScreenBackgroundBootstrap` chỉ bind overlay/meta/modal — không đụng `HUDLayer`.
- Một meta screen mở tại một thời điểm; `GameplayInputGate` chặn input khi mở.

## Màn hình & thành phần cần build

### Wish (Gacha)
| Widget | Mô tả |
|--------|--------|
| `WishBannerHeader` | Tên banner, timer, pity counter |
| `WishCurrencyBar` | Gems, stardust |
| `WishPullButtons` | x1 / x10 với cost label |
| `WishResultOverlay` | Card reveal animation slot (prefab) |
| `WishHistoryRow` | Optional — log pull |

**Bỏ:** `CreateFullScreenPanel(..., BgWish)` làm toàn layout.

### Team
| Widget | Mô tả |
|--------|--------|
| `TeamSlotGrid` | 4 slot active party (reuse `PartyStripUI` style) |
| `CharacterListScroll` | Roster unlocked/locked |
| `CharacterDetailPanel` | Level, element, stats preview |
| `TeamConfirmBar` | Apply / Close |

**Bỏ:** full `BgTeam` mockup; giữ art làm nền mờ 30–40% opacity.

### Weapon
| Widget | Mô tả |
|--------|--------|
| `WeaponGrid` | Icon + rarity frame |
| `WeaponDetailPanel` | ATK, substat, refine |
| `WeaponRankUpUI` | Đã có — gắn vào panel thật |
| `WeaponEquipButton` | Gắn cho active character |

### Artifact (Inventory subtype)
| Widget | Mô tả |
|--------|--------|
| `ArtifactSetTabs` | Filter theo set |
| `ArtifactGrid` | 5-slot layout preview |
| `ArtifactDetailStats` | Main stat + substats |

### Inventory
| Widget | Mô tả |
|--------|--------|
| `InventoryTabBar` | Items / Materials / Quest |
| `ItemGrid` | `InventoryUI` grid thật (đã có skeleton) |
| `ItemTooltip` | Hover / select detail |
| `InventorySortBar` | Rarity, type |

**Bỏ:** `BgInventory` full-screen; thay bằng `InventoryShell` prefab.

### Dialogue
| Widget | Mô tả |
|--------|--------|
| `DialoguePanel` | Box + portrait frame |
| `DialogueText` | Typewriter optional |
| `DialogueChoices` | Branch buttons |
| `DialogueContinue` | Đã có |

## Thứ tự triển khai đề xuất

1. **InventoryShell** — ít phụ thuộc, grid đã có.
2. **TeamShell** — dùng lại `PartyRoster`, `CharacterPortraitManifest`.
3. **WishShell** — kết nối `GachaPityState`, banner SO.
4. **WeaponShell** — nối `WeaponDatabase`, rank-up.
5. **ArtifactShell** — mở rộng từ inventory tabs.
6. **Dialogue polish** — tách khỏi mockup nếu còn.

## Editor / pipeline

- `BESUIPrefabBuilder`: thêm `BuildMetaScreenShell(screenId)` thay `CreateFullScreenPanel` cho từng meta.
- `BESUIScreenBackgroundSetup`: manifest chỉ list **theme backgrounds**, không map HUD sprites.
- `BES → Setup Project`: rebuild meta prefabs sau mỗi screen.

## QA checklist (meta)

- [ ] Không có `BgWish` / `BgTeam` / `BgInventory` stretch che toàn màn khi test widget-only.
- [ ] Mở Wish đóng Inventory; nav exclusive qua `UINavigationController`.
- [ ] ESC / Close trả về gameplay, `GameplayInputGate` unblock.
- [ ] 1920×1080: grid không crop; scroll khi > N items.
- [ ] Portrait / icon từ manifest whitelist, không `Group 427*`.

## Files dự kiến chạm

- `Assets/_Project/Scripts/Editor/BESUIPrefabBuilder.cs`
- `Assets/_Project/Scripts/UI/UINavigationController.cs`
- `Assets/_Project/Scripts/UI/InventoryUI.cs`, `WishUI.cs`, `TeamUI.cs`, …
- `Assets/_Project/Scripts/UI/UIScreenBackgroundBootstrap.cs`
- `Assets/_Project/UI/Prefabs/Screens/*.prefab`
