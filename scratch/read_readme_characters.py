import os

readme_path = "c:/Users/Admin/Documents/BES/README.md"
with open(readme_path, 'r', encoding='utf-8', errors='ignore') as f:
    for i, line in enumerate(f, 1):
        if any(name in line.lower() for name in ['elio', 'aurelian', 'sahure', 'rashad']):
            print(f"Line {i}: {line.strip()}")
