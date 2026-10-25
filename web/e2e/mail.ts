import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

export const MAIL_DIRECTORY = 'e2e/.mail';

function decodeQuotedPrintable(body: string): string {
  const bytes = body
    .replace(/=\r?\n/g, '')
    .replace(/=([0-9A-F]{2})/gi, (_, hex: string) => String.fromCharCode(Number.parseInt(hex, 16)));
  return Buffer.from(bytes, 'latin1').toString('utf8');
}

export function decodeEml(raw: string): { readonly headers: string; readonly body: string } {
  const separator = /\r?\n\r?\n/.exec(raw);
  const headers = separator ? raw.slice(0, separator.index) : raw;
  const body = separator ? raw.slice(separator.index + separator[0].length) : '';
  const encoding = /^content-transfer-encoding:\s*([\w-]+)/im.exec(headers)?.[1]?.toLowerCase();
  if (encoding === 'base64') {
    return { headers, body: Buffer.from(body.replace(/\s+/g, ''), 'base64').toString('utf8') };
  }

  return { headers, body: encoding === 'quoted-printable' ? decodeQuotedPrintable(body) : body };
}

export function latestMailTo(email: string): string | null {
  if (!existsSync(MAIL_DIRECTORY)) {
    return null;
  }

  const sentTo = (headers: string) =>
    headers
      .split(/\r?\n/)
      .some((line) => line.toLowerCase().startsWith('to:') && line.includes(email));
  const found = readdirSync(MAIL_DIRECTORY)
    .filter((name) => name.endsWith('.eml'))
    .map((name) => decodeEml(readFileSync(join(MAIL_DIRECTORY, name), 'utf8')))
    .filter((message) => sentTo(message.headers));
  return found.at(-1)?.body ?? null;
}
