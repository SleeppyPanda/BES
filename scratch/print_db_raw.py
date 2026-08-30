import os

db_path = "C:/Users/Admin/Documents/BES/Assets/Resources/Data/ItemDatabase.asset"
if os.path.exists(db_path):
    with open(db_path, 'r', encoding='utf-8', errors='ignore') as f:
        for i in range(100):
            print(f.readline().strip())
else:
    print("Not found.")
