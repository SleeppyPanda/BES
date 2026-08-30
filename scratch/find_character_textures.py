import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

folder = "c:/Users/Admin/Documents/BES/Assets"
image_extensions = ('.png', '.jpg', '.jpeg', '.tga', '.psd', '.tif', '.tiff', '.bmp')

exclude_folders = {'art ui', 'ui', 'jmo assets', 'dunguyn', 'cartoonvfx9x', 'textmesh pro', 'tutorialinfo'}

print("Searching for character-related texture files...")
count = 0
for root, dirs, files in os.walk(folder):
    # Exclude folders
    dirs[:] = [d for d in dirs if d.lower() not in exclude_folders]
    
    for file in files:
        if file.lower().endswith(image_extensions):
            path = os.path.join(root, file).replace('\\', '/')
            print(f"Found: {path}")
            count += 1

print(f"Total relevant image files: {count}")
