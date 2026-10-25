import { formatPhone, isValidPhone } from './phone';

describe('phone', () => {
  it('validates like the backend', () => {
    expect(isValidPhone('(11) 98765-4321')).toBe(true);
    expect(isValidPhone('1133224455')).toBe(true);
    expect(isValidPhone('0113322445')).toBe(false);
    expect(isValidPhone('113322445')).toBe(false);
    expect(isValidPhone('119876543210')).toBe(false);
    expect(isValidPhone('11 9876.54321')).toBe(false);
  });

  it('formats landline and mobile numbers while typing', () => {
    expect(formatPhone('1')).toBe('1');
    expect(formatPhone('119')).toBe('(11) 9');
    expect(formatPhone('1133224455')).toBe('(11) 3322-4455');
    expect(formatPhone('11987654321')).toBe('(11) 98765-4321');
  });
});
