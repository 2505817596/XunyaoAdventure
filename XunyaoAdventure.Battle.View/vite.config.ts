import { defineConfig } from "vite";
import { resolve } from "node:path";

// https://vite.dev/config/
export default defineConfig({
  base: "./",
  build: {
    outDir: "../XunyaoAdventure/wwwroot/battle-client",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        "battle-view": resolve(__dirname, "src/main.ts"),
      },
      preserveEntrySignatures: "strict",
      output: {
        entryFileNames: "assets/battle-view.js",
      },
    },
  },
});
