import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const rootDir = fileURLToPath(new URL(".", import.meta.url));

// Dev server + Rollup build for the Trading agent web UI. The Vitest config lives in vitest.config.ts
// (the split avoids the vite 6.x <-> vitest 2.x defineConfig type mismatch). Dev proxies /api -> the
// Trading.Api backend on :5080.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": resolve(rootDir, "src"),
    },
  },
  server: {
    port: 5175,
    strictPort: true,
    proxy: {
      "/api": "http://localhost:5080",
    },
  },
  build: {
    outDir: "dist",
    sourcemap: true,
  },
});
