import os
import re

folder = "c:/Users/Admin/Documents/BES/Assets"
print("Searching for Material assets and their shaders...")
found = 0
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.mat'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
            shader = re.search(r'm_Shader:\s*({[^}]*})', content)
            if shader:
                print(f"  Material: {file} -> Shader {shader.group(1)}")
                found += 1
                if found > 10:
                    break
    if found > 10:
        break
