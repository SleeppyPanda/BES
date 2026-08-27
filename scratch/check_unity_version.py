import os

path = "c:/Users/Admin/Documents/BES/ProjectSettings/ProjectVersion.txt"
print("Reading Unity project version...")
if os.path.exists(path):
    with open(path, 'r', encoding='utf-8') as f:
        print(f.read().strip())
else:
    print("ProjectVersion.txt not found.")
