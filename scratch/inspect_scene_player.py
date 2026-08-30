import os
import re

scene_path = "c:/Users/Admin/Documents/BES/Assets/Scenes/desert map.unity"
print(f"Reading serialized Player components in {os.path.basename(scene_path)}...")
if os.path.exists(scene_path):
    with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # Let's search for walkSpeed or sprintSpeed in the scene YAML
    walk_speed = re.findall(r'walkSpeed:\s*([^\n\r]+)', content)
    sprint_speed = re.findall(r'sprintSpeed:\s*([^\n\r]+)', content)
    print(f"  walkSpeed: {walk_speed}")
    print(f"  sprintSpeed: {sprint_speed}")
else:
    print("Scene file not found.")
