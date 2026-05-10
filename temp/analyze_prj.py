import olefile, os, re

dir_path = r'D:\Program Files\进度计划编制V6\sample'
files = os.listdir(dir_path)
prj_path = os.path.join(dir_path, files[0])
ole = olefile.OleFileIO(prj_path)
data = ole.openstream('Contents').read()
text = data.decode('gb2312', errors='ignore')

# Extract task names from CCodeValue
print("=== CODE VALUES (Task Categories) ===")
cats = re.findall(r'CCodeValue(.*?)(?=CCode\b|CWork|\x00\x00)', text, re.DOTALL)
for cat in cats[:1]:
    names = re.findall(r'[\u4e00-\u9fff]{2,}', cat)
    for n in names:
        print("  ", n)

# Extract CWork tasks  
print("\n=== TASKS (CWork entries) ===")
works = list(re.finditer(r'CWork(\d+)(.*?)(?=CWork\d|CLabour|CRelat|CNode|CFilter|CGroup|CSort|$)', text, re.DOTALL))
print(f"Total tasks: {len(works)}")
for m in works[:10]:
    wid = m.group(1)
    chunk = m.group(2)
    names = re.findall(r'[\u4e00-\u9fff]{2,}', chunk)
    print(f"  CWork{wid}: {names[:3] if names else '(binary)'}")

# Extract relations
print("\n=== RELATIONS ===")
rels = re.findall(r'CRelatRec(.*?)(?=CRelat|CWork|CNode|CFilter|CGroup|$)', text, re.DOTALL)
print(f"Total relations: {len(rels)}")
# Try to decode relation pairs
for r in rels[:15]:
    # Relations usually contain pairs of work IDs
    ids = re.findall(r'\d{1,3}', r)
    if ids:
        pairs = [(ids[i], ids[i+1]) for i in range(0, len(ids)-1, 2)]
        print(f"  {pairs[:3]}")
