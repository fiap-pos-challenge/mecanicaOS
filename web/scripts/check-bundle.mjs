import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { pathToFileURL } from 'node:url';

export const FAKE_MARKERS = [
  'mecanicaos-fake-api',
  'fake-token.',
  'convite-demo',
  'Transportes Rota Sul Ltda',
];

export function findFakeMarkers(content) {
  return FAKE_MARKERS.filter((marker) => content.includes(marker));
}

function listScripts(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    if (statSync(path).isDirectory()) {
      return listScripts(path);
    }

    return path.endsWith('.js') ? [path] : [];
  });
}

export function checkBundle(directory) {
  return listScripts(directory).flatMap((file) =>
    findFakeMarkers(readFileSync(file, 'utf8')).map(
      (marker) => `${relative(process.cwd(), file)}: ${marker}`,
    ),
  );
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const directory = process.argv[2] ?? 'dist/oficina-web/browser';
  const expectFake = process.argv.includes('--expect-fake');
  const found = checkBundle(directory);
  if (expectFake && found.length === 0) {
    process.stderr.write(`The demo bundle in ${directory} does not contain the fake API.\n`);
    process.exit(1);
  }

  if (!expectFake && found.length > 0) {
    process.stderr.write(`The production bundle contains fake API code:\n${found.join('\n')}\n`);
    process.exit(1);
  }

  process.stdout.write(
    expectFake
      ? `Demo bundle in ${directory} contains the fake API, as expected.\n`
      : `No fake API code in ${directory}.\n`,
  );
}
