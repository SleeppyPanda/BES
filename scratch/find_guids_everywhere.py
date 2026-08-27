import os

guids = [
    'ef81f1816e86be247b97cda08c69be8f',
    'ea0fa813083dbd043b2e59df1da38804',
    'fa30a08e063deae49bf44360a0f8bfdf'
]

folder = "c:/Users/Admin/Documents/BES"
print("Searching for original GUIDs...")
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.meta'):
            path = os.path.join(root, file)
            try:
                with open(path, 'r', errors='ignore') as f:
                    content = f.read().lower()
                for g in guids:
                    if g in content:
                        print(f"Found GUID {g} in file: {path}")
            except Exception as e:
                pass
