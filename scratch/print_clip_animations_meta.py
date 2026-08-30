import os

meta_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx.meta"
if os.path.exists(meta_path):
    with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    printing = False
    count = 0
    for line in lines:
        if 'clipAnimations:' in line:
            printing = True
        if printing:
            print(line.strip())
            count += 1
            if count > 50:
                break
else:
    print("Meta file not found.")
