import os
import re

controller_path = "c:/Users/Admin/Documents/BES/Assets/_Project/AnimatorControllers/PlayerAnimatorController.controller"
if os.path.exists(controller_path):
    print("Reading Animator Controller transitions...")
    with open(controller_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    blocks = re.split(r'--- !u!1101 &', content)[1:]
    for idx, block in enumerate(blocks):
        # We search for lines under m_Conditions:
        #   - m_ConditionMode: ...
        #     m_ConditionEvent: ...
        #     m_EventTreshold: ...
        cond_block = re.search(r'm_Conditions:\s*(.*?)\s*m_DstStateMachine:', block, re.DOTALL)
        conditions = []
        if cond_block:
            conds_raw = re.findall(r'-\s*m_ConditionMode:\s*(\d+)\s+m_ConditionEvent:\s*([^\n\r]+)\s+m_EventTreshold:\s*([^\n\r]+)', cond_block.group(1))
            conditions = conds_raw
        
        dst = re.search(r'm_DstState:\s*({[^}]*})', block)
        dst_str = dst.group(1) if dst else "Exit"
        
        duration = re.search(r'm_TransitionDuration:\s*([^\n\r]+)', block)
        exit_time = re.search(r'm_HasExitTime:\s*([^\n\r]+)', block)
        
        print(f"Transition {idx+1}: Dst={dst_str}, Duration={duration.group(1)}, HasExitTime={exit_time.group(1)}")
        for mode, event, thresh in conditions:
            print(f"  Condition: Event='{event}', Mode={mode}, Threshold={thresh}")
else:
    print("Animator Controller not found.")
