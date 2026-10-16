import { maskDocument, maskEmail, maskName, maskPhone, maskPlate } from './mask';

describe('mask', () => {
  it('hides the sensitive part of documents', () => {
    expect(maskDocument('52998224725')).toBe('•••.982.247-••');
    expect(maskDocument('11222333000181')).toBe('11.222.•••/••••-81');
  });

  it('hides e-mail, phone, plate and surname', () => {
    expect(maskEmail('joana@example.com')).toBe('j•••@example.com');
    expect(maskEmail('invalid')).toBe('•••');
    expect(maskPhone('(11) 98765-4321')).toBe('•••••••4321');
    expect(maskPhone('12')).toBe('••••');
    expect(maskPlate('abc-1234')).toBe('ABC••••');
    expect(maskName('Joana Prado Lima')).toBe('Joana P. L.');
    expect(maskName('Joana')).toBe('Joana');
  });
});
