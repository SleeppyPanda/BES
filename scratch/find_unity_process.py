import subprocess
import os

try:
    out = subprocess.check_output('wmic process where "name=\'Unity.exe\'" get ExecutablePath', shell=True).decode()
    print("WMIC Output:")
    print(out)
except Exception as e:
    print("Error running WMIC:", e)

# Fallback: search common paths
paths = [
    r"C:\Program Files\Unity\Hub\Editor",
    r"C:\Program Files (x86)\Unity\Editor",
    r"C:\Program Files\Unity\Editor"
]

print("Searching common paths:")
for p in paths:
    if os.path.exists(p):
        for root, dirs, files in os.walk(p):
            if "Unity.exe" in files:
                print("Found Unity at:", os.path.join(root, "Unity.exe"))
                break
