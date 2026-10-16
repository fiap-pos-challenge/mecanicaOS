import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export const SECRET_PATTERNS = [
  { name: 'private key', pattern: /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/ },
  { name: 'AWS access key', pattern: /\bAKIA[0-9A-Z]{16}\b/ },
  { name: 'GitHub token', pattern: /\bgh[pousr]_[A-Za-z0-9]{36,}\b/ },
  { name: 'Slack token', pattern: /\bxox[abprs]-[A-Za-z0-9-]{10,}\b/ },
  { name: 'Google API key', pattern: /\bAIza[0-9A-Za-z_-]{35}\b/ },
  { name: 'Stripe live key', pattern: /\b[sr]k_live_[0-9A-Za-z]{16,}\b/ },
  {
    name: 'signed JWT',
    pattern: /\beyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b/,
  },
];

const SKIPPED =
  /(^|\/)(node_modules|dist|coverage|playwright-report|test-results|\.angular)\/|package-lock\.json$|\.(png|jpe?g|ico|woff2?|pdf|xlsx)$/;

export function findSecrets(content) {
  return SECRET_PATTERNS.filter(({ pattern }) => pattern.test(content)).map(({ name }) => name);
}

export function listCandidateFiles() {
  const output = execFileSync('git', ['ls-files', '--cached', '--others', '--exclude-standard'], {
    encoding: 'utf8',
  });
  return output
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line !== '' && !SKIPPED.test(line));
}

export function checkSecrets(files) {
  return files.flatMap((file) => {
    try {
      return findSecrets(readFileSync(file, 'utf8')).map((name) => `${file}: ${name}`);
    } catch {
      return [];
    }
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const files = listCandidateFiles();
  const found = checkSecrets(files);
  if (found.length > 0) {
    process.stderr.write(`Possible secrets found:\n${found.join('\n')}\n`);
    process.exit(1);
  }

  process.stdout.write(`No secrets found in ${files.length} files.\n`);
}
