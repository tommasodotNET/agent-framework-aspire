import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default () => {
  process.env = {...process.env, ...loadEnv('', process.cwd())};

  // import.meta.env.VITE_NAME available here with: process.env.VITE_NAME
  // import.meta.env.VITE_PORT available here with: process.env.VITE_PORT

  return defineConfig({
    plugins: [react()],
    assetsInclude: ['**/*.md'],
    server: {
      port: process.env.PORT,
      proxy: {
        '/agenta2a/dotnet': {
          target: process.env.services__dotnetagent__https__0 || process.env.services__dotnetagent__http__0,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/agenta2a\/dotnet/, '/agenta2a'),
        },
        '/agenta2a/python': {
          target: process.env.services__pythonagent__https__0 || process.env.services__pythonagent__http__0,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/agenta2a\/python/, '/'),
        },
        '/agenta2a/groupchat': {
          target: process.env.services__dotnetgroupchat__https__0 || process.env.services__dotnetgroupchat__http__0,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/agenta2a\/groupchat/, '/agenta2a'),
        },
      },
    },
  });
}