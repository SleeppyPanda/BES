import json
import struct

glb_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\enemy.glb"

with open(glb_path, 'rb') as f:
    header = f.read(12)
    chunk_header = f.read(8)
    chunk_length, chunk_type = struct.unpack('<II', chunk_header)
    json_bytes = f.read(chunk_length)
    data = json.loads(json_bytes.decode('utf-8', errors='ignore'))

nodes = data.get('nodes', [])
for i, n in enumerate(nodes):
    print(f"[{i:2d}] {n.get('name')}: children={n.get('children')}, trans={n.get('translation')}")
