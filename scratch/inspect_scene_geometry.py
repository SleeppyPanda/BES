import os
import re

scene_path = "c:/Users/Admin/Documents/BES/Assets/Scenes/desert map.unity"
print("Scanning scene geometry...")
if os.path.exists(scene_path):
    with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # We want to find GameObjects and their positions
    # GameObjects start with:
    # --- !u!1 &<id>
    # GameObject:
    #   m_Name: <name>
    # And their transform:
    # --- !u!4 &<id>
    # Transform:
    #   m_LocalPosition: {x: ..., y: ..., z: ...}
    #   m_GameObject: {fileID: <gameobject_id>}
    
    # Let's extract GameObjects
    go_blocks = re.findall(r'--- !u!1 &(\d+)\s+GameObject:.*?m_Name:\s*([^\n\r]+)', content, re.DOTALL)
    go_dict = {go_id: name for go_id, name in go_blocks}
    print(f"Total GameObjects found: {len(go_dict)}")
    
    # Let's extract Transforms and associate with GameObjects
    transforms = re.findall(r'Transform:\s*.*?\s*m_LocalPosition:\s*\{x:\s*([-\d.]+),\s*y:\s*([-\d.]+),\s*z:\s*([-\d.]+)\}.*?m_GameObject:\s*\{fileID:\s*(\d+)\}', content, re.DOTALL)
    
    # Look for interesting object names like "stairs", "bridge", "platform", "wall", "ruin"
    interesting_names = ["bridge", "platform", "stairs", "stair", "ruin", "wall", "gate", "arch"]
    found_objs = []
    for tx, ty, tz, go_id in transforms:
        name = go_dict.get(go_id, "").lower()
        if any(keyword in name for keyword in interesting_names):
            found_objs.append((go_dict[go_id], float(tx), float(ty), float(tz)))
            
    print(f"Found {len(found_objs)} interesting static objects:")
    for name, x, y, z in found_objs[:50]:
        print(f"  Name: '{name}' -> Position: ({x:.2f}, {y:.2f}, {z:.2f})")
else:
    print("Scene not found.")
