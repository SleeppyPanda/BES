import os

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
print("Reading last 500 lines of Editor.log for warnings/errors...")
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    for line in lines[-500:]:
        lower = line.lower()
        if 'warning' in lower or 'error' in lower or 'exception' in lower:
            print(line.strip())
else:
    print("Editor.log not found.")
