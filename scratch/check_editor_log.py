import os
import time

editor_log = os.path.expanduser(r"~\AppData\Local\Unity\Editor\Editor.log")
if os.path.exists(editor_log):
    mtime = os.path.getmtime(editor_log)
    print(f"Editor.log last modified: {time.ctime(mtime)}")
    with open(editor_log, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    # Print last 30 lines
    print("=== Last 30 lines of Editor.log ===")
    for line in lines[-30:]:
        print(line, end='')
