import os
import re

def list_takes(file_path):
    print(f"=== {os.path.basename(file_path)} ===")
    with open(file_path, 'rb') as f:
        content = f.read()
    
    # In binary FBX, takes are defined as "AnimationStack" or "AnimationLayer" objects
    # Let's search for the name pattern of AnimStack and AnimLayer
    # Typically: "AnimStack\x00\x00..." followed by take name
    # Let's search for "AnimStack" or "AnimationStack"
    # Find all strings that are 4 to 30 characters long and contain printable characters
    strings = re.findall(b'[\x20-\x7E]{4,30}', content)
    takes = []
    for s in strings:
        s_str = s.decode('utf-8')
        if 'take' in s_str.lower() or 'mixamo' in s_str.lower() or 'anim' in s_str.lower() or 'idle' in s_str.lower() or 'walk' in s_str.lower() or 'run' in s_str.lower() or 'jump' in s_str.lower():
            takes.append(s_str)
            
    # Print unique matching strings
    seen = set()
    for t in takes:
        if t not in seen:
            print(f"  - {t}")
            seen.add(t)

list_takes("c:/Users/Admin/Documents/BES/Assets/Model character/Alina.fbx")
list_takes("c:/Users/Admin/Documents/BES/Assets/Model character/Chibi2.fbx")
list_takes("c:/Users/Admin/Documents/BES/Assets/Model character/Main3.fbx")
