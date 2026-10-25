import { createClient } from '@hey-api/openapi-ts';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join } from 'node:path';

const webRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

export const snapshotPath = join(
  webRoot,
  '..',
  'tests',
  'Oficina.ContractTests',
  'OpenApi',
  'Snapshots',
  'openapi.json',
);
export const generatedPath = join(webRoot, 'src', 'app', 'core', 'api', 'generated');

export async function generateApiClient(output = generatedPath) {
  await createClient({
    input: snapshotPath,
    output: { path: output, postProcess: [] },
    logs: { level: 'silent' },
    plugins: ['@hey-api/typescript'],
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await generateApiClient();
  process.stdout.write(`API client generated in ${generatedPath}\n`);
}
