import os

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
print("Reading recent [BES Debug Load] runtime logs from Editor.log...")
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    found = False
    for line in lines:
        if '[BES Debug Load]' in line:
            print(line.strip())
            found = True
    if not found:
        print("No [BES Debug Load] logs found in Editor.log.")
else:
    print("Editor.log not found.")
