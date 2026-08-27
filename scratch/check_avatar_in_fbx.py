import os
import re

def check_avatar(fbx_path):
    print(f"=== AVATARS IN {os.path.basename(fbx_path)} ===")
    if not os.path.exists(fbx_path):
        print("  Not found!")
        return
    with open(fbx_path, 'rb') as f:
        content = f.read()
    # Search for Avatar definition in FBX or meta
    # In Unity meta files, we have "avatarSetup" and in FBX we might see bone mappings.
    # Let's search for "Avatar" or "AvatarSetup" in the binary content.
    avatars = re.findall(b'Avatar:\\s*\\d+', content)
    print(f"Found Avatar tokens in FBX: {len(avatars)}")

folder = "c:/Users/Admin/Documents/BES/Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped"
check_avatar(os.path.join(folder, "Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx"))
