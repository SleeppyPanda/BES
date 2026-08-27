import os
import re

fbx_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture_fbx/Meshy_AI_Arcane_Moonlit_Sigil_0827210455_texture.fbx"
print("Scanning FBX structure...")
if os.path.exists(fbx_path):
    with open(fbx_path, 'rb') as f:
        data = f.read()
    
    # In FBX files (binary or text), object names usually appear as null-terminated strings
    # or ASCII names. Let's find all occurrences of strings matching mesh/model names.
    # ASCII characters between 3 and 60 chars long containing letters/numbers
    strings = re.findall(b'[a-zA-Z_0-9\-:]{3,60}', data)
    unique_strings = sorted(list(set(strings)))
    
    # Filter for names that look like node names, meshes, or sub-objects
    interesting = []
    for s in unique_strings:
        try:
            decoded = s.decode('ascii').lower()
            if any(kw in decoded for kw in ["mesh", "sigil", "model", "geom", "node", "wind", "current", "column", "pad", "base"]):
                interesting.append(s.decode('ascii'))
        except Exception:
            pass
            
    print(f"Total interesting strings: {len(interesting)}")
    for item in interesting[:100]:
        print(f"  {item}")
else:
    print("FBX not found.")
