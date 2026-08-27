import os
import re

fbx_path = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx"

print(f"=== CLIPS IN {os.path.basename(fbx_path)} ===")
if not os.path.exists(fbx_path):
    print("  Not found!")
    exit()

with open(fbx_path, 'rb') as f:
    content = f.read()

# Let's search for "Take" or animation names in the FBX.
# In FBX files, AnimationStack or AnimationTake node definitions look like:
# AnimationStack: 1234567, "AnimStack::Walking", "AnimationStack"
# AnimationStack: 1234567, "AnimStack::mixamo.com", "AnimationStack"
# AnimationStack: 1234567, "AnimStack::__preview__Walking", "AnimationStack"
stacks = re.findall(b'AnimationStack:\\s*\\d+,\\s*"AnimStack::([^"]+)"', content)
print("AnimationStack names:")
for s in stacks:
    print(f"  - {s.decode('ascii', errors='ignore')}")

takes = re.findall(b'Take:\\s*"([^"]+)"', content)
print("Take names:")
for t in takes:
    print(f"  - {t.decode('ascii', errors='ignore')}")

# Also just search for any strings matching standard Mixamo or Meshy clip names
strings = re.findall(b'[a-zA-Z0-9_\\.]{4,}', content)
unique_strings = sorted(list(set([s.decode('ascii', errors='ignore') for s in strings])))
for s in unique_strings:
    if 'mixamo' in s.lower() or 'walking' in s.lower() or 'take' in s.lower():
        print(f"  String: {s}")
