import os

meta_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx.meta"
print("Reading Walking_frame_rate_60.fbx.meta...")
if os.path.exists(meta_path):
    with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # We want to see clipAnimations section or loopTime
    import re
    clips = re.findall(r'clipAnimations:.*?(?=\n\s*[^-\s]|\Z)', content, re.DOTALL)
    if clips:
        print("ClipAnimations section:")
        print(clips[0])
    else:
        print("clipAnimations section not found or empty.")
        # Search for any loopTime inside the file
        loop_times = re.findall(r'loopTime:\s*\d+', content)
        print(f"Loop times found: {loop_times}")
else:
    print("Meta file not found.")
