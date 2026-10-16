import { formatCountdown } from './duration';

describe('formatCountdown', () => {
  it('formats minutes and seconds', () => {
    expect(formatCountdown(0)).toBe('0:00');
    expect(formatCountdown(-3)).toBe('0:00');
    expect(formatCountdown(9.2)).toBe('0:10');
    expect(formatCountdown(900)).toBe('15:00');
  });
});
