import struct
import json

glb_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\enemy.glb"

try:
    with open(glb_path, 'rb') as f:
        # GLB header: magic (4 bytes), version (4 bytes), length (4 bytes)
        header = f.read(12)
        if len(header) < 12:
            print("GLB too short")
            exit()
        
        magic, version, length = struct.unpack('<III', header)
        if magic != 0x46546C67: # "glTF"
            print("Not a valid GLB file")
            exit()
            
        # First chunk is JSON
        chunk_header = f.read(8)
        if len(chunk_header) < 8:
            print("Chunk header too short")
            exit()
            
        chunk_length, chunk_type = struct.unpack('<II', chunk_header)
        if chunk_type != 0x4E4F534A: # "JSON"
            print("First chunk is not JSON")
            exit()
            
        json_bytes = f.read(chunk_length)
        json_str = json_bytes.decode('utf-8', errors='ignore')
        data = json.loads(json_str)
        
        # Check animations
        animations = data.get('animations', [])
        print(f"Total animations found: {len(animations)}")
        for anim in animations:
            name = anim.get('name', 'unnamed')
            print(f"Animation Name: {name}")
except Exception as e:
    print(f"Error parsing GLB: {e}")
