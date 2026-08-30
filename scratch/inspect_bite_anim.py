anim_path = r"c:\Users\Admin\Documents\BES\Assets\_Project\Animations\Enemy_BabyMonster_Bite.anim"
with open(anim_path, 'r', encoding='utf-8', errors='ignore') as f:
    content = f.read()

print("=== Enemy_BabyMonster_Bite.anim inspection ===")
for line in content.splitlines()[:60]:
    print(line)
