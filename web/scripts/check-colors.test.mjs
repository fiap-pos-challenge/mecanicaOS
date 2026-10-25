import assert from 'node:assert/strict';
import { test } from 'node:test';
import { findColorLiterals } from './check-colors.mjs';

test('flags hex and color functions in stylesheets', () => {
  const source =
    '.a { color: #fff; }\n.b { background: rgb(0 0 0 / 50%); }\n.c { color: var(--fg); }';
  assert.deepEqual(findColorLiterals(source, '.scss'), [1, 2]);
});

test('flags named colors in color properties only', () => {
  const source = '.a { color: white; }\n.b { display: block; }\n.c { border: 1px solid red; }';
  assert.deepEqual(findColorLiterals(source, '.scss'), [1, 3]);
});

test('ignores token references, interpolation and template ids', () => {
  const source = '.a { color: var(--accent); }\n#{$selector} { color: currentcolor; }';
  assert.deepEqual(findColorLiterals(source, '.scss'), []);
  assert.deepEqual(findColorLiterals('<input #search id="x" />', '.html'), []);
});

test('flags inline colors in html attributes', () => {
  assert.deepEqual(findColorLiterals('<svg fill="#0b7a99"></svg>', '.html'), [1]);
});

test('flags color literals in TypeScript strings', () => {
  assert.deepEqual(findColorLiterals("const color = '#ff0000';\nconst ok = 'abc';", '.ts'), [1]);
});
