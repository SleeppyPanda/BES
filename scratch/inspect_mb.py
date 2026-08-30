with open(r"c:\Users\Admin\Documents\BES\Assets\_Project\Prefabs\Enemy_BabyMonster.prefab", 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

idx = content.find("EnemyAI")
if idx != -1:
    print(content[idx:idx+600])
else:
    # Look for all MonoBehaviours
    mb = content.find("MonoBehaviour:")
    while mb != -1:
        print(content[mb:mb+400])
        print("========================")
        mb = content.find("MonoBehaviour:", mb+1)
