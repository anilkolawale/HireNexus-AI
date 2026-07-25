/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: { port: 5173 },
  build: {
    chunkSizeWarningLimit: 600,
    rollupOptions: {
      output: {
        manualChunks: {
          // Core React + routing
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          // State management
          'vendor-state': ['@reduxjs/toolkit', 'react-redux', '@tanstack/react-query'],
          // Animation
          'vendor-motion': ['framer-motion'],
          // Charts
          'vendor-charts': ['recharts'],
          // UI icons
          'vendor-icons': ['lucide-react'],
          // SignalR (large)
          'vendor-signalr': ['@microsoft/signalr'],
        },
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: true,
  },
})
