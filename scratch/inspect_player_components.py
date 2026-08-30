import os
import re

folder = "c:/Users/Admin/Documents/BES/Assets/_Project/Prefabs"
print("Reading serialized speeds in player prefabs...")
for file in os.listdir(folder):
    if file.endswith('.prefab') and 'player_' in file.lower():
        path = os.path.join(folder, file)
        print(f"=== {file} ===")
        with open(path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        
        # Search for walkSpeed and sprintSpeed in YAML
        walk_speed = re.findall(r'walkSpeed:\s*([^\n\r]+)', content)
        sprint_speed = re.findall(r'sprintSpeed:\s*([^\n\r]+)', content)
        print(f"  walkSpeed: {walk_speed}")
        print(f"  sprintSpeed: {sprint_speed}")
