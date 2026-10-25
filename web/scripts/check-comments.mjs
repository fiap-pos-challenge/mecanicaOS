import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join, relative } from 'node:path';
import { pathToFileURL } from 'node:url';

const checkedExtensions = new Set(['.ts', '.html', '.scss']);
const ignoredDirectories = new Set([
  'node_modules',
  'dist',
  'coverage',
  'playwright-report',
  'test-results',
  '.angular',
  'generated',
]);

export function findComments(source, extension) {
  if (extension === '.html') {
    return findHtmlComments(source);
  }

  return findCodeComments(source, extension === '.scss');
}

function findHtmlComments(source) {
  const lines = [];
  let index = source.indexOf('<!--');
  while (index !== -1) {
    lines.push(lineAt(source, index));
    index = source.indexOf('<!--', index + 4);
  }

  return lines;
}

function findCodeComments(source, isStylesheet) {
  const lines = [];
  const templateDepth = [];
  let quote = null;
  let inUrl = false;

  for (let index = 0; index < source.length; index++) {
    const char = source[index];
    const next = source[index + 1];

    if (quote) {
      if (char === '\\') {
        index++;
      } else if (quote === '`' && char === '$' && next === '{') {
        templateDepth.push(0);
        quote = null;
        index++;
      } else if (char === quote) {
        quote = null;
      }
      continue;
    }

    if (inUrl) {
      inUrl = char !== ')';
      continue;
    }

    if (char === "'" || char === '"' || (!isStylesheet && char === '`')) {
      quote = char;
    } else if (
      isStylesheet &&
      source.startsWith('url(', index) &&
      !/['"]/.test(source[index + 4] ?? '')
    ) {
      inUrl = true;
      index += 3;
    } else if (char === '/' && (next === '/' || next === '*')) {
      lines.push(lineAt(source, index));
      const end = next === '/' ? source.indexOf('\n', index) : source.indexOf('*/', index + 2);
      index = end === -1 ? source.length : end;
    } else if (templateDepth.length > 0 && char === '{') {
      templateDepth[templateDepth.length - 1]++;
    } else if (templateDepth.length > 0 && char === '}') {
      if (templateDepth[templateDepth.length - 1] === 0) {
        templateDepth.pop();
        quote = '`';
      } else {
        templateDepth[templateDepth.length - 1]--;
      }
    }
  }

  return lines;
}

function lineAt(source, index) {
  return source.slice(0, index).split('\n').length;
}

function listFiles(directory) {
  return readdirSync(directory).flatMap((entry) => {
    const path = join(directory, entry);
    if (statSync(path).isDirectory()) {
      return ignoredDirectories.has(entry) ? [] : listFiles(path);
    }

    return checkedExtensions.has(extname(path)) ? [path] : [];
  });
}

export function checkDirectories(directories) {
  return directories.flatMap((directory) =>
    listFiles(directory).flatMap((file) =>
      findComments(readFileSync(file, 'utf8'), extname(file)).map(
        (line) => `${relative(process.cwd(), file)}:${line}`,
      ),
    ),
  );
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const violations = checkDirectories(['src', 'e2e']);
  if (violations.length > 0) {
    process.stderr.write(
      `Comments are not allowed in .ts, .html or .scss files:\n${violations.join('\n')}\n`,
    );
    process.exit(1);
  }

  process.stdout.write('No comments found in .ts, .html or .scss files.\n');
}
