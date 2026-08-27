import os
import re
import sys

# Force UTF-8 stdout
sys.stdout.reconfigure(encoding='utf-8')

path = "c:/Users/Admin/Documents/BES/ĐỒ ÁN  TỐT NGHIỆP.md"
print("Searching document for 'rương' or similar...")
if os.path.exists(path):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
            
    normalized = re.sub(r'\s+', ' ', content)
    
    # We search for "rương" case-insensitively, allowing any spacing or accents
    # Also search for "rương", "hòm", "chest", "treasure", "kho báu"
    # Let's print any matches of "rương" or "chest" or "treasure" (not inside base64!)
    # Base64 string characters are mostly alphanumeric without spaces, so we can ignore long words
    words = normalized.split(' ')
    clean_text = ' '.join([w for w in words if len(w) < 30])
    
    targets = ["rương", "ruong", "hòm", "hom", "kho báu", "kho bau", "chest", "treasure", "đồ án"]
    for t in targets:
        matches = []
        for match in re.finditer(re.escape(t), clean_text, re.IGNORECASE):
            start = max(0, match.start() - 100)
            end = min(len(clean_text), match.end() + 100)
            matches.append(clean_text[start:end])
        print(f"Target '{t}': Found {len(matches)} matches.")
        for idx, m in enumerate(matches[:10]):
            print(f"  {idx+1}: ...{m}...")
else:
    print("File not found.")
