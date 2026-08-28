import struct

def parse_fbx_binary(filepath):
    with open(filepath, 'rb') as f:
        data = f.read()
    
    # Check header
    if data[:20] != b'Kaydara FBX Binary  ':
        print("Not FBX binary")
        return
    
    pos = 27
    nodes = []
    
    def read_node(offset):
        if offset + 13 > len(data):
            return None, len(data)
        
        end_offset, num_props, prop_list_len, name_len = struct.unpack('<III B', data[offset:offset+13])
        if end_offset == 0:
            return None, offset + 13
        
        name = data[offset+13:offset+13+name_len].decode('utf-8', errors='ignore')
        prop_offset = offset + 13 + name_len
        
        props = []
        cur = prop_offset
        for _ in range(num_props):
            ptype = chr(data[cur])
            cur += 1
            if ptype == 'S':
                plen = struct.unpack('<I', data[cur:cur+4])[0]
                cur += 4
                s = data[cur:cur+plen].decode('utf-8', errors='ignore')
                cur += plen
                props.append(s)
            elif ptype == 'R': # raw binary
                plen = struct.unpack('<I', data[cur:cur+4])[0]
                cur += 4 + plen
                props.append('<raw>')
            elif ptype in ('I', 'F', 'D', 'L', 'C'):
                sizes = {'I': 4, 'F': 4, 'D': 8, 'L': 8, 'C': 1}
                cur += sizes[ptype]
                props.append('<num>')
            elif ptype in ('f', 'd', 'l', 'i', 'b'): # array
                arr_len, enc, comp_len = struct.unpack('<III', data[cur:cur+12])
                cur += 12 + comp_len
                props.append('<array>')
            else:
                break
        
        children = []
        sub_cur = cur
        while sub_cur < end_offset:
            child, next_sub = read_node(sub_cur)
            if child:
                children.append(child)
            sub_cur = next_sub
            if next_sub == sub_cur: # avoid infinite loop
                break
        
        return {'name': name, 'props': props, 'children': children}, end_offset
    
    cur = 27
    root_nodes = []
    while cur < len(data) - 160:
        node, next_cur = read_node(cur)
        if node:
            root_nodes.append(node)
        cur = next_cur
        if next_cur == cur:
            break
    
    return root_nodes

fbx_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"
nodes = parse_fbx_binary(fbx_path)
print(f"Top-level nodes: {[n['name'] for n in nodes]}")
objects = [n for n in nodes if n['name'] == 'Objects']
if objects:
    models = [c for c in objects[0]['children'] if c['name'] == 'Model']
    print(f"Total Models in Objects: {len(models)}")
    for m in models:
        print(f"  Model: {m['props']}")
