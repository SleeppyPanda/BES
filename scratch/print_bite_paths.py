with open(r"c:\Users\Admin\Documents\BES\Assets\_Project\Animations\Enemy_BabyMonster_Bite.anim", 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

import re
paths = re.findall(r'path:\s*([a-zA-Z0-9_/]+)', content)
print("Paths in Bite anim:", set(paths))
