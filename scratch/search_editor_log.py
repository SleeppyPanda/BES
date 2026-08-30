import os

log_path = "C:/Users/Admin/AppData/Local/Unity/Editor/Editor.log"
if os.path.exists(log_path):
    print("Searching for character setup logs...")
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        for line in f:
            if "[BES Character Setup]" in line or "CharacterModelSetup" in line:
                print(line.strip())
else:
    print("Editor.log not found.")
