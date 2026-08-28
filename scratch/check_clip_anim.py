with open(r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx.meta", 'r', encoding='utf-8') as f:
    text = f.read()

import re
clips = re.findall(r'clipAnimations:(.*?)\n\s*isReadable:', text, re.DOTALL)
if clips:
    print("clipAnimations:", clips[0][:500])
else:
    print("No clipAnimations section found!")
