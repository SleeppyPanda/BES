import os
import re

def extract_cs_info(file_path):
    with open(file_path, "r", encoding="utf-8-sig", errors="ignore") as f:
        content = f.read()
    
    # Simple regex to find namespace, class/struct/interface/enum, and public members
    lines = content.split('\n')
    extracted = []
    
    class_def_pat = re.compile(r'\b(class|struct|interface|enum)\s+(\w+)')
    namespace_pat = re.compile(r'\bnamespace\s+([\w\.]+)')
    
    current_namespace = "Global"
    current_type = None
    
    comment_buffer = []
    
    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        
        # Collect comments
        if stripped.startswith('//') or stripped.startswith('///'):
            comment_buffer.append(stripped)
            continue
        
        # Check namespace
        ns_match = namespace_pat.search(stripped)
        if ns_match:
            current_namespace = ns_match.group(1)
            comment_buffer = []
            continue
            
        # Check class/struct/interface/enum
        type_match = class_def_pat.search(stripped)
        if type_match:
            type_kind = type_match.group(1)
            type_name = type_match.group(2)
            
            # Print type declaration
            comments = "\n".join(comment_buffer)
            comment_buffer = []
            
            # Clean up the declaration line (e.g. inheritance)
            decl = stripped
            if decl.endswith('{'):
                decl = decl[:-1].strip()
            
            extracted.append({
                "line": i,
                "type": "type",
                "kind": type_kind,
                "name": type_name,
                "decl": decl,
                "comments": comments,
                "members": []
            })
            current_type = extracted[-1]
            continue
            
        # If inside a type, check for public methods/properties/fields
        if current_type and current_type["kind"] in ("class", "struct", "interface"):
            # Simple heuristic for public/protected members: public/protected keyword, and not containing class/struct/interface/enum
            if any(k in stripped for k in ("public ", "protected ", "internal ")):
                # Skip type declaration lines that might match
                if not any(k in stripped for k in ("class ", "struct ", "interface ", "enum ")):
                    comments = "\n".join(comment_buffer)
                    
                    decl = stripped
                    if decl.endswith('{'):
                        decl = decl[:-1].strip()
                    if decl.endswith(';'):
                        decl = decl[:-1].strip()
                        
                    current_type["members"].append({
                        "line": i,
                        "decl": decl,
                        "comments": comments
                    })
                    
        comment_buffer = [] # Reset comment buffer if not used
        
    return current_namespace, extracted

def main():
    root_dir = r"c:\Users\Admin\Documents\BES\Assets"
    output_file = r"c:\Users\Admin\Documents\BES\scratch\codebase_structure.txt"
    
    with open(output_file, "w", encoding="utf-8") as out:
        out.write("CODEBASE STRUCTURE OVERVIEW\n")
        out.write("===========================\n\n")
        
        for dirpath, dirnames, filenames in os.walk(root_dir):
            # Skip common folders to avoid clutter
            if any(p in dirpath for p in ("Library", "Temp", "packages")):
                continue
                
            for filename in filenames:
                if filename.endswith(".cs"):
                    full_path = os.path.join(dirpath, filename)
                    rel_path = os.path.relpath(full_path, r"c:\Users\Admin\Documents\BES")
                    
                    namespace, types = extract_cs_info(full_path)
                    
                    if not types:
                        continue
                        
                    out.write(f"File: {rel_path}\n")
                    out.write(f"Namespace: {namespace}\n")
                    out.write("-" * len(rel_path) + "\n")
                    
                    for t in types:
                        if t["comments"]:
                            out.write(f"  {t['comments']}\n")
                        out.write(f"  Line {t['line']}: {t['decl']}\n")
                        
                        for m in t["members"]:
                            # Filter member details a bit to avoid massive outputs, only show signatures
                            # e.g., print public methods/properties/fields
                            if m["comments"]:
                                out.write(f"    {m['comments']}\n")
                            out.write(f"    Line {m['line']}: {m['decl']}\n")
                        out.write("\n")
                    out.write("\n" + "="*80 + "\n\n")
                    
    print(f"Structure extracted successfully to {output_file}")

if __name__ == "__main__":
    main()
