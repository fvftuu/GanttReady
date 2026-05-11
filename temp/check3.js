const fs = require('fs');
const c = fs.readFileSync('I:\\NetPlan\\src\\NetPlan.Server\\wwwroot\\js\\netplan.js', 'utf8');
const lines = c.split('\n');

// Count final brace depth
let depth = 0;
for (let i = 0; i < lines.length; i++) {
    const opens = (lines[i].match(/\{/g) || []).length;
    const closes = (lines[i].match(/\}/g) || []).length;
    depth += opens - closes;
}

console.log('Total lines: ' + lines.length);
console.log('Final brace depth: ' + depth);

// Check around renderNetwork function - does the else block have a proper close?
// Count braces from line 1413 (} else {) to 1417 (end of forEach)... onward
let subDepth = 0;
for (let i = 0; i <= 1413; i++) {
    subDepth += (lines[i].match(/\{/g) || []).length - (lines[i].match(/\}/g) || []).length;
}
console.log('Depth at line 1413: ' + subDepth); // Should be 2 (in function, in if/else)

for (let i = 1414; i < 1422; i++) {
    subDepth += (lines[i].match(/\{/g) || []).length - (lines[i].match(/\}/g) || []).length;
    console.log('Depth at line ' + (i+1) + ': ' + subDepth + ' -> ' + lines[i].trim().substring(0, 60));
}

// Now check if there's a proper `}` between line 1422 and 1423
for (let i = 1422; i < 1425; i++) {
    subDepth += (lines[i].match(/\{/g) || []).length - (lines[i].match(/\}/g) || []).length;
    console.log('Depth at line ' + (i+1) + ': ' + subDepth + ' -> ' + lines[i].trim().substring(0, 60));
}

// Continue to find where the else block closes
for (let i = 1425; i < lines.length; i++) {
    subDepth += (lines[i].match(/\{/g) || []).length - (lines[i].match(/\}/g) || []).length;
    if (subDepth === 1) {
        console.log('Found depth=1 at line ' + (i+1) + ': ' + lines[i].trim().substring(0, 60));
        break;
    }
    if (i > 1700) {
        console.log('Searched to line 1700 without finding depth=1');
        break;
    }
}
