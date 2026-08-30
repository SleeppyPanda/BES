with open(r"c:\Users\Admin\Documents\BES\Assets\_Project\Prefabs\Enemy_BabyMonster.prefab", 'r', encoding='utf-8') as f:
    text = f.read()

import re
scripts = re.findall(r'MonoBehaviour:.*?m_Script:\s*\{fileID:\s*([0-9-]+),\s*guid:\s*([a-f0-9]+).*?\}', text, re.DOTALL)
print("Monobehaviours count:", len(scripts))
for fid, guid in scripts:
    print(f"  guid: {guid}")
