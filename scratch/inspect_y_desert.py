import re

scene_path = r"c:\Users\Admin\Documents\BES\Assets\Scenes\desert map.unity"
with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

gos = re.findall(r'--- !u!1 &([0-9-]+).*?m_Name:\s*([^\n]+)', content, re.DOTALL)
go_dict = {gid: name for gid, name in gos}

transforms = re.findall(r'--- !u!4 &([0-9-]+).*?m_GameObject:\s*\{fileID:\s*([0-9-]+)\}.*?m_LocalPosition:\s*\{x:\s*([0-9.-]+),\s*y:\s*([0-9.-]+),\s*z:\s*([0-9.-]+)\}', content, re.DOTALL)

ys = []
objects = []
for tid, gid, x, y, z in transforms:
    name = go_dict.get(gid, "Unknown")
    try:
        yf = float(y)
        ys.append(yf)
        objects.append((name, float(x), yf, float(z)))
    except:
        pass

if ys:
    print(f"Total objects: {len(ys)}")
    print(f"Min Y: {min(ys):.2f}, Max Y: {max(ys):.2f}")
    
    # Sort by Y ascending
    objects.sort(key=lambda item: item[2])
    print("\nLowest 15 objects:")
    for name, x, y, z in objects[:15]:
        print(f"  {name}: pos=({x:.1f}, {y:.1f}, {z:.1f})")
    
    print("\nHighest 15 objects:")
    for name, x, y, z in objects[-15:]:
        print(f"  {name}: pos=({x:.1f}, {y:.1f}, {z:.1f})")
