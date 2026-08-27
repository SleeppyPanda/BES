import os

log_path = "C:/Users/Admin/AppData/Local/Unity/Editor/Editor.log"
if os.path.exists(log_path):
    print("Reading Unity Editor.log...")
    with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    print("Last 100 lines of Editor.log:")
    for line in lines[-100:]:
        print(line, end='')
else:
    print(f"Editor.log not found at {log_path}")
