import os
import re

def parse_class_brief(file_path):
    with open(file_path, "r", encoding="utf-8-sig", errors="ignore") as f:
        content = f.read()
        
    lines = content.split('\n')
    
    # Simple regex to find namespace, class/struct/interface/enum, and base classes
    class_def_pat = re.compile(r'\b(class|struct|interface|enum)\s+(\w+)(?:\s*:\s*([\w\s,<>]+))?')
    namespace_pat = re.compile(r'\bnamespace\s+([\w\.]+)')
    
    namespace = "Global"
    types = []
    
    comment_buffer = []
    
    for line in lines:
        stripped = line.strip()
        
        # Collect comments
        if stripped.startswith('//') or stripped.startswith('///'):
            comment_buffer.append(stripped)
            continue
            
        ns_match = namespace_pat.search(stripped)
        if ns_match:
            namespace = ns_match.group(1)
            comment_buffer = []
            continue
            
        type_match = class_def_pat.search(stripped)
        if type_match:
            type_kind = type_match.group(1)
            type_name = type_match.group(2)
            bases = type_match.group(3)
            
            # Keep only the class summary comments (e.g. up to 4 lines, stripping the /// or //)
            clean_comments = []
            for c in comment_buffer:
                c_clean = c.lstrip('/ \t*')
                if c_clean:
                    clean_comments.append(c_clean)
            
            summary = " ".join(clean_comments[:3]) # First 3 lines
            
            base_list = [b.strip() for b in bases.split(',')] if bases else []
            
            types.append({
                "kind": type_kind,
                "name": type_name,
                "bases": base_list,
                "summary": summary
            })
            comment_buffer = []
            
        # Reset if empty line or unrelated code
        if not stripped:
            comment_buffer = []
            
    return namespace, types

def main():
    root_dir = r"c:\Users\Admin\Documents\BES\Assets"
    output_file = r"c:\Users\Admin\Documents\BES\scratch\codebase_summary.txt"
    
    # We will group by folder & namespace
    modules = {}
    
    for dirpath, dirnames, filenames in os.walk(root_dir):
        if any(p in dirpath for p in ("Library", "Temp", "packages")):
            continue
            
        for filename in filenames:
            if filename.endswith(".cs"):
                full_path = os.path.join(dirpath, filename)
                rel_path = os.path.relpath(full_path, r"c:\Users\Admin\Documents\BES")
                
                # Determine module by subdirectory
                parts = rel_path.split(os.sep)
                # Examples:
                # Assets/UI/Scripts/Menu/StoryModePanelController.cs -> UI/Scripts/Menu
                # Assets/_Project/Scripts/Gameplay/Combat/CombatManager.cs -> _Project/Scripts/Gameplay/Combat
                if len(parts) >= 4:
                    module_name = "/".join(parts[1:-1])
                else:
                    module_name = parts[1] if len(parts) > 2 else "Root"
                    
                namespace, types = parse_class_brief(full_path)
                
                if not types:
                    continue
                    
                if module_name not in modules:
                    modules[module_name] = []
                    
                for t in types:
                    modules[module_name].append({
                        "file": rel_path,
                        "namespace": namespace,
                        "kind": t["kind"],
                        "name": t["name"],
                        "bases": t["bases"],
                        "summary": t["summary"]
                    })
                    
    with open(output_file, "w", encoding="utf-8") as out:
        out.write("HIGH-LEVEL CODEBASE SUMMARY\n")
        out.write("===========================\n\n")
        
        # Sort modules alphabetically
        for mod in sorted(modules.keys()):
            out.write(f"=== MODULE: {mod} ===\n")
            out.write("=" * (12 + len(mod)) + "\n\n")
            
            # Group by namespace in this module
            ns_groups = {}
            for t in modules[mod]:
                ns = t["namespace"]
                if ns not in ns_groups:
                    ns_groups[ns] = []
                ns_groups[ns].append(t)
                
            for ns in sorted(ns_groups.keys()):
                out.write(f"  Namespace: {ns}\n")
                out.write(f"  " + "-" * (11 + len(ns)) + "\n")
                
                for t in ns_groups[ns]:
                    bases_str = f" : {', '.join(t['bases'])}" if t['bases'] else ""
                    out.write(f"    - [{t['kind']}] {t['name']}{bases_str}\n")
                    out.write(f"      File: {t['file']}\n")
                    if t['summary']:
                        out.write(f"      Desc: {t['summary']}\n")
                    out.write("\n")
            out.write("\n" + "#"*80 + "\n\n")
            
    print(f"Summary written to {output_file}")

if __name__ == "__main__":
    main()
