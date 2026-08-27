import os

folder = "c:/Users/Admin/Documents/BES/Assets"
print("Searching for Player prefab...")
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.prefab') and 'player' in file.lower():
            print(f"  - {os.path.join(root, file).replace('\\', '/')}")
