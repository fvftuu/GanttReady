import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'happy-dom',
    include: ['tests/**/*.test.ts'],
    exclude: ['node_modules', 'dist'],
    coverage: {
      provider: 'v8',
      include: [
        'core/**/*.ts',
        'render/**/*.ts',
        'interaction/**/*.ts',
        'utils/**/*.ts',
      ],
      exclude: [
        'index.ts',
        'gantt/**/*.ts',
        'charts/**/*.ts',
        'storage/**/*.ts',
        'tests/**/*.ts',
      ],
      reporter: ['text', 'lcov'],
    },
  },
});
