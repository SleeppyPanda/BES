import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

path = "c:/Users/Admin/Documents/BES/ĐỒ ÁN  TỐT NGHIỆP.md"
print("Printing lines 1775-1810...")
if os.path.exists(path):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    for idx in range(1774, min(1815, len(lines))):
        print(f"Line {idx+1}: {lines[idx].strip()}")
else:
    print("File not found.")
