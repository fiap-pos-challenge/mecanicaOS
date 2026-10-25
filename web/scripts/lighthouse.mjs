import { spawn, spawnSync } from 'node:child_process';
import { chromium } from '@playwright/test';
import { launch } from 'chrome-launcher';
import lighthouse from 'lighthouse';

const image = 'oficina-web:lighthouse';
const container = 'oficina-web-lighthouse';
const port = 4302;
const minimumScore = 95;
const pages = ['/login', '/update'];

const docker = (args, options = {}) => spawnSync('docker', args, { stdio: 'inherit', ...options });

async function waitForServer(url, attempts = 60) {
  for (let attempt = 0; attempt < attempts; attempt++) {
    try {
      const response = await fetch(url);
      if (response.ok) {
        return;
      }
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 1000));
    }
  }

  throw new Error(`Server did not start: ${url}`);
}

function stopServer() {
  docker(['rm', '-f', container], { stdio: 'ignore' });
}

stopServer();
if (docker(['build', '-t', image, '.']).status !== 0) {
  process.exit(1);
}

spawn(
  'docker',
  [
    'run',
    '--rm',
    '--name',
    container,
    '-p',
    `${port}:8080`,
    '-e',
    'API_UPSTREAM=http://127.0.0.1:9',
    image,
  ],
  { stdio: 'ignore' },
);

let failed = false;
const chrome = await launch({
  chromePath: chromium.executablePath(),
  chromeFlags: ['--headless=new', '--no-sandbox', '--disable-gpu'],
});

try {
  await waitForServer(`http://localhost:${port}/`);
  for (const path of pages) {
    for (const formFactor of ['desktop', 'mobile']) {
      const result = await lighthouse(`http://localhost:${port}${path}`, {
        port: chrome.port,
        onlyCategories: ['accessibility'],
        formFactor,
        screenEmulation:
          formFactor === 'desktop'
            ? { mobile: false, width: 1350, height: 940, deviceScaleFactor: 1, disabled: false }
            : { mobile: true, width: 390, height: 844, deviceScaleFactor: 3, disabled: false },
        output: 'json',
        logLevel: 'error',
      });
      const score = Math.round((result?.lhr.categories['accessibility']?.score ?? 0) * 100);
      process.stdout.write(`Lighthouse accessibility ${path} (${formFactor}): ${score}\n`);
      if (score < minimumScore) {
        failed = true;
        const audits = Object.values(result?.lhr.audits ?? {}).filter(
          (audit) => audit.score !== null && audit.score < 1 && audit.scoreDisplayMode === 'binary',
        );
        process.stdout.write(`  failing audits: ${audits.map((audit) => audit.id).join(', ')}\n`);
      }
    }
  }
} finally {
  chrome.kill();
  stopServer();
}

if (failed) {
  process.stderr.write(`Accessibility score below ${minimumScore}.\n`);
  process.exit(1);
}
