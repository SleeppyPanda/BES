import os

controller_path = "c:/Users/Admin/Documents/BES/Assets/_Project/AnimatorControllers/PlayerAnimatorController.controller"
if os.path.exists(controller_path):
    print("Reading PlayerAnimatorController.controller...")
    with open(controller_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
        
    # We want to check the states and their motions:
    # m_Name: Idle / m_Motion: {fileID: ..., guid: ...}
    # Let's search for m_Name and the following m_Motion
    import re
    state_motions = re.findall(r'm_Name:\s*([^\n\r]+)(?:\s*[\w:]+)*\s*m_Motion:\s*([^\n\r]+)', content)
    print("Found states and motions in Animator Controller:")
    for state, motion in state_motions:
        print(f"State: {state} -> Motion: {motion}")
else:
    print("PlayerAnimatorController.controller not found.")
