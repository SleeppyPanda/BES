import os

folder = "c:/Users/Admin/Documents/BES/Assets"
print("Searching for FBX/Anim assets with 'idle' or 'stand'...")
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.fbx') or file.endswith('.anim'):
            lower = file.lower()
            if 'idle' in lower or 'stand' in lower:
                print(f"  - {os.path.join(root, file).replace('\\', '/')}")
