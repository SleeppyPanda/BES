import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

path = "c:/Users/Admin/Documents/BES/ĐỒ ÁN  TỐT NGHIỆP.md"
print("Searching full document...")
if os.path.exists(path):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
        
    targets = ["rương", "hòm", "chest", "treasure", "tương tác", "khám phá", "mở khóa"]
    for t in targets:
        found_lines = []
        for i, line in enumerate(lines):
            # clean line of base64 data to avoid printing large lines
            if len(line) > 500:
                continue
            if t.lower() in line.lower():
                found_lines.append((i+1, line.strip()))
        print(f"\nTarget '{t}': Found {len(found_lines)} matches.")
        for line_num, text in found_lines[:15]:
            print(f"  Line {line_num}: {text}")
else:
    print("File not found.")
