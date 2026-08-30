import re

with open(r"Assets\_Project\Prefabs\Enemy_BabyMonster.prefab", "r", errors="ignore") as f:
    c = f.read()

go_names = {}
tf_to_go = {}
go_to_tf = {}
parent_map = {}

docs = c.split("--- !u!")
for d in docs:
    if d.startswith("1 &"):
        go_id = re.search(r"&(\d+)", d).group(1)
        name = re.search(r"m_Name: (.*)", d).group(1)
        go_names[go_id] = name
    elif d.startswith("4 &"):
        tf_id = re.search(r"&(\d+)", d).group(1)
        go_id = re.search(r"m_GameObject: {fileID: (\d+)}", d).group(1)
        parent_id = re.search(r"m_Father: {fileID: (\d+)}", d).group(1)
        tf_to_go[tf_id] = go_id
        go_to_tf[go_id] = tf_id
        parent_map[tf_id] = parent_id

root_tf = None
for tf_id, p_id in parent_map.items():
    if p_id == "0":
        root_tf = tf_id
        break

def print_tree(tf_id, indent=0):
    go_id = tf_to_go.get(tf_id)
    name = go_names.get(go_id, "Unknown")
    print("  " * indent + f"- {name} (tf: {tf_id}, go: {go_id})")
    for child_tf, p_id in parent_map.items():
        if p_id == tf_id:
            print_tree(child_tf, indent + 1)

print("Full Prefab Hierarchy Tree:")
if root_tf:
    print_tree(root_tf)
else:
    print("Root transform not found")
