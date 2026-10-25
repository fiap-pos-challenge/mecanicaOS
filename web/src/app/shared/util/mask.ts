import { onlyDigits } from './digits';
import { formatDocument } from './document';
import { normalizePlate } from './plate';

const hidden = '•';

function hide(text: string): string {
  return text.replace(/\d/g, hidden);
}

export function maskDocument(value: string): string {
  const digits = onlyDigits(value);
  const formatted = formatDocument(digits);
  if (digits.length === 11) {
    return `${hide(formatted.slice(0, 4))}${formatted.slice(4, 11)}${hide(formatted.slice(11))}`;
  }

  return `${formatted.slice(0, 6)}${hide(formatted.slice(6, 15))}${formatted.slice(15)}`;
}

export function maskEmail(value: string): string {
  const [user = '', domain = ''] = value.split('@');
  if (!domain) {
    return hidden.repeat(3);
  }

  return `${user.charAt(0)}${hidden.repeat(3)}@${domain}`;
}

export function maskPhone(value: string): string {
  const digits = onlyDigits(value);
  return digits.length < 4
    ? hidden.repeat(4)
    : `${hidden.repeat(digits.length - 4)}${digits.slice(-4)}`;
}

export function maskPlate(value: string): string {
  const plate = normalizePlate(value);
  return `${plate.slice(0, 3)}${hidden.repeat(Math.max(0, plate.length - 3))}`;
}

export function maskName(value: string): string {
  const [first = '', ...rest] = value.trim().split(/\s+/);
  return rest.length === 0
    ? first
    : `${first} ${rest.map((part) => `${part.charAt(0)}.`).join(' ')}`;
}
