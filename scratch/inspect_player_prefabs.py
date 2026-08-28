import glob
import re

for p in glob.glob(r"c:\Users\Admin\Documents\BES\Assets\_Project\Prefabs\Player_*.prefab"):
    with open(p, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    print(f"=== {p} ===")
    
    # Find CharacterController
    cc_match = re.search(r'CharacterController:.*?m_Height:\s*([0-9.]+).*?m_Radius:\s*([0-9.]+).*?m_Center:\s*\{x:\s*([0-9.-]+),\s*y:\s*([0-9.-]+),\s*z:\s*([0-9.-]+)\}.*?m_SkinWidth:\s*([0-9.]+).*?m_StepOffset:\s*([0-9.]+)', content, re.DOTALL)
    if cc_match:
        print(f"  CharacterController: Height={cc_match.group(1)}, Radius={cc_match.group(2)}, Center=({cc_match.group(3)}, {cc_match.group(4)}, {cc_match.group(5)}), SkinWidth={cc_match.group(6)}, StepOffset={cc_match.group(7)}")
    
    # Find Transform localPositions
    transforms = re.findall(r'Transform:.*?m_LocalPosition:\s*\{x:\s*([0-9.-]+),\s*y:\s*([0-9.-]+),\s*z:\s*([0-9.-]+)\}', content, re.DOTALL)
    print(f"  LocalPositions found: {transforms[:5]}")
