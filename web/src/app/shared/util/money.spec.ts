import {
  addCents,
  cents,
  centsFromDecimal,
  centsToDecimal,
  formatBrl,
  multiplyCents,
  parseBrl,
  percentOf,
} from './money';

const space = String.fromCharCode(160);

describe('money', () => {
  it('keeps values as integer cents without floating point drift', () => {
    expect(addCents(cents(10), cents(20))).toBe(30);
    expect(addCents(centsFromDecimal(0.1), centsFromDecimal(0.2))).toBe(30);
    expect(centsFromDecimal(1234.56)).toBe(123456);
    expect(centsToDecimal(cents(123456))).toBe(1234.56);
    expect(addCents()).toBe(0);
  });

  it('rejects values that are not safe integers or finite', () => {
    expect(() => cents(1.5)).toThrow(RangeError);
    expect(() => centsFromDecimal(Number.NaN)).toThrow(RangeError);
  });

  it('formats as Brazilian real', () => {
    expect(formatBrl(cents(123456))).toBe(`R$${space}1.234,56`);
    expect(formatBrl(cents(5))).toBe(`R$${space}0,05`);
  });

  it('parses Brazilian money text', () => {
    expect(parseBrl('R$ 1.234,56')).toBe(123456);
    expect(parseBrl('1234,5')).toBe(123450);
    expect(parseBrl('-10')).toBe(-1000);
    expect(parseBrl(`R$${space}7,00`)).toBe(700);
    expect(parseBrl('12,345')).toBeNull();
    expect(parseBrl('abc')).toBeNull();
  });

  it('multiplies and applies percentages with rounding', () => {
    expect(multiplyCents(cents(333), 3)).toBe(999);
    expect(multiplyCents(cents(1999), 1.5)).toBe(2999);
    expect(percentOf(cents(10000), 1250)).toBe(1250);
    expect(percentOf(cents(999), 1000)).toBe(100);
  });
});
