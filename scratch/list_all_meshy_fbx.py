import glob
import re

for path in glob.glob(r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\**\*.fbx", recursive=True):
    print("FBX:", path)
