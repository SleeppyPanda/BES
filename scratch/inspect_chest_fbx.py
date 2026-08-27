import os

fbx_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture_fbx/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture.fbx"
print("Reading chest FBX content...")
if os.path.exists(fbx_path):
    # Let's search for Take or Animation names in the FBX file
    with open(fbx_path, 'rb') as f:
        content = f.read()
    
    # FBX files contain animation take names like "Take 001" or custom animations
    # We can do a simple string search in the binary file
    import re
    takes = re.findall(b'CurrentTakeName:\\s*"([^"]+)"', content)
    takes_2 = re.findall(b'TakeName:\\s*"([^"]+)"', content)
    print(f"Takes: {takes}")
    print(f"TakeNames: {takes_2}")
    
    # Print any animation stack names
    stacks = re.findall(b'AnimationStack::\\s*([^\\0\\n\\r]+)', content)
    print(f"Animation Stacks: {stacks}")
else:
    print("FBX file not found.")
