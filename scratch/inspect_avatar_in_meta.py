import os

meta_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx.meta"
print("Searching meta file for rig/avatar/human details...")
if os.path.exists(meta_path):
    with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    import re
    # Look for humanDescription, avatarSetup, or any bone mapping
    hd = re.findall(r'humanDescription:.*?(?=\n\s*[^-\s]|\Z)', content, re.DOTALL)
    if hd:
        print("Found humanDescription in meta!")
        # Print first 20 lines of it
        print('\n'.join(hd[0].split('\n')[:20]))
    else:
        print("humanDescription NOT found in meta!")
        
    # Search for bone mapping
    bm = re.findall(r'human:\s*\[.*?\]', content, re.DOTALL)
    print(f"Found human bone mapping tokens: {len(bm)}")
else:
    print("Meta file not found.")
