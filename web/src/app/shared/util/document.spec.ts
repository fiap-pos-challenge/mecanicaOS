import { documentKindOf, formatDocument, isValidCnpj, isValidCpf } from './document';

describe('document', () => {
  it('validates CPF check digits', () => {
    expect(isValidCpf('529.982.247-25')).toBe(true);
    expect(isValidCpf('52998224724')).toBe(false);
    expect(isValidCpf('11111111111')).toBe(false);
    expect(isValidCpf('5299822472')).toBe(false);
  });

  it('validates CNPJ check digits', () => {
    expect(isValidCnpj('11.222.333/0001-81')).toBe(true);
    expect(isValidCnpj('11222333000182')).toBe(false);
    expect(isValidCnpj('00000000000000')).toBe(false);
    expect(isValidCnpj('11222333000')).toBe(false);
  });

  it('detects the document kind like the backend', () => {
    expect(documentKindOf('529.982.247-25')).toBe('Cpf');
    expect(documentKindOf('11.222.333/0001-81')).toBe('Cnpj');
    expect(documentKindOf('52998224725x')).toBeNull();
    expect(documentKindOf('123')).toBeNull();
  });

  it('masks CPF and CNPJ while typing', () => {
    expect(formatDocument('529')).toBe('529');
    expect(formatDocument('5299822')).toBe('529.982.2');
    expect(formatDocument('52998224725')).toBe('529.982.247-25');
    expect(formatDocument('112223330001')).toBe('11.222.333/0001');
    expect(formatDocument('11222333000181')).toBe('11.222.333/0001-81');
    expect(formatDocument('112223330001819')).toBe('11.222.333/0001-81');
  });
});
