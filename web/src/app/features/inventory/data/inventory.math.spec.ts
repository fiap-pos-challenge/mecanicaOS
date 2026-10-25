import { cents } from '../../../shared/util/money';
import {
  balanceAfterExit,
  countDifference,
  countSummary,
  isValidQuantityFor,
  levelPercent,
  orderTotal,
  roundQuantity,
  situationOf,
  suggestedQuantity,
} from './inventory.math';
import { CountLine } from './inventory.models';

const now = Date.parse('2026-10-05T12:00:00Z');
const daysAgo = (days: number): string => new Date(now - days * 86_400_000).toISOString();

function line(systemQuantity: number, countedQuantity: number | null): CountLine {
  return {
    partId: 'p',
    code: 'A',
    name: 'A',
    type: 'Part',
    unitOfMeasure: 'un',
    systemQuantity,
    countedQuantity,
  };
}

describe('inventory math', () => {
  it('accepts fractional quantities only for supplies', () => {
    expect(isValidQuantityFor('Part', 2)).toBe(true);
    expect(isValidQuantityFor('Part', 1.5)).toBe(false);
    expect(isValidQuantityFor('Supply', 1.25)).toBe(true);
    expect(isValidQuantityFor('Supply', 0.0005)).toBe(false);
    expect(isValidQuantityFor('Supply', 0)).toBe(false);
    expect(isValidQuantityFor('Part', Number.NaN)).toBe(false);
    expect(roundQuantity(0.1 + 0.2)).toBe(0.3);
  });

  it('classifies the stock situation', () => {
    expect(situationOf({ stockQuantity: 0, minimumStock: 5, lastOutflowAt: daysAgo(1) }, now)).toBe(
      'OutOfStock',
    );
    expect(situationOf({ stockQuantity: 2, minimumStock: 8, lastOutflowAt: daysAgo(1) }, now)).toBe(
      'Reorder',
    );
    expect(
      situationOf({ stockQuantity: 10, minimumStock: 2, lastOutflowAt: daysAgo(120) }, now),
    ).toBe('Idle');
    expect(situationOf({ stockQuantity: 10, minimumStock: 2, lastOutflowAt: null }, now)).toBe(
      'Idle',
    );
    expect(
      situationOf({ stockQuantity: 10, minimumStock: 2, lastOutflowAt: daysAgo(5) }, now),
    ).toBe('Ok');
  });

  it('computes the visual level against twice the minimum', () => {
    expect(levelPercent(0, 5)).toBe(0);
    expect(levelPercent(5, 5)).toBe(50);
    expect(levelPercent(20, 5)).toBe(100);
    expect(levelPercent(3, 0)).toBe(100);
  });

  it('blocks a negative balance', () => {
    expect(balanceAfterExit(2, 2)).toBe(0);
    expect(balanceAfterExit(2, 3)).toBeNull();
    expect(balanceAfterExit(1.5, 0.25)).toBe(1.25);
  });

  it('computes the count differences and progress', () => {
    expect(countDifference(line(10, 8))).toBe(-2);
    expect(countDifference(line(10, null))).toBeNull();
    expect(countSummary([line(10, 8), line(4, 4), line(3, null)])).toEqual({
      counted: 2,
      total: 3,
      withDifference: 1,
      complete: false,
    });
    expect(countSummary([line(1, 1)]).complete).toBe(true);
  });

  it('suggests purchases from the minimum and the forecast', () => {
    expect(suggestedQuantity('Part', 2, 8, 22)).toBe(28);
    expect(suggestedQuantity('Part', 20, 8, 5)).toBe(0);
    expect(suggestedQuantity('Supply', 1.25, 2, 0)).toBe(2.8);
    expect(
      orderTotal([
        { unitCost: cents(11_200), quantity: 2 },
        { unitCost: cents(1999), quantity: 1.5 },
      ]),
    ).toBe(25_399);
    expect(orderTotal([])).toBe(0);
  });
});
