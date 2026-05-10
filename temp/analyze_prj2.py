import olefile, os

dir_path = r'D:\Program Files\进度计划编制V6\sample'
files = os.listdir(dir_path)
prj_path = os.path.join(dir_path, files[0])
ole = olefile.OleFileIO(prj_path)
data = ole.openstream('Contents').read()
text = data.decode('gb2312', errors='ignore')

# Print all section headers
import re
headers = re.findall(r'C[A-Z][a-zA-Z]+[\d]*', text)
print("=== Section headers found ===")
seen = set()
for h in headers:
    if h not in seen:
        seen.add(h)
        print(f"  {h}")

# Find readable Chinese text segments
print("\n=== Chinese text segments ===")
chunks = re.findall(r'[\u4e00-\u9fff]{2,}', text)
for c in chunks[:60]:
    print(f"  {c}")

print("\n=== First 2000 chars ===")
print(repr(text[:2000]))
