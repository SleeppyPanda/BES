import re

with open(r"c:\Users\Admin\Documents\BES\Assets\_Project\Prefabs\Enemy_BabyMonster.prefab", 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

names = re.findall(r'GameObject:.*?m_Name:\s*([a-zA-Z0-9_]+)', content, re.DOTALL)
print(f"Total GameObjects in Enemy_BabyMonster.prefab: {len(names)}")
print("Names:", names)
