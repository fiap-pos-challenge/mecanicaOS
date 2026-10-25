declare const centsBrand: unique symbol;

export type Cents = number & { readonly [centsBrand]: true };

const formatter = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const amountFormatter = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function cents(value: number): Cents {
  if (!Number.isSafeInteger(value)) {
    throw new RangeError('Cents must be a safe integer.');
  }

  return value as Cents;
}

export function centsFromDecimal(amount: number): Cents {
  if (!Number.isFinite(amount)) {
    throw new RangeError('Amount must be finite.');
  }

  return cents(Math.round(amount * 100));
}

export function centsToDecimal(value: Cents): number {
  return value / 100;
}

export function parseBrl(text: string): Cents | null {
  const normalized = text.replace(/R\$|\s|\u00a0/g, '');
  const match = /^(-?)(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?$/.exec(normalized);
  if (!match) {
    return null;
  }

  const [, sign = '', integer = '0', fraction = ''] = match;
  const total = Number(integer.replace(/\./g, '')) * 100 + Number(fraction.padEnd(2, '0'));
  return cents(sign === '-' ? -total : total);
}

export function formatBrl(value: Cents): string {
  return formatter.format(value / 100);
}

export function formatAmount(value: Cents): string {
  return amountFormatter.format(value / 100);
}

export function addCents(...values: readonly Cents[]): Cents {
  return cents(values.reduce<number>((total, value) => total + value, 0));
}

export function multiplyCents(value: Cents, quantity: number): Cents {
  return cents(Math.round(value * quantity));
}

export function percentOf(value: Cents, basisPoints: number): Cents {
  return cents(Math.round((value * basisPoints) / 10_000));
}
