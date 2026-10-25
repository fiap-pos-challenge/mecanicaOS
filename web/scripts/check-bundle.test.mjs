import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { checkBundle, findFakeMarkers } from './check-bundle.mjs';

test('finds the fake API markers in a script', () => {
  assert.deepEqual(findFakeMarkers('const a="mecanicaos-fake-api";const t="fake-token.x"'), [
    'mecanicaos-fake-api',
    'fake-token.',
  ]);
  assert.deepEqual(findFakeMarkers('const a="Oficina";'), []);
});

test('scans every script of a bundle folder, including subfolders', () => {
  const directory = mkdtempSync(join(tmpdir(), 'bundle-'));
  try {
    mkdirSync(join(directory, 'nested'));
    writeFileSync(join(directory, 'main.js'), 'const ok = 1;');
    writeFileSync(join(directory, 'nested', 'chunk.js'), 'const d = "convite-demo";');
    writeFileSync(join(directory, 'styles.css'), '.a{content:"convite-demo"}');
    assert.equal(checkBundle(directory).length, 1);
    assert.match(checkBundle(directory)[0] ?? '', /chunk\.js: convite-demo$/);
  } finally {
    rmSync(directory, { recursive: true, force: true });
  }
});
