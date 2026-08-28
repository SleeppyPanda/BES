import glob

oasis_folder = r"c:\Users\Admin\Documents\BES\Assets\MeshyImports\Meshy_AI_Sandstone_Oasis_Guard_biped\Meshy_AI_Sandstone_Oasis_Guard_biped"
for f in glob.glob(oasis_folder + r"\*.fbx"):
    print(f)
