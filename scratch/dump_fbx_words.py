import re

fbx_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"

with open(fbx_path, 'rb') as f:
    content = f.read()

# Find strings of alphanumeric characters length 3 to 40
words = re.findall(b'[A-Za-z0-9_]{3,40}', content)
# Filter unique words
unique_words = sorted(list(set([w.decode('utf-8') for w in words])))
print(f"Total unique identifiers in FBX: {len(unique_words)}")

# Look for bone names / limb names / node names
bone_like = [w for w in unique_words if any(k in w.lower() for k in ['hip', 'spine', 'head', 'arm', 'leg', 'foot', 'hand', 'toe', 'tail', 'jaw', 'neck', 'bone', 'root', 'mesh'])]
print("Bone-like identifiers in FBX:", bone_like[:50])
