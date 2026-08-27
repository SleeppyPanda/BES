import os
import struct
import json

path = r"Assets\MeshyImports\Model quái con\enemy.glb"
if not os.path.exists(path):
    for r, d, fs in os.walk("Assets/MeshyImports"):
        if "quái con" in r or "quai" in r.lower():
            path = os.path.join(r, "enemy.glb")
            break

print("Checking GLB at:", path)
with open(path, "rb") as f:
    header = f.read(12)
    if len(header) < 12:
        print("Header too short")
        exit()
    magic, version, length = struct.unpack("<III", header)
    if magic == 0x46546C67:
        chunk_header = f.read(8)
        if len(chunk_header) < 8:
            print("Chunk header too short")
            exit()
        chunk_len, chunk_type = struct.unpack("<II", chunk_header)
        if chunk_type == 0x4E4F534A:
            chunk_data = f.read(chunk_len)
            data = json.loads(chunk_data.decode("utf-8"))
            if "animations" in data:
                print("Animations in enemy.glb:")
                for i, anim in enumerate(data["animations"]):
                    print(f"  - Index {i}: name={anim.get('name')}")
            else:
                print("No animations found in JSON chunk.")
        else:
            print("First chunk is not JSON")
    else:
        print("Not a valid GLB")
