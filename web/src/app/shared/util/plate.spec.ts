import { formatPlate, isValidPlate, normalizePlate, plateFormatOf } from './plate';

describe('plate', () => {
  it('normalizes separators and case', () => {
    expect(normalizePlate('abc-1234')).toBe('ABC1234');
    expect(normalizePlate(' bra 2e19 ')).toBe('BRA2E19');
  });

  it('accepts old and Mercosul formats', () => {
    expect(plateFormatOf('ABC-1234')).toBe('old');
    expect(plateFormatOf('bra2e19')).toBe('mercosul');
    expect(plateFormatOf('AB12345')).toBeNull();
    expect(isValidPlate('BRA2E19')).toBe(true);
    expect(isValidPlate('BRA2EE9')).toBe(false);
  });

  it('formats for display', () => {
    expect(formatPlate('abc1234')).toBe('ABC-1234');
    expect(formatPlate('bra2e19')).toBe('BRA2E19');
  });
});
