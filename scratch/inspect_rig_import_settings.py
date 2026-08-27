import os

meta_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx.meta"
if os.path.exists(meta_path):
    with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    import re
    # We want to search for animationType, avatarSetup, and anything under ModelImporter
    animation_type = re.findall(r'animationType:\s*\d+', content)
    avatar_setup = re.findall(r'avatarSetup:\s*\d+', content)
    print(f"animationType: {animation_type}")
    print(f"avatarSetup: {avatar_setup}")
    
    # Let's print the first 40 lines of ModelImporter settings
    lines = content.split('\n')
    for i in range(min(40, len(lines))):
        print(lines[i])
else:
    print("Meta file not found.")
