import os
import sys

# Set stdout encoding to utf-8
sys.stdout.reconfigure(encoding='utf-8')

folder = "c:/Users/Admin/Documents/BES/Assets"
image_extensions = ('.png', '.jpg', '.jpeg', '.tga', '.psd', '.tif', '.tiff', '.bmp')

print("Searching for image files in Assets...")
count = 0
for root, dirs, files in os.walk(folder):
    for file in files:
        if file.lower().endswith(image_extensions):
            count += 1
            path = os.path.join(root, file).replace('\\', '/')
            print(f"Found: {path}")
            if count >= 100:
                print("Too many results, truncating...")
                break
    if count >= 100:
        break

print(f"Total image files found: {count}")
