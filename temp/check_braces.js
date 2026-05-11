const fs = require('fs');
const c = fs.readFileSync('I:\\NetPlan\\src\\NetPlan.Server\\wwwroot\\js\\netplan.js', 'utf8');
const lines = c.split('\n');

// Find brace depth changes around the else block (line 1413)
console.log('=== Else block area (lines 1400-1470) ===');
for (let i = 1399; i < 1470; i++) {
    let d = 0;
    for (let j = 0; j <= i; j++) {
        const ln = lines[j];
        d += (ln.match(/\{/g) || []).length - (ln.match(/\}/g) || []).length;
    }
    console.log((i+1) + ': depth=' + d + ' | ' + lines[i].substring(0, 80));
}

// Find where depth returns to 1
console.log('\n=== Depth transitions to 1 ===');
for (let i = 1470; i < lines.length; i++) {
    let d = 0;
    for (let j = 0; j <= i; j++) {
        const ln = lines[j];
        d += (ln.match(/\{/g) || []).length - (ln.match(/\}/g) || []).length;
    }
    if (d === 1) {
        console.log((i+1) + ': depth=1 | ' + lines[i].substring(0, 80));
        break;
    }
}

// Final depth
let total = 0;
lines.forEach(l => total += (l.match(/\{/g) || []).length - (l.match(/\}/g) || []).length);
console.log('\nFinal brace depth: ' + total);
