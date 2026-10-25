import { ContractError, required } from './contract-error';

describe('required', () => {
  it('returns present values and rejects missing ones', () => {
    expect(required(0, 'page')).toBe(0);
    expect(() => required(null, 'name')).toThrow(ContractError);
    expect(() => required(undefined, 'name')).toThrow('"name"');
  });
});
