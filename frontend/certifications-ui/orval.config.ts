import { defineConfig } from 'orval';

export default defineConfig({
  certificationsApi: {
    input: '../../openapi/certifications-v1.json',
    output: {
      target: 'src/app/core/api/generated',
      client: 'angular',
      mode: 'tags-split',
      clean: true,
      mock: false,
      formatter: 'prettier',
      tsconfig: './tsconfig.app.json',
    },
  },
});
