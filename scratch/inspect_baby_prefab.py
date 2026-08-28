import re

ctrl_path = r"c:\Users\Admin\Documents\BES\Assets\_Project\AnimatorControllers\Enemy_BabyMonster.controller"
with open(ctrl_path, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

print("=== Enemy_BabyMonster.controller ===")

# Parameters
params = re.findall(r'm_Name:\s*([a-zA-Z0-9_]+)\s*\n\s*m_Type:\s*([0-9]+)', content)
print("Parameters:", params)

# States & Motions
states = re.findall(r'AnimatorState:.*?m_Name:\s*([a-zA-Z0-9_]+).*?m_Motion:\s*\{fileID:\s*([0-9-]+),\s*guid:\s*([a-f0-9]+).*?\}', content, re.DOTALL)
print("States & Motions:")
for sname, fid, guid in states:
    print(f"  {sname}: fileID={fid}, guid={guid}")

# Transitions
transitions = re.findall(r'AnimatorStateTransition:.*?m_Name:\s*([a-zA-Z0-9_]*).*?m_Conditions:(.*?)m_DstState:\s*\{fileID:\s*([0-9-]+)\}', content, re.DOTALL)
print(f"Total Transitions: {len(transitions)}")
for tname, conds, dst in transitions:
    cond_clean = " ".join(conds.strip().split())
    print(f"  -> Dst: {dst}, Conds: {cond_clean}")

prefab_path = r"c:\Users\Admin\Documents\BES\Assets\_Project\Prefabs\Enemy_BabyMonster.prefab"
with open(prefab_path, 'r', encoding='utf-8', errors='ignore') as f:
    pcontent = f.read()

# Check components on prefab
print("\n=== Enemy_BabyMonster.prefab components ===")
anims = re.findall(r'Animator:.*?m_Controller:\s*\{fileID:\s*([0-9-]+),\s*guid:\s*([a-f0-9]+).*?\}', pcontent, re.DOTALL)
print("Animator Controllers on prefab:", anims)
avatars = re.findall(r'm_Avatar:\s*\{fileID:\s*([0-9-]+),\s*guid:\s*([a-f0-9]+).*?\}', pcontent)
print("Avatars on prefab:", avatars)
ai_scripts = re.findall(r'EnemyAI:.*', pcontent)
print("EnemyAI count:", len(ai_scripts))
