import { cents } from '../../../shared/util/money';
import {
  isBelowCost,
  isValidBarcode,
  isValidPartCode,
  isValidQuantity,
  itemsTotal,
  lineTotal,
  marginPercent,
  roundHalfUp,
  savingsOf,
  savingsPercent,
} from './catalog.math';

describe('catalog math in cents', () => {
  it('rounds half up away from zero without negative zero', () => {
    expect(roundHalfUp(2.5)).toBe(3);
    expect(roundHalfUp(-2.5)).toBe(-3);
    expect(roundHalfUp(2.4)).toBe(2);
    expect(Object.is(roundHalfUp(-0.2), 0)).toBe(true);
    expect(roundHalfUp(0)).toBe(0);
  });

  it('multiplies price by fractional quantity rounding each line', () => {
    expect(lineTotal({ unitPrice: cents(5200), quantity: 4 })).toBe(20_800);
    expect(lineTotal({ unitPrice: cents(1999), quantity: 1.5 })).toBe(2999);
    expect(lineTotal({ unitPrice: cents(3333), quantity: 0.333 })).toBe(1110);
    expect(lineTotal({ unitPrice: cents(1), quantity: 0.5 })).toBe(1);
  });

  it('adds the package lines and computes the savings', () => {
    const total = itemsTotal([
      { unitPrice: cents(18_900), quantity: 1 },
      { unitPrice: cents(34_900), quantity: 1 },
      { unitPrice: cents(26_000), quantity: 1 },
    ]);

    expect(total).toBe(79_800);
    expect(savingsOf(total, cents(74_000))).toBe(5800);
    expect(savingsPercent(total, cents(74_000))).toBe(7.3);
    expect(savingsOf(total, cents(80_000))).toBe(-200);
    expect(savingsPercent(cents(0), cents(100))).toBeNull();
    expect(itemsTotal([])).toBe(0);
  });

  it('computes the margin over the sale price', () => {
    expect(marginPercent(cents(18_900), cents(11_200))).toBe(40.7);
    expect(marginPercent(cents(5200), cents(3224))).toBe(38);
    expect(marginPercent(cents(1000), cents(1200))).toBe(-20);
    expect(marginPercent(cents(0), cents(100))).toBeNull();
    expect(marginPercent(cents(100), null)).toBeNull();
    expect(isBelowCost(cents(1000), cents(1200))).toBe(true);
    expect(isBelowCost(cents(1200), cents(1200))).toBe(false);
    expect(isBelowCost(cents(1200), null)).toBe(false);
  });

  it('validates quantities, codes and EAN-13 barcodes', () => {
    expect(isValidQuantity(1.25)).toBe(true);
    expect(isValidQuantity(0.001)).toBe(true);
    expect(isValidQuantity(0.0001)).toBe(false);
    expect(isValidQuantity(0)).toBe(false);
    expect(isValidQuantity(Number.NaN)).toBe(false);
    expect(isValidPartCode(' pec-0101 ')).toBe(true);
    expect(isValidPartCode('PEC 0101')).toBe(false);
    expect(isValidPartCode('A'.repeat(31))).toBe(false);
    expect(isValidBarcode('7891234567895')).toBe(true);
    expect(isValidBarcode('7891234567890')).toBe(false);
    expect(isValidBarcode('789123')).toBe(false);
  });
});
