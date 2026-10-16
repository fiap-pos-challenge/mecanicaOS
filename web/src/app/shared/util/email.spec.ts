import { EMAIL_MAX_LENGTH, isValidEmail, normalizeEmail } from './email';

describe('email', () => {
  it('normalizes and validates', () => {
    expect(normalizeEmail('  Ana@Oficina.Local ')).toBe('ana@oficina.local');
    expect(isValidEmail('ana@oficina.local')).toBe(true);
    expect(isValidEmail('ana@oficina')).toBe(false);
    expect(isValidEmail(`${'a'.repeat(EMAIL_MAX_LENGTH)}@x.io`)).toBe(false);
  });
});
