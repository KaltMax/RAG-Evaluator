import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    alias: {
      // Visual Studio's Vitest adapter still imports the removed 'vitest/runners'
      // entry point; see vs-test-adapter/vitest-runners-shim.js.
      'vitest/runners': fileURLToPath(
        new URL('./vs-test-adapter/vitest-runners-shim.js', import.meta.url),
      ),
    },
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
      // SignalR hub for real-time job notifications (WebSocket)
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
})
