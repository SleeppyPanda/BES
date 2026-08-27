import os

folder = "c:/Users/Admin/Documents/BES/Assets/_Project/Scripts"
for root, dirs, files in os.walk(folder):
    for file in files:
        if "camera" in file.lower() or "follow" in file.lower():
            print(os.path.join(root, file).replace('\\', '/'))
