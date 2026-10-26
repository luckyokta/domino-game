import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Where the ASP.NET Core API listens in development (see launchSettings.json).
const API_TARGET = process.env.VITE_PROXY_TARGET || 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // The browser talks only to the Vite server; /api and /hubs are forwarded to the API.
    // This avoids CORS entirely during development (WebSocket upgrade enabled for SignalR).
    proxy: {
      '/api': { target: API_TARGET, changeOrigin: true },
      '/hubs': { target: API_TARGET, changeOrigin: true, ws: true },
    },
  },
  build: {
    // `npm run build` writes the production site straight into the API's wwwroot,
    // so the API alone can serve the whole app.
    outDir: '../src/DominoGame.Api/wwwroot',
    emptyOutDir: true,
  },
});
