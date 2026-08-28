with open(r"c:\Users\Admin\Documents\BES\Assets\_Project\Prefabs\Enemy_BabyMonster.prefab", 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

idx = content.find("4729215998307139338")
while idx != -1:
    print(content[max(0, idx-50):idx+300])
    print("---------------------------------")
    idx = content.find("4729215998307139338", idx+1)
