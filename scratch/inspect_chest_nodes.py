import os

fbx_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture_fbx/Meshy_AI_Jeweled_Treasure_Ches_0827194829_texture.fbx"
print("Reading chest nodes...")
if os.path.exists(fbx_path):
    with open(fbx_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    import re
    # Look for Model nodes:
    # Model: "Model::Lid", "Mesh"
    models = re.findall(r'Model:\s*"\w+::([^"]+)"', content)
    print(f"Found model nodes: {set(models)}")
else:
    print("FBX not found.")
