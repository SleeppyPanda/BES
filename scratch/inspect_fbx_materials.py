import os
import re

def inspect_materials_and_textures(file_path):
    print(f"=== {os.path.basename(file_path)} ===")
    with open(file_path, 'rb') as f:
        content = f.read()
        
    # Search for Material names
    # Materials are defined under "Material" template or node
    materials = re.findall(b"Material::(\\w+)", content)
    # Search for Texture references (like filename strings ending in png, jpg, tga)
    textures = re.findall(b'([\\w\\s\\-_/]+\\.(?:png|jpg|tga|jpeg|psd|tif|bmp))', content, re.IGNORECASE)
    
    print("Found Materials in FBX:")
    for m in set(materials):
        try:
            print(f"  - {m.decode('utf-8')}")
        except Exception:
            pass
            
    print("Found Texture references in FBX:")
    for t in set(textures):
        try:
            print(f"  - {t.decode('utf-8')}")
        except Exception:
            pass
    print("-" * 50)

folder = "c:/Users/Admin/Documents/BES/Assets/Model character"
inspect_materials_and_textures(os.path.join(folder, "Alina.fbx"))
inspect_materials_and_textures(os.path.join(folder, "Chibi2.fbx"))
inspect_materials_and_textures(os.path.join(folder, "Main3.fbx"))
