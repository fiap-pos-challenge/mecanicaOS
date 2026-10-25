import { hasOnly, onlyDigits } from './digits';

export function isValidPhone(value: string): boolean {
  const digits = onlyDigits(value);
  return (
    hasOnly(value, /[()\- ]/) &&
    digits.length >= 10 &&
    digits.length <= 11 &&
    !digits.startsWith('0')
  );
}

export function formatPhone(value: string): string {
  const digits = onlyDigits(value).slice(0, 11);
  if (digits.length <= 2) {
    return digits;
  }

  const area = `(${digits.slice(0, 2)}) `;
  const number = digits.slice(2);
  const split = digits.length === 11 ? 5 : 4;
  return number.length > split
    ? `${area}${number.slice(0, split)}-${number.slice(split)}`
    : `${area}${number}`;
}
