import re

fbx_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"

with open(fbx_path, 'rb') as f:
    content = f.read()

# Find all LimbNode and Model occurrences
models = re.findall(b'Model::([a-zA-Z0-9_]+)', content)
print("All Model:: names in FBX:")
for m in sorted(list(set([x.decode('utf-8') for x in models]))):
    print(" ", m)
