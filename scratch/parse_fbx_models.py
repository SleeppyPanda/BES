import re

fbx_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"

with open(fbx_path, 'rb') as f:
    content = f.read()

# Search for null-terminated or length-prefixed strings like "mixamorig", "Hips", "Spine", "Head", "Leg", "Arm"
patterns = [b'mixamorig:[a-zA-Z0-9_]+', b'Hips', b'Spine[0-9]*', b'Head', b'Left[a-zA-Z0-9_]+', b'Right[a-zA-Z0-9_]+']
for p in patterns:
    matches = re.findall(p, content)
    if matches:
        print(f"Pattern {p}: {set(matches[:10])}")

# Let's also extract all Model:: strings in binary FBX
# In binary FBX, string properties have a 4-byte length prefix
# Let's find occurrences of "Model::"
model_indices = [m.start() for m in re.finditer(b'Model::', content)]
print(f"Total Model:: occurrences: {len(model_indices)}")
model_names = []
for idx in model_indices[:40]:
    # Extract string following Model:: until null byte or non-printable
    sub = content[idx+7:idx+50]
    name = bytearray()
    for b in sub:
        if 32 <= b <= 126 and b != 0:
            name.append(b)
        else:
            break
    model_names.append(name.decode('utf-8', errors='ignore'))
print("Sample Model names:", model_names)
