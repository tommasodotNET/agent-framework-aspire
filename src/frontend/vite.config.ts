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
        '/agent/dotnet': {
          target: process.env.services__dotnetagent__https__0 || process.env.services__dotnetagent__http__0,
          rewrite: (path) => path.replace(/^\/agent\/dotnet/, '/agent'),
        },
        '/agent/python': {
          target: process.env.services__pythonagent__https__0 || process.env.services__pythonagent__http__0,
          rewrite: (path) => path.replace(/^\/agent\/python/, '/agent'),
        },
        '/agent/groupchat': {
          target: process.env.services__dotnetgroupchat__https__0 || process.env.services__dotnetgroupchat__http__0,
          rewrite: (path) => path.replace(/^\/agent\/groupchat/, '/agent'),
        },
        // Legacy support - default to .NET API
        '/agent': {
          target: process.env.services__dotnetagent__http__0,
        },
      },
    },
  });
}