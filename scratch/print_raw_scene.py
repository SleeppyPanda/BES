import os

scene_path = "c:/Users/Admin/Documents/BES/Assets/Scenes/desert map.unity"
if os.path.exists(scene_path):
    with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
        for i in range(50):
            print(f.readline().strip())
else:
    print("Scene not found.")
