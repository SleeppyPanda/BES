import os
import re

def find_clips(fbx_path):
    print(f"=== CLIPS IN {os.path.basename(fbx_path)} ===")
    with open(fbx_path, 'rb') as f:
        content = f.read()
    strings = re.findall(b'[a-zA-Z0-9_\\.]{4,}', content)
    unique_strings = sorted(list(set([s.decode('ascii', errors='ignore') for s in strings])))
    
    keywords = ['take', 'anim', 'idle', 'walk', 'run', 'die', 'death']
    for s in unique_strings:
        if any(kw in s.lower() for kw in keywords):
            print(f"  - {s}")

folder = "c:/Users/Admin/Documents/BES/Assets/Model character"
find_clips(os.path.join(folder, "Alina.fbx"))
