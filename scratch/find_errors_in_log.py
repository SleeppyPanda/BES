import os

editor_log = os.path.expanduser(r"~\AppData\Local\Unity\Editor\Editor.log")
with open(editor_log, 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

print("=== Search for Exception / Error / Break in last 500 lines ===")
for i, line in enumerate(lines[-500:]):
    lower = line.lower()
    if "exception" in lower or "nullreference" in lower or "error" in lower or "break" in lower:
        if "curl error" not in lower:
            print(f"Line {i}: {line.strip()}")
