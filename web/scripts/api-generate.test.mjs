import assert from 'node:assert/strict';
import { mkdtempSync, readdirSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { generateApiClient, generatedPath } from './api-generate.mjs';

test('committed API types match the backend OpenAPI snapshot', async () => {
  const output = mkdtempSync(join(tmpdir(), 'oficina-api-'));
  try {
    await generateApiClient(output);
    const files = readdirSync(output).sort();
    assert.deepEqual(readdirSync(generatedPath).sort(), files);
    for (const file of files) {
      assert.equal(
        readFileSync(join(generatedPath, file), 'utf8'),
        readFileSync(join(output, file), 'utf8'),
        `${file} is outdated. Run "npm run api:generate".`,
      );
    }
  } finally {
    rmSync(output, { recursive: true, force: true });
  }
});
