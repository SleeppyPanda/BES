import os
import re

db_path = "c:/Users/Admin/Documents/BES/Assets/Resources/Data/ItemDatabase.asset"
print("Reading ItemDatabase.asset...")
if os.path.exists(db_path):
    with open(db_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # Find item definition blocks
    # We can match:
    # - itemId: ...
    # - displayName: ...
    item_ids = re.findall(r'itemId:\s*([^\n\r]+)', content)
    display_names = re.findall(r'displayName:\s*([^\n\r]+)', content)
    
    print(f"Total items found: {len(item_ids)}")
    for i in range(min(len(item_ids), len(display_names))):
        print(f"  ID: '{item_ids[i].strip()}' -> Name: '{display_names[i].strip()}'")
else:
    print("ItemDatabase.asset not found.")
