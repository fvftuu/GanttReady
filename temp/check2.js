const fs = require('fs');
const c = fs.readFileSync('I:\\NetPlan\\src\\NetPlan.Server\\wwwroot\\js\\netplan.js', 'utf8');
const lines = c.split('\n');

let out = '';
out += '=== Lines 1402-1425 ===\n';
for (let i = 1401; i < 1425; i++) {
    let depth = 0;
    for (let j = 0; j <= i; j++) {
        depth += (lines[j].match(/\{/g) || []).length - (lines[j].match(/\}/g) || []).length;
    }
    out += (i+1) + ' [depth=' + depth + '] ' + lines[i] + '\n';
}

out += '\n=== Lines 1640-1650 (end of renderNetwork) ===\n';
for (let i = 1639; i < 1650; i++) {
    let depth = 0;
    for (let j = 0; j <= i; j++) {
        depth += (lines[j].match(/\{/g) || []).length - (lines[j].match(/\}/g) || []).length;
    }
    out += (i+1) + ' [depth=' + depth + '] ' + lines[i] + '\n';
}

// Check the _netUpdateArrows function
out += '\n=== _netUpdateArrows (checking ex calculation) ===\n';
for (let i = 1729; i < 1760; i++) {
    out += (i+1) + ': ' + lines[i] + '\n';
}

// Check last 5 lines
out += '\n=== Last 5 lines ===\n';
for (let i = lines.length - 5; i < lines.length; i++) {
    out += (i+1) + ': ' + lines[i] + '\n';
}

// Check renderNetwork closing
out += '\n=== Searching for renderNetwork closing ===\n';
for (let i = 1644; i < 1652; i++) {
    out += (i+1) + ': ' + lines[i] + '\n';
}

fs.writeFileSync('I:\\NetPlan\\temp\\check_result.txt', out);
