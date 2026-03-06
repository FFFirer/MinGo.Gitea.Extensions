import { defineConfig } from 'vite'
import { resolve } from 'path'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [tailwindcss()],
  resolve: {
    alias: {
      '@': resolve(__dirname, './BlazorApp')
    }
  },
  server: {
    port: 5065,
    strictPort: true,
    cors: true,
    hmr: {
      port: 5173
    }
  },
  build: {
    outDir: './BlazorApp/wwwroot',
    emptyOutDir: false,
    manifest: true,
    rollupOptions: {
      input: './index.css',
      output: {
        entryFileNames: '[name].css',
        assetFileNames: '[name][extname]'
      }
    }
  }
})