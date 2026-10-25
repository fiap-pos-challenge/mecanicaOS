import { addCents, Cents, cents } from '../../../shared/util/money';
import { PartType } from '../../catalog/data/catalog.models';
import { lineTotal } from '../../catalog/data/catalog.math';
import { CountLine, StockItem, StockSituation } from './inventory.models';

export const IDLE_DAYS = 90;
export const QUANTITY_SCALE = 1000;
const DAY_MS = 24 * 60 * 60 * 1000;

export function roundQuantity(value: number): number {
  return Math.round(value * QUANTITY_SCALE) / QUANTITY_SCALE;
}

export function isValidQuantityFor(type: PartType, quantity: number): boolean {
  if (!Number.isFinite(quantity) || quantity <= 0) {
    return false;
  }

  return type === 'Part'
    ? Number.isInteger(quantity)
    : Math.abs(quantity * QUANTITY_SCALE - Math.round(quantity * QUANTITY_SCALE)) < 1e-6;
}

export function situationOf(
  item: Pick<StockItem, 'stockQuantity' | 'minimumStock' | 'lastOutflowAt'>,
  now: number = Date.now(),
): StockSituation {
  if (item.stockQuantity <= 0) {
    return 'OutOfStock';
  }

  if (item.stockQuantity < item.minimumStock) {
    return 'Reorder';
  }

  const lastOutflow = item.lastOutflowAt ? Date.parse(item.lastOutflowAt) : Number.NaN;
  return Number.isNaN(lastOutflow) || now - lastOutflow > IDLE_DAYS * DAY_MS ? 'Idle' : 'Ok';
}

export function levelPercent(stockQuantity: number, minimumStock: number): number {
  if (stockQuantity <= 0) {
    return 0;
  }

  if (minimumStock <= 0) {
    return 100;
  }

  return Math.min(100, Math.round((stockQuantity / (minimumStock * 2)) * 100));
}

export function balanceAfterExit(stockQuantity: number, quantity: number): number | null {
  const balance = roundQuantity(stockQuantity - quantity);
  return balance < 0 ? null : balance;
}

export function countDifference(
  line: Pick<CountLine, 'systemQuantity' | 'countedQuantity'>,
): number | null {
  return line.countedQuantity === null
    ? null
    : roundQuantity(line.countedQuantity - line.systemQuantity);
}

export function countSummary(lines: readonly CountLine[]): {
  readonly counted: number;
  readonly total: number;
  readonly withDifference: number;
  readonly complete: boolean;
} {
  const counted = lines.filter((line) => line.countedQuantity !== null).length;
  const withDifference = lines.filter((line) => {
    const difference = countDifference(line);
    return difference !== null && difference !== 0;
  }).length;
  return { counted, total: lines.length, withDifference, complete: counted === lines.length };
}

export function suggestedQuantity(
  type: PartType,
  stockQuantity: number,
  minimumStock: number,
  forecast30d: number,
): number {
  const target = Math.max(minimumStock * 2, forecast30d + minimumStock);
  const missing = target - stockQuantity;
  if (missing <= 0) {
    return 0;
  }

  return type === 'Part' ? Math.ceil(missing) : Math.ceil(missing * 10) / 10;
}

export function orderTotal(
  lines: readonly { readonly unitCost: Cents; readonly quantity: number }[],
): Cents {
  return addCents(
    cents(0),
    ...lines.map((line) => lineTotal({ unitPrice: line.unitCost, quantity: line.quantity })),
  );
}
