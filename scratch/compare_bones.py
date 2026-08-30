import json
import struct
import re

# 1. Inspect GLB bones
glb_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\enemy.glb"
fbx_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"

print("--- GLB NODES & BONES ---")
try:
    with open(glb_path, 'rb') as f:
        header = f.read(12)
        chunk_header = f.read(8)
        chunk_length, chunk_type = struct.unpack('<II', chunk_header)
        json_bytes = f.read(chunk_length)
        data = json.loads(json_bytes.decode('utf-8', errors='ignore'))
        
        nodes = data.get('nodes', [])
        print(f"Total GLB nodes: {len(nodes)}")
        for idx, node in enumerate(nodes[:25]):
            print(f"  [{idx}] Name: {node.get('name')}, Children: {node.get('children')}")
        
        # Check animations channel target node paths/indices
        anims = data.get('animations', [])
        for a in anims:
            print(f"Animation '{a.get('name')}' targets:")
            for ch in a.get('channels', [])[:10]:
                target = ch.get('target', {})
                node_idx = target.get('node')
                path = target.get('path')
                node_name = nodes[node_idx].get('name') if node_idx is not None and node_idx < len(nodes) else 'Unknown'
                print(f"    Node [{node_idx}] ({node_name}) -> {path}")
except Exception as e:
    print(f"Error GLB: {e}")

print("\n--- FBX BONES & MODEL ---")
try:
    with open(fbx_path, 'rb') as f:
        content = f.read()
    
    # Find Model definitions in FBX (Model: 12345, "Model::BoneName", "LimbNode")
    models = re.findall(b'Model:\\s*\\d+,\\s*"Model::([^"]+)",\\s*"([^"]+)"', content)
    print(f"Total FBX models/nodes: {len(models)}")
    for name, mtype in models[:25]:
        print(f"  Name: {name.decode('utf-8', errors='ignore')}, Type: {mtype.decode('utf-8', errors='ignore')}")
except Exception as e:
    print(f"Error FBX: {e}")
