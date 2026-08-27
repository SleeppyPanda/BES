import os

guids = {
    'ef81f1816e86be247b97cda08c69be8f': 'Idle',
    'ea0fa813083dbd043b2e59df1da38804': 'Walk',
    'fa30a08e063deae49bf44360a0f8bfdf': 'Run'
}

folder = "c:/Users/Admin/Documents/BES/Assets"
print("Searching for animation clip meta files...")
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.meta'):
            path = os.path.join(root, file).replace('\\', '/')
            with open(path, 'r', errors='ignore') as f:
                content = f.read()
            for guid, name in guids.items():
                if guid in content:
                    print(f"GUID for {name} ({guid}) corresponds to: {path}")
