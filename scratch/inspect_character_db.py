import os

db_path = "c:/Users/Admin/Documents/BES/Assets/Resources/Data/CharacterDatabase.asset"
if os.path.exists(db_path):
    print("Reading CharacterDatabase.asset...")
    with open(db_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    # Unity YAML files have serialized fields. Let's find character IDs and names.
    # Look for patterns like:
    # id: elio
    # name: ...
    import re
    # Find all matches of id and name or similar fields
    ids = re.findall(r'id:\s*([^\n\r]+)', content)
    names = re.findall(r'characterName:\s*([^\n\r]+)', content)
    
    print(f"IDs found: {ids}")
    print(f"Names found: {names}")
    
    # Print blocks of characters
    lines = content.split('\n')
    for i, line in enumerate(lines):
        if 'id:' in line or 'characterName:' in line or 'displayName:' in line:
            print(f"Line {i+1}: {line.strip()}")
else:
    print("CharacterDatabase.asset not found.")
