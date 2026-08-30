import os
import re

controller_path = "c:/Users/Admin/Documents/BES/Assets/_Project/AnimatorControllers/PlayerAnimatorController.controller"
if os.path.exists(controller_path):
    with open(controller_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    blocks = re.split(r'--- !u!1101 &', content)[1:]
    if blocks:
        print("=== RAW BLOCK 1 ===")
        # Print first 30 lines
        lines = blocks[0].split('\n')
        for i in range(min(30, len(lines))):
            print(lines[i])
