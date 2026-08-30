import os

fbx_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx"

try:
    with open(fbx_path, 'rb') as f:
        content = f.read()
    
    # Simple check for Take name strings in FBX
    print(f"FBX File Size: {len(content)} bytes")
    # In FBX binary or ASCII, animations are defined in Takes or AnimStack
    # Let's search for "Take" or "Animation" strings
    import re
    takes = re.findall(b'Take:\\s*"([^"]+)"', content)
    if not takes:
        takes = re.findall(b';Take:\\s*(\\w+)', content)
    if not takes:
        takes = re.findall(b'AnimationStack::\\s*(\\w+)', content)
        
    print(f"Takes/AnimStacks found: {takes}")
except Exception as e:
    print(f"Error reading FBX: {e}")
