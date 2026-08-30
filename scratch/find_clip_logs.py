import os

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
print("Reading Unity Editor.log for ListFBXClips output...")
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    for line in lines:
        if '=== CLIPS IN' in line or 'Clip:' in line:
            print(line.strip())
else:
    print("Editor.log not found.")
