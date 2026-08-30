with open(r"c:\Users\Admin\Documents\BES\Assets\_Project\AnimatorControllers\Enemy_BabyMonster.controller", 'r', encoding='utf-8') as f:
    text = f.read()

import re
states = re.findall(r'--- !u!1102 &([0-9-]+).*?m_Name: (.*?)\n.*?m_Motion: \{fileID: ([0-9-]+), guid: ([a-f0-9]+)', text, re.DOTALL)
print("States:")
for sid, sname, fid, guid in states:
    print(f"  [{sid}] {sname}: fileID={fid}, guid={guid}")

transitions = re.findall(r'--- !u!1101 &([0-9-]+).*?m_Conditions:(.*?)m_DstState: \{fileID: ([0-9-]+)\}', text, re.DOTALL)
print("\nTransitions:")
for tid, conds, dst in transitions:
    print(f"  [{tid}] -> Dst {dst}: {' '.join(conds.strip().split())}")
