import os
import re

print("Scanning for ItemDefinition assets...")
assets_dir = "c:/Users/Admin/Documents/BES/Assets"
count = 0
for root, dirs, files in os.walk(assets_dir):
    for file in files:
        if file.endswith(".asset"):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
            if "Assembly-CSharp::BES.Gameplay.ItemDefinition" in content or "m_Script: {fileID: 11500000, guid: " in content:
                # This might be an ItemDefinition
                # Let's extract itemId and displayName
                match_id = re.search(r'itemId:\s*([^\n\r]+)', content)
                match_name = re.search(r'displayName:\s*([^\n\r]+)', content)
                if match_id:
                    item_id = match_id.group(1).strip()
                    display_name = match_name.group(1).strip() if match_name else "Unknown"
                    print(f"Asset: '{file}' -> ID: '{item_id}' -> Name: '{display_name}'")
                    count += 1

print(f"Total ItemDefinition files inspected: {count}")
