meta_path = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Model_Quai_Con\Meshy_AI_Animation_Walking_frame_rate_60.fbx.meta"
with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

idx = content.find("humanDescription:")
if idx != -1:
    print(content[idx-200:idx+400])
else:
    print(content[-500:])
