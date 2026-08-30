with open(r"c:\Users\Admin\Documents\BES\Assets\_Project\Prefabs\Enemy_BabyMonster.prefab", 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

idx = content.find("Animator:")
if idx != -1:
    print(content[idx:idx+800])
