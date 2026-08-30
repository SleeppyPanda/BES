import os

folder = "c:/Users/Admin/Documents/BES/Assets"
print("Searching for Scene files...")
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.unity'):
            print(f"  - {os.path.join(root, file).replace('\\', '/')}")
