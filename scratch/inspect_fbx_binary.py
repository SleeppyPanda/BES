import os
import re

def inspect_fbx_binary(fbx_path):
    print(f"=== BINARY STRINGS OF {os.path.basename(fbx_path)} ===")
    with open(fbx_path, 'rb') as f:
        content = f.read()
        
    # Find all printable ASCII sequences of length 4 or more
    strings = re.findall(b'[a-zA-Z0-9_\\.]{4,}', content)
    
    unique_strings = sorted(list(set([s.decode('ascii', errors='ignore') for s in strings])))
    
    print(f"Total ASCII strings: {len(unique_strings)}")
    
    keywords = ['armature', 'rig', 'mesh', 'boy', 'body', 'beta', 'joints', 'surface', 'hair', 'xuongtoc', 'camera', 'light']
    for s in unique_strings:
        s_lower = s.lower()
        if any(kw in s_lower for kw in keywords):
            # Print string if it matches keywords
            print(f"  - {s}")
            
    print("-" * 50)

folder = "c:/Users/Admin/Documents/BES/Assets/Model character"
inspect_fbx_binary(os.path.join(folder, "Main3.fbx"))
