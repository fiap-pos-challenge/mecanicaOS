// @ts-check
// Lint rules for the web app. Security rules make the build fail on unsafe DOM/storage APIs.
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const globals = require('globals');

const forbiddenSyntax = [
  {
    selector: 'MemberExpression[property.name=/^(innerHTML|outerHTML)$/]',
    message: 'innerHTML/outerHTML are forbidden: render through Angular templates.',
  },
  {
    selector: "CallExpression[callee.property.name='insertAdjacentHTML']",
    message: 'insertAdjacentHTML is forbidden.',
  },
  {
    selector: 'MemberExpression[property.name=/^bypassSecurityTrust/]',
    message: 'bypassSecurityTrust* is forbidden: it disables Angular sanitization.',
  },
  {
    selector: "CallExpression[callee.object.name='document'][callee.property.name=/^write(ln)?$/]",
    message: 'document.write is forbidden.',
  },
];

const forbiddenStorage = ['localStorage', 'sessionStorage'].flatMap((name) => [
  {
    object: 'window',
    property: name,
    message: `${name} is forbidden: tokens and personal data stay in memory.`,
  },
  {
    object: 'globalThis',
    property: name,
    message: `${name} is forbidden: tokens and personal data stay in memory.`,
  },
]);

module.exports = defineConfig([
  {
    ignores: [
      'dist/**',
      'coverage/**',
      'playwright-report/**',
      'test-results/**',
      '.angular/**',
      'src/app/core/api/generated/**',
      'src/mock-worker/**',
    ],
  },
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.strict,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: ['app', 'ui'], style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        [
          { type: 'element', prefix: ['app', 'ui'], style: 'kebab-case' },
          { type: 'attribute', prefix: ['app', 'ui'], style: 'camelCase' },
        ],
      ],
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
      '@angular-eslint/prefer-signals': 'error',
      '@angular-eslint/no-developer-preview': 'off',
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-extraneous-class': ['error', { allowWithDecorator: true }],
      '@typescript-eslint/no-non-null-assertion': 'error',
      'no-console': 'error',
      'no-eval': 'error',
      'no-implied-eval': 'error',
      'no-new-func': 'error',
      'no-restricted-globals': [
        'error',
        {
          name: 'localStorage',
          message: 'localStorage is forbidden: tokens and personal data stay in memory.',
        },
        {
          name: 'sessionStorage',
          message: 'sessionStorage is forbidden: tokens and personal data stay in memory.',
        },
      ],
      'no-restricted-properties': ['error', ...forbiddenStorage],
      'no-restricted-syntax': ['error', ...forbiddenSyntax],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      '@angular-eslint/template/no-any': 'error',
      'no-restricted-syntax': [
        'error',
        {
          selector: 'BoundAttribute[name=/^(innerHTML|outerHTML)$/]',
          message: '[innerHTML] is forbidden: render text through interpolation.',
        },
      ],
    },
  },
  {
    files: [
      'src/app/core/preferences/ui-preferences.ts',
      'src/app/core/preferences/ui-preferences.spec.ts',
    ],
    rules: {
      'no-restricted-properties': 'off',
    },
  },
  {
    files: ['scripts/**/*.mjs'],
    extends: [eslint.configs.recommended],
    languageOptions: { globals: globals.node },
  },
]);
