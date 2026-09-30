import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

const ui = path.resolve(__dirname, '../hotel-ui/src')
export default defineConfig({
  plugins: [react()],
  resolve: { alias: { 'hotel-ui': ui }, dedupe: ['react', 'react-dom'] },
  server: { port: 5175, fs: { allow: ['..'] } },
})
