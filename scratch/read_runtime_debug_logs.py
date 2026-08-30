import os

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
print("Reading recent [BES Debug] runtime logs from Editor.log...")
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    # Get last 1000 lines
    recent_lines = lines[-1000:]
    found = False
    for line in recent_lines:
        if '[BES Debug]' in line:
            print(line.strip())
            found = True
    if not found:
        print("No [BES Debug] logs found in the last 1000 lines of Editor.log.")
else:
    print("Editor.log not found.")
