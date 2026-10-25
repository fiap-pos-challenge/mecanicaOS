import { hasOnly, onlyDigits } from './digits';

export type DocumentKind = 'Cpf' | 'Cnpj';

const cnpjFirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
const cnpjSecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

function digitAt(digits: string, index: number): number {
  return Number(digits.charAt(index));
}

function cpfCheckDigit(digits: string, length: number): number {
  let sum = 0;
  for (let index = 0; index < length; index++) {
    sum += digitAt(digits, index) * (length + 1 - index);
  }

  return ((sum * 10) % 11) % 10;
}

function cnpjCheckDigit(digits: string, weights: readonly number[]): number {
  const remainder =
    weights.reduce((sum, weight, index) => sum + digitAt(digits, index) * weight, 0) % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

function isRepeated(digits: string): boolean {
  return new Set(digits).size === 1;
}

export function isValidCpf(value: string): boolean {
  const digits = onlyDigits(value);
  return (
    digits.length === 11 &&
    !isRepeated(digits) &&
    digitAt(digits, 9) === cpfCheckDigit(digits, 9) &&
    digitAt(digits, 10) === cpfCheckDigit(digits, 10)
  );
}

export function isValidCnpj(value: string): boolean {
  const digits = onlyDigits(value);
  return (
    digits.length === 14 &&
    !isRepeated(digits) &&
    digitAt(digits, 12) === cnpjCheckDigit(digits, cnpjFirstWeights) &&
    digitAt(digits, 13) === cnpjCheckDigit(digits, cnpjSecondWeights)
  );
}

export function documentKindOf(value: string): DocumentKind | null {
  if (!hasOnly(value, /[.\-/ ]/)) {
    return null;
  }

  if (isValidCpf(value)) {
    return 'Cpf';
  }

  return isValidCnpj(value) ? 'Cnpj' : null;
}

export function formatDocument(value: string): string {
  const digits = onlyDigits(value).slice(0, 14);
  if (digits.length <= 11) {
    return digits
      .replace(/^(\d{3})(\d)/, '$1.$2')
      .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
      .replace(/\.(\d{3})(\d{1,2})$/, '.$1-$2');
  }

  return digits
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d{1,2})$/, '$1-$2');
}
