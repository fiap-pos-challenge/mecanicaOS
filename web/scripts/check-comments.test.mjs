import assert from 'node:assert/strict';
import { test } from 'node:test';
import { findComments } from './check-comments.mjs';

test('finds line and block comments in TypeScript', () => {
  assert.deepEqual(findComments('const a = 1;\n// note\nconst b = 2; /* x */', '.ts'), [2, 3]);
});

test('ignores comment markers inside strings and template literals', () => {
  const source =
    "const url = 'https://example.com';\nconst t = `a ${'//'} // b`;\nconst c = \"/* no */\";";
  assert.deepEqual(findComments(source, '.ts'), []);
});

test('finds a comment right after an interpolation closes', () => {
  assert.deepEqual(findComments('const t = `a ${b}`; // c', '.ts'), [1]);
});

test('ignores unquoted urls in stylesheets but finds scss comments', () => {
  const source = '.a { background: url(https://example.com/x.png); }\n// note\n.b { color: red; }';
  assert.deepEqual(findComments(source, '.scss'), [2]);
});

test('finds html comments only', () => {
  assert.deepEqual(findComments('<a href="https://x.dev">x</a>\n<!-- note -->', '.html'), [2]);
});
