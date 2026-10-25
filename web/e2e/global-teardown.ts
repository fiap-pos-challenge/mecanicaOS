import { spawnSync } from 'node:child_process';
import { rmSync } from 'node:fs';

export default function globalTeardown(): void {
  if (process.env['E2E_KEEP_STACK']) {
    return;
  }

  spawnSync(
    'docker',
    [
      'compose',
      '--env-file',
      '../.env',
      '-f',
      'e2e/docker-compose.e2e.yml',
      'down',
      '-v',
      '--remove-orphans',
    ],
    { stdio: 'ignore' },
  );
  rmSync('e2e/.mail', { recursive: true, force: true });
}
