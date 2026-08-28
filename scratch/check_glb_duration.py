import struct
import json

glb_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\enemy.glb"

try:
    with open(glb_path, 'rb') as f:
        header = f.read(12)
        magic, version, length = struct.unpack('<III', header)
        chunk_header = f.read(8)
        chunk_length, chunk_type = struct.unpack('<II', chunk_header)
        json_bytes = f.read(chunk_length)
        json_str = json_bytes.decode('utf-8', errors='ignore')
        data = json.loads(json_str)
        
        animations = data.get('animations', [])
        print(f"Total animations: {len(animations)}")
        for anim in animations:
            name = anim.get('name', 'unnamed')
            print(f"Animation: {name}")
            channels = anim.get('channels', [])
            samplers = anim.get('samplers', [])
            print(f"  Channels: {len(channels)}, Samplers: {len(samplers)}")
            
            # Find max time in accessors
            accessors = data.get('accessors', [])
            max_time = 0.0
            for sampler in samplers:
                input_accessor_idx = sampler.get('input')
                if input_accessor_idx is not None and input_accessor_idx < len(accessors):
                    acc = accessors[input_accessor_idx]
                    acc_max = acc.get('max', [0.0])
                    if acc_max and acc_max[0] > max_time:
                        max_time = acc_max[0]
            print(f"  Duration: {max_time} seconds")
except Exception as e:
    print(f"Error: {e}")
