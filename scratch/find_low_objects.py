import re

scene_path = r"c:\Users\Admin\Documents\BES\Assets\Scenes\Map_Sa_Mac_2.unity"
with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

# Find gameobjects and their transform positions
gos = re.findall(r'--- !u!1 &([0-9-]+).*?m_Name:\s*([^\n]+)', content, re.DOTALL)
go_dict = {gid: name for gid, name in gos}

transforms = re.findall(r'--- !u!4 &([0-9-]+).*?m_GameObject:\s*\{fileID:\s*([0-9-]+)\}.*?m_LocalPosition:\s*\{x:\s*([0-9.-]+),\s*y:\s*([0-9.-]+),\s*z:\s*([0-9.-]+)\}', content, re.DOTALL)

print("=== Scene Objects Analysis ===")
low_objects = []
for tid, gid, x, y, z in transforms:
    name = go_dict.get(gid, "Unknown")
    try:
        yf = float(y)
        if yf < -1.0:
            low_objects.append((name, float(x), yf, float(z)))
    except:
        pass

print(f"Objects below Y = -1.0 (Total: {len(low_objects)}):")
for name, x, y, z in low_objects[:30]:
    print(f"  {name}: pos=({x:.1f}, {y:.1f}, {z:.1f})")
