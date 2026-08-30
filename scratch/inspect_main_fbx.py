import glob
import re

for path in glob.glob(r"c:\Users\Admin\Documents\BES\Assets\Model character\*.fbx"):
    with open(path, 'rb') as f:
        data = f.read()
    takes = re.findall(b'Take:\\s*"([^"]+)"', data)
    if not takes:
        takes = re.findall(b'AnimationStack::\\s*([a-zA-Z0-9_]+)', data)
    print(f"=== {path} ===")
    print(f"  Size: {len(data)} bytes")
    print(f"  Takes found: {takes[:10]}")
