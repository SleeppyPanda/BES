import os
import re

folder = "c:/Users/Admin/Documents/BES/Assets/_Project/AnimatorControllers"
for file in os.listdir(folder):
    if file.endswith('.overrideController'):
        path = os.path.join(folder, file)
        print(f"=== {file} ===")
        with open(path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        # Find overrides:
        # m_Clips:
        #   - m_OriginalClip: {fileID: ..., guid: ...}
        #     m_OverrideClip: {fileID: ..., guid: ...}
        # We can extract all original and override clip guids
        clips = re.findall(r'm_OriginalClip:\s*\{[^}]*guid:\s*([a-f0-9]+)[^}]*\}\s*m_OverrideClip:\s*\{[^}]*guid:\s*([a-f0-9]+)', content, re.DOTALL)
        for orig, ovr in clips:
            print(f"  Override: Original GUID {orig} -> Override GUID {ovr}")
