import re

ctrl_path = r"c:\Users\Admin\Documents\BES\Assets\_Project\AnimatorControllers\PlayerAnimatorController.controller"
with open(ctrl_path, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

print("=== PlayerAnimatorController.controller ===")

# Find parameters
params = re.findall(r'm_Name:\s*([a-zA-Z0-9_]+)\s*\n\s*m_Type:\s*([0-9]+)', content)
print("Parameters:", params)

# Find states & motions
states = re.findall(r'AnimatorState:.*?m_Name:\s*([a-zA-Z0-9_]+).*?m_Motion:\s*\{fileID:\s*([0-9-]+),\s*guid:\s*([a-f0-9]+).*?\}', content, re.DOTALL)
print("States & Motions:")
for sname, fid, guid in states:
    print(f"  {sname}: fileID={fid}, guid={guid}")

# Find default state
default_state = re.search(r'm_DefaultState:\s*\{fileID:\s*([0-9-]+)\}', content)
if default_state:
    print("Default State FileID:", default_state.group(1))

# Find transitions
transitions = re.findall(r'AnimatorStateTransition:.*?m_Name:\s*([a-zA-Z0-9_]*).*?m_Conditions:(.*?)m_DstState:\s*\{fileID:\s*([0-9-]+)\}', content, re.DOTALL)
print(f"Total Transitions: {len(transitions)}")
for tname, conds, dst in transitions[:10]:
    cond_clean = " ".join(conds.strip().split())
    print(f"  -> Dst: {dst}, Conds: {cond_clean}")
