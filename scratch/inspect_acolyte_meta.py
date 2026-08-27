import os

folder = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped"
for file in os.listdir(folder):
    if file.endswith('.meta'):
        path = os.path.join(folder, file)
        with open(path, 'r', errors='ignore') as f:
            for line in f:
                if 'guid:' in line:
                    print(f"{file} -> {line.strip()}")
