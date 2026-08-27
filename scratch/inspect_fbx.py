import os
import re

def inspect_fbx(file_path):
    print(f"Inspecting: {os.path.basename(file_path)}")
    if not os.path.exists(file_path):
        print("File not found.")
        return
    
    with open(file_path, 'rb') as f:
        content = f.read()
    
    # In FBX files, animation clips/takes are often represented by "AnimationTake" or "Take:" or similar strings.
    # Let's search for ascii strings matching animation keywords
    # Animation clips are defined under AnimationStack or AnimationLayer or Take name
    # Let's find all occurrences of "AnimationStack" or "AnimationLayer" or Take names
    takes = re.findall(b"Take\\x00\\x00\\x10\\x00[\\x00-\\xff]*?(\\w+)\\x00", content)
    if not takes:
        # Try raw string search for Take names
        takes = re.findall(b"Take:\\s*([^\\r\\n\\x00]+)", content)
    
    # Also find any ascii strings containing words like idle, walk, run, jump, attack
    keywords = [b"idle", b"walk", b"run", b"jump", b"attack", b"die", b"sprint"]
    found_keywords = set()
    for kw in keywords:
        matches = re.findall(b"(?i)" + kw + b"[\\w\\s\\-_]*", content)
        for m in matches:
            try:
                found_keywords.add(m.decode('utf-8').strip())
            except Exception:
                pass
                
    print("Found potential animation/take names:")
    for t in takes:
        try:
            print(f"  - Take: {t.decode('utf-8')}")
        except Exception:
            pass
    print("Found keywords in file:")
    for k in sorted(list(found_keywords))[:15]:
        print(f"  - {k}")
    print("-" * 50)

folder = "c:/Users/Admin/Documents/BES/Assets/Model character"
fbxs = [os.path.join(folder, f) for f in os.listdir(folder) if f.endswith(".fbx")]
for fbx in fbxs:
    inspect_fbx(fbx)
