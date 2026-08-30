import re

ctrl_path = r"c:\Users\Admin\Documents\BES\Assets\_Project\AnimatorControllers\Enemy_BabyMonster.controller"
with open(ctrl_path, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

print("=== Enemy_BabyMonster.controller inspection ===")
# Find all AnimatorState and their motions
states = re.findall(r'AnimatorState:.*?m_Name:\s*([a-zA-Z0-9_]+).*?m_Motion:\s*\{fileID:\s*([0-9-]+),\s*guid:\s*([a-f0-9]+).*?\}', content, re.DOTALL)
print("States and Motions:")
for sname, fid, guid in states:
    print(f"  State: {sname} -> Motion fileID={fid}, guid={guid}")
