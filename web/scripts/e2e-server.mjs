import { spawnSync } from 'node:child_process';
import { chmodSync, mkdirSync, rmSync } from 'node:fs';

const composeFile = 'e2e/docker-compose.e2e.yml';
const mailDirectory = 'e2e/.mail';
const compose = (args, options = {}) =>
  spawnSync('docker', ['compose', '-f', composeFile, ...args], { stdio: 'inherit', ...options });

const stopStack = () => compose(['down', '-v', '--remove-orphans'], { stdio: 'ignore' });

stopStack();
rmSync(mailDirectory, { recursive: true, force: true });
mkdirSync(mailDirectory, { recursive: true });
chmodSync(mailDirectory, 0o777);
const up = compose(['up', '--build', '--wait', '--wait-timeout', '600']);
if (up.status !== 0) {
  compose(['logs', '--tail', '80']);
  stopStack();
  process.exit(up.status ?? 1);
}

const stop = () => {
  stopStack();
  process.exit(0);
};

process.on('SIGINT', stop);
process.on('SIGTERM', stop);
setInterval(() => undefined, 60_000);
