import os

scenes_dir = "c:/Users/Admin/Documents/BES/Assets/Scenes"
print("Scanning scenes for HUDController/GameplayHudLayout...")
for file in os.listdir(scenes_dir):
    if file.endswith(".unity"):
        path = os.path.join(scenes_dir, file)
        with open(path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        hud_count = content.count("HUDController")
        layout_count = content.count("GameplayHudLayout")
        if hud_count > 0 or layout_count > 0:
            print(f"Scene '{file}': contains HUDController {hud_count} times, GameplayHudLayout {layout_count} times.")
        else:
            print(f"Scene '{file}': No HUD components found.")
