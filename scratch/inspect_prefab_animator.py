import os
import re

prefab_path = "c:/Users/Admin/Documents/BES/Assets/_Project/Prefabs/Player_elio.prefab"
print("Reading Player_elio.prefab Animator configuration...")
if os.path.exists(prefab_path):
    with open(prefab_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # We want to find the Animator component block:
    # Animator:
    #   ...
    #   m_Controller: {fileID: ..., guid: ..., type: ...}
    animators = re.findall(r'Animator:\s*.*?\s*m_Controller:\s*({[^}]*})', content, re.DOTALL)
    print(f"Found Animator m_Controller in prefab: {animators}")
    
    # Let's print the entire Animator block
    blocks = re.split(r'--- !u!114 &|--- !u!95 &|--- !u!1 &', content)
    for b in blocks:
        if 'm_Controller' in b:
            print("--- Animator Block ---")
            print(b[:400])
else:
    print("Prefab not found.")
