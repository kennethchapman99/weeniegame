import { defineConfig } from 'vitest/config';
import { viteSingleFile } from 'vite-plugin-singlefile';

// Single self-contained artifact: all JS/CSS/assets inlined into index.html so the
// build runs from file:// and inside constrained webviews with ZERO runtime network
// requests (hard rule — see CLAUDE.md). assetsInlineLimit is set absurdly high so any
// future image/font asset is base64-inlined at build time, never fetched.
export default defineConfig({
  plugins: [viteSingleFile()],
  // Fixed dev port so the Tauri desktop shell (M11) reliably attaches to `npm run dev`.
  server: { port: 5173, strictPort: true },
  build: {
    target: 'es2022',
    assetsInlineLimit: 100_000_000,
    cssCodeSplit: false,
    rollupOptions: {
      output: { inlineDynamicImports: true },
    },
  },
  test: {
    globals: true,
    environment: 'node',
    include: ['tests/**/*.test.ts'],
  },
});
