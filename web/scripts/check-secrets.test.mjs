import assert from 'node:assert/strict';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { checkSecrets, exampleSecrets, findSecrets } from './check-secrets.mjs';

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

test('reads the secret values of the env example and ignores the rest', () => {
  const example = [
    '# comment',
    'JWT_SECRET=dev-only-jwt-secret-value',
    'DEV_ADMIN_EMAIL=admin@oficina.local',
    'SONAR_TOKEN=',
    'SHORT_PASSWORD=abc',
  ].join('\n');
  assert.deepEqual(exampleSecrets(example), [
    { name: 'JWT_SECRET from .env.example', value: 'dev-only-jwt-secret-value' },
  ]);
});

test('flags env example secrets copied into another file but never the env files', () => {
  const directory = mkdtempSync(join(tmpdir(), 'secrets-'));
  try {
    const known = [{ name: 'JWT_SECRET from .env.example', value: 'dev-only-jwt-secret-value' }];
    const leaked = join(directory, 'compose.yml');
    writeFileSync(leaked, 'Jwt__Secret: dev-only-jwt-secret-value');
    const clean = join(directory, 'clean.yml');
    writeFileSync(clean, 'Jwt__Secret: ${JWT_SECRET}');
    assert.deepEqual(checkSecrets([leaked, clean, '.env.example', '.env'], known), [
      `${leaked}: JWT_SECRET from .env.example`,
    ]);
  } finally {
    rmSync(directory, { recursive: true, force: true });
  }
});
