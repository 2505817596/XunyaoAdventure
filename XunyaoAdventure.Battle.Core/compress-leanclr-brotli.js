const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

const binDir = process.argv[2];
if (!binDir) {
  console.error("Usage: node compress-leanclr-brotli.js <laya-bin-dir>");
  process.exit(1);
}

const files = fs.readdirSync(binDir)
  .filter((name) => name.endsWith(".bytes") || name === "lean.js" || name === "lean.wasm")
  .map((name) => path.join(binDir, name));

for (const file of files) {
  const input = fs.readFileSync(file);
  const output = zlib.brotliCompressSync(input, {
    params: {
      [zlib.constants.BROTLI_PARAM_QUALITY]: 11,
      [zlib.constants.BROTLI_PARAM_SIZE_HINT]: input.length,
    },
  });

  fs.writeFileSync(`${file}.br`, output);
  console.log(`Brotli ${path.basename(file)}: ${input.length} -> ${output.length} bytes`);
}
