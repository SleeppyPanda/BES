import os

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
print("Searching entire Editor.log for BES logs...")
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    import re
    matches = re.findall(r'(\[BES Debug[^\]]*\].*?\n)', content)
    print(f"Found {len(matches)} matches:")
    # Print last 50 matches
    for m in matches[-50:]:
        print(m.strip())
else:
    print("Editor.log not found.")
