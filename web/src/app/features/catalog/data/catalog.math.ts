import { addCents, cents, Cents } from '../../../shared/util/money';

export const QUANTITY_DECIMALS = 3;

export interface PricedLine {
  readonly unitPrice: Cents;
  readonly quantity: number;
}

export function roundHalfUp(value: number): number {
  const rounded = value < 0 ? -Math.round(-value) : Math.round(value);
  return rounded === 0 ? 0 : rounded;
}

export function lineTotal(line: PricedLine): Cents {
  const thousandths = Math.round(line.quantity * 10 ** QUANTITY_DECIMALS);
  return cents(roundHalfUp((line.unitPrice * thousandths) / 10 ** QUANTITY_DECIMALS));
}

export function itemsTotal(lines: readonly PricedLine[]): Cents {
  return addCents(...lines.map(lineTotal));
}

export function savingsOf(total: Cents, price: Cents): Cents {
  return cents(total - price);
}

export function savingsPercent(total: Cents, price: Cents): number | null {
  return total <= 0 ? null : roundTo((savingsOf(total, price) / total) * 100, 1);
}

export function marginPercent(price: Cents, cost: Cents | null): number | null {
  return cost === null || price <= 0 ? null : roundTo(((price - cost) / price) * 100, 1);
}

export function isBelowCost(price: Cents, cost: Cents | null): boolean {
  return cost !== null && price < cost;
}

export function isValidQuantity(quantity: number): boolean {
  return (
    Number.isFinite(quantity) &&
    quantity > 0 &&
    Math.abs(quantity * 10 ** QUANTITY_DECIMALS - Math.round(quantity * 10 ** QUANTITY_DECIMALS)) <
      1e-6
  );
}

export function isValidBarcode(barcode: string): boolean {
  if (!/^\d{13}$/.test(barcode)) {
    return false;
  }

  const digits = [...barcode].map(Number);
  const check = digits.pop() ?? -1;
  const sum = digits.reduce((total, digit, index) => total + digit * (index % 2 === 0 ? 1 : 3), 0);
  return (10 - (sum % 10)) % 10 === check;
}

export function isValidPartCode(code: string): boolean {
  return /^[A-Z0-9.-]{1,30}$/.test(code.trim().toUpperCase());
}

function roundTo(value: number, decimals: number): number {
  const factor = 10 ** decimals;
  return roundHalfUp(value * factor) / factor;
}
