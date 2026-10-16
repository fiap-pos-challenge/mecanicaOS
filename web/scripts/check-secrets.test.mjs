import assert from 'node:assert/strict';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { checkSecrets, findSecrets } from './check-secrets.mjs';

test('flags common secret formats', () => {
  assert.deepEqual(findSecrets(`key = ${'AKIA'}ABCDEFGHIJKLMNOP`), ['AWS access key']);
  assert.deepEqual(findSecrets(`-----BEGIN ${'PRIVATE'} KEY-----`), ['private key']);
  assert.deepEqual(findSecrets(`t = ghp_${'a'.repeat(36)}`), ['GitHub token']);
  assert.deepEqual(findSecrets('const token = "fake-token.admin";'), []);
});

test('reports the file of each finding and ignores unreadable paths', () => {
  const directory = mkdtempSync(join(tmpdir(), 'secrets-'));
  try {
    const file = join(directory, 'config.ts');
    writeFileSync(file, `const key = 'sk_live_${'x'.repeat(20)}';`);
    assert.deepEqual(checkSecrets([file, join(directory, 'missing.ts')]), [
      `${file}: Stripe live key`,
    ]);
  } finally {
    rmSync(directory, { recursive: true, force: true });
  }
});
