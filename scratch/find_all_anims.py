import os
import glob

assets_dir = r"c:\Users\Admin\Documents\BES\Assets"
all_fbx = glob.glob(os.path.join(assets_dir, "**", "*.fbx"), recursive=True)
all_anim = glob.glob(os.path.join(assets_dir, "**", "*.anim"), recursive=True)
all_glb = glob.glob(os.path.join(assets_dir, "**", "*.glb"), recursive=True)

print(f"Total FBX files: {len(all_fbx)}")
for f in all_fbx:
    if any(k in f.lower() for k in ['quai', 'monster', 'walk', 'attack', 'bite', 'run']):
        print("  FBX:", os.path.relpath(f, assets_dir))

print(f"\nTotal Anim files: {len(all_anim)}")
for a in all_anim:
    print("  Anim:", os.path.relpath(a, assets_dir))

print(f"\nTotal GLB files: {len(all_glb)}")
for g in all_glb:
    print("  GLB:", os.path.relpath(g, assets_dir))
