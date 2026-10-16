import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join, relative, sep } from 'node:path';
import { pathToFileURL } from 'node:url';

const tokensFile = join('src', 'styles', 'tokens.scss');
const checkedExtensions = new Set(['.ts', '.html', '.scss']);
const namedColors = [
  'white',
  'black',
  'red',
  'green',
  'blue',
  'yellow',
  'orange',
  'purple',
  'gray',
  'grey',
  'pink',
  'brown',
  'cyan',
  'magenta',
];
const functionPattern = /\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch)\(/i;
const hexPattern = /(?<![&\w$])#(?:[0-9a-f]{8}|[0-9a-f]{6}|[0-9a-f]{3,4})\b(?![{\w-])/i;
const namedPattern = new RegExp(
  String.raw`(?:color|background|border|fill|stroke|outline|shadow)[\w-]*\s*:[^;{}]*\b(?:${namedColors.join('|')})\b`,
  'i',
);
const htmlStylePattern =
  /\b(?:style|fill|stroke|color)\s*=\s*"[^"]*(?:#[0-9a-f]{3,8}\b|rgba?\(|hsla?\()/i;

export function findColorLiterals(source, extension) {
  return source.split('\n').flatMap((line, index) => {
    const found =
      extension === '.html'
        ? htmlStylePattern.test(line)
        : functionPattern.test(line) ||
          hexPattern.test(line) ||
          (extension === '.scss' && namedPattern.test(line));
    return found ? [index + 1] : [];
  });
}

function listFiles(directory) {
  return readdirSync(directory).flatMap((entry) => {
    const path = join(directory, entry);
    if (statSync(path).isDirectory()) {
      return listFiles(path);
    }

    return checkedExtensions.has(extname(path)) &&
      !path.endsWith(`${sep}${tokensFile}`) &&
      path !== tokensFile
      ? [path]
      : [];
  });
}

export function checkColors(directory) {
  return listFiles(directory).flatMap((file) =>
    findColorLiterals(readFileSync(file, 'utf8'), extname(file)).map(
      (line) => `${relative(process.cwd(), file)}:${line}`,
    ),
  );
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const violations = checkColors('src').filter((violation) => !violation.startsWith(tokensFile));
  if (violations.length > 0) {
    process.stderr.write(
      `Color literals are only allowed in ${tokensFile}:\n${violations.join('\n')}\n`,
    );
    process.exit(1);
  }

  process.stdout.write(`No color literals outside ${tokensFile}.\n`);
}
