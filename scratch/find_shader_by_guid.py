import os

guid = "933532a4fcc9baf4fa0491de14d08ed7"
folder = "c:/Users/Admin/Documents/BES/Assets"
print(f"Searching for guid {guid}...")
found = False
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.meta'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
            if guid in content:
                print(f"  Found meta: {path}")
                found = True
if not found:
    print("Not found in Assets meta files.")
