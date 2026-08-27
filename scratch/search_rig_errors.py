import os

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
print("Searching Editor.log for avatar/humanoid/rig errors...")
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    for line in lines:
        lower = line.lower()
        if 'avatar' in lower or 'humanoid' in lower or 'rig' in lower or 'animation' in lower:
            if 'error' in lower or 'warning' in lower:
                print(line.strip())
else:
    print("Editor.log not found.")
