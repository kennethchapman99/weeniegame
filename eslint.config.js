import js from '@eslint/js';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  { ignores: ['dist', 'node_modules', 'prototype', 'starter'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ['src/**/*.ts'],
    rules: {
      // Determinism rule (CLAUDE.md): gameplay randomness must route through the
      // injected seedable rng, never bare Math.random() in src logic.
      'no-restricted-properties': [
        'error',
        {
          object: 'Math',
          property: 'random',
          message: 'Use the injected seedable rng (core/rng.ts), not Math.random(), in src logic.',
        },
      ],
    },
  },
);
