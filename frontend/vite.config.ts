import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],
    build: {
      outDir: 'dist',
      sourcemap: mode !== 'production',
      rollupOptions: {
        output: {
          manualChunks: (id) => {
            if (id.includes('node_modules/react') || id.includes('node_modules/react-dom') || id.includes('node_modules/react-router-dom')) return 'vendor';
            if (id.includes('node_modules/@tanstack')) return 'query';
            if (id.includes('node_modules/recharts')) return 'charts';
            if (id.includes('node_modules/date-fns')) return 'ui';
          }
        }
      },
      // Warn if any chunk exceeds 500kB
      chunkSizeWarningLimit: 500,
    },
    server: {
      port: 3000,
      proxy: {
        '/api': {
          target: env.VITE_API_URL?.replace('/api/v1', '') || 'http://localhost:5000',
          changeOrigin: true,
        }
      }
    },
    define: {
      __APP_VERSION__: JSON.stringify(process.env.npm_package_version),
    }
  };
});