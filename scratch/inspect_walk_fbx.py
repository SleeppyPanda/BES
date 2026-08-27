import os
import re

path = r"Assets\MeshyImports\Model quái con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"
if not os.path.exists(path):
    for r, d, fs in os.walk("Assets/MeshyImports"):
        if "quái con" in r or "quai" in r.lower():
            path = os.path.join(r, "Meshy_AI_Animation_Walking_frame_rate_60.fbx")
            break

print("Checking walk FBX at:", path)
print("File size:", os.path.getsize(path))

with open(path, "rb") as f:
    c = f.read()

# Let's search for AnimationCurve, AnimStack, take names, etc.
takes = re.findall(b'TakeName:\\s*\"([^\"]+)\"', c)
if not takes:
    takes = re.findall(b'TakeName\\x00\\x00\\x08\\x00\\x00\\x00([a-zA-Z0-9_]+)', c)
print("FBX Takes found:", [t.decode(errors="ignore") for t in takes])

# Let's search for AnimationStack nodes
stacks = re.findall(b'AnimationStack', c)
print("AnimationStack occurrences:", len(stacks))

# Print all human-readable strings longer than 5 chars containing 'walk' or 'anim'
strings = re.findall(b'[a-zA-Z0-9_\\-\\s]{5,}', c)
walk_strings = [s.decode(errors="ignore") for s in strings if b'walk' in s.lower() or b'anim' in s.lower() or b'take' in s.lower()]
print("Walk/Anim/Take strings:", list(set(walk_strings))[:20])
