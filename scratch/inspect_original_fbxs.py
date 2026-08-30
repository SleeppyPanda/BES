import os
import re

def inspect_fbx(fbx_path):
    print(f"=== CLIPS IN {os.path.basename(fbx_path)} ===")
    if not os.path.exists(fbx_path):
        print("  Not found!")
        return
    with open(fbx_path, 'rb') as f:
        content = f.read()
    # Search for animation clip names or take names
    strings = re.findall(b'[a-zA-Z0-9_\\.]{4,}', content)
    unique_strings = sorted(list(set([s.decode('ascii', errors='ignore') for s in strings])))
    keywords = ['take', 'anim', 'idle', 'walk', 'run', 'die', 'death', 'jump', 'combat', 'pose']
    found = False
    for s in unique_strings:
        if any(kw in s.lower() for kw in keywords):
            print(f"  - {s}")
            found = True
    if not found:
        print("  No clips found by keywords.")

folder = "c:/Users/Admin/Documents/BES/Assets/Model character"
inspect_fbx(os.path.join(folder, "Main3.fbx"))
inspect_fbx(os.path.join(folder, "Chibi2.fbx"))
