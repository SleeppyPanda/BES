import os

log_path = os.path.expandvars(r'%LOCALAPPDATA%\Unity\Editor\Editor.log')
print("Inspecting rig import logs...")
if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # Find all occurrences of "Rig Error" and show context
    import re
    matches = [m.start() for m in re.finditer(r'Rig Error:', content)]
    for idx, pos in enumerate(matches):
        start = max(0, pos - 300)
        end = min(len(content), pos + 500)
        print(f"--- MATCH {idx+1} ---")
        print(content[start:end])
        print("---------------------\n")
else:
    print("Editor.log not found.")
