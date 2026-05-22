with open('src/NetPlan.Server/Pages/Project/Analysis.razor', 'r', encoding='utf-8') as f:
    content = f.read()

MARKER = '<div class="modal-content" style="width:640px;">'
old_start = content.find(MARKER)

i = old_start
depth = 0
while i < len(content):
    if content[i:i+5] == '<div ' or content[i:i+3] == '<div':
        depth += 1
        i += 1
    elif content[i:i+6] == '</div>':
        depth -= 1
        i += 6
        if depth == 0:
            old_end = i
            break
    else:
        i += 1

# Read the new HTML from a separate clean file
new_html = open('dialog_new.html', 'r', encoding='utf-8').read()

result = content[:old_start] + new_html + content[old_end:]
with open('src/NetPlan.Server/Pages/Project/Analysis.razor', 'w', encoding='utf-8') as f:
    f.write(result)
print('Done')
