import os

parent = "Assets/MeshyImports"
if os.path.exists(parent):
    for d in os.listdir(parent):
        full_d = os.path.join(parent, d)
        if os.path.isdir(full_d) and "quái" in d and "con" in d:
            new_d = os.path.join(parent, "Model_Quai_Con")
            print(f"Renaming directory {full_d} -> {new_d}")
            os.rename(full_d, new_d)
            
            # Also rename meta file
            meta = full_d + ".meta"
            new_meta = new_d + ".meta"
            if os.path.exists(meta):
                print(f"Renaming meta file {meta} -> {new_meta}")
                os.rename(meta, new_meta)
            break
else:
    print("Parent directory not found")
