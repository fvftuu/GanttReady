import * as esbuild from 'esbuild';
import { readFileSync, writeFileSync } from 'fs';

await esbuild.build({
  entryPoints: ['dist/index.js'],
  bundle: true,
  outfile: '../netplan-bundle.js',
  format: 'iife',
  globalName: 'NetPlan',
  minify: true,
  sourcemap: false,
});

var bundle = readFileSync('../netplan-bundle.js', 'utf-8');

// 生成最终的 netplan.js: Node.js shim + IIFE + CommonJS 导出
var header =
  '// == NetPlan TS Module ==\n' +
  '// Node.js compatibility shim for testing\n' +
  'if (typeof window === "undefined") {\n' +
  '  global.window = global;\n' +
  '  global.document = { createElementNS: function(){return {}}, createElement: function(){return {}}, querySelector: function(){return null}, body: { appendChild: function(){}, removeChild: function(){} } };\n' +
  '  global.navigator = { userAgent: "node" };\n' +
  '}\n\n';

var cjsExport =
  '\nif (typeof module !== "undefined" && module.exports) {\n' +
  '  module.exports = NetPlan;\n' +
  '}\n';

writeFileSync('../netplan.js', header + bundle + cjsExport);
writeFileSync('../netplan-bundle.js', ''); // cleanup
console.log('Written', header.length + bundle.length + cjsExport.length, 'bytes');
