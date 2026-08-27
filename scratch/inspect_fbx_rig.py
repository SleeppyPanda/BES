import os

def inspect_fbx_hierarchy(fbx_path):
    print(f"=== HIERARCHY OF {os.path.basename(fbx_path)} ===")
    with open(fbx_path, 'rb') as f:
        content = f.read()
    
    # We want to find nodes in FBX:
    # Model: 123456, "Model::NodeName", "LimbNode" or "Mesh"
    # Connections:
    # C: "OO", parent_id, child_id
    
    # Let's search for Model definitions
    import re
    # Find all Model nodes: Model: ID, "Model::Name", "Type"
    models = re.findall(b';Model::(\\w+), Model::(\\w+)[^\\n]*\\n', content)
    # FBX binary or text might have different formats. Let's do a simple regex for "Model::"
    model_matches = re.findall(b'Model::(\\w+)', content)
    print(f"Total Model nodes found: {len(model_matches)}")
    unique_models = sorted(list(set([m.decode('utf-8', errors='ignore') for m in model_matches])))
    
    print("Model names in FBX:")
    for m in unique_models:
        if any(keyword in m.lower() for keyword in ['armature', 'rig', 'mesh', 'boy', 'body', 'beta', 'joints', 'surface', 'hair', 'xuongtoc']):
            print(f"  - {m}")
            
    # Check for Deformer connections (skinning)
    # Deformer::ID, "Deformer::Name", "Skin"
    deformers = re.findall(b'Deformer::(\\w+)', content)
    print(f"Deformer nodes: {len(deformers)}")
    
    print("-" * 50)

folder = "c:/Users/Admin/Documents/BES/Assets/Model character"
inspect_fbx_hierarchy(os.path.join(folder, "Main3.fbx"))
