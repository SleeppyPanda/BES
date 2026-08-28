import re

fbx_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"

with open(fbx_path, 'rb') as f:
    content = f.read()

# Find all Bone strings in FBX
bone_matches = re.findall(b'Bone_[0-9]+', content)
unique_bones = sorted(list(set([b.decode('utf-8') for b in bone_matches])))
print(f"Total Unique Bones in FBX: {len(unique_bones)}")
print(unique_bones[:30])

# Also check root name: Armature, UniRigArmature, Root, etc.
armature_matches = re.findall(b'[a-zA-Z0-9_]*Armature[a-zA-Z0-9_]*', content)
print("Armature strings:", set([a.decode('utf-8') for a in armature_matches]))
