import { formatDate, formatDateTime, formatTime, localDateKey, parseUtc, toUtcIso } from './date';

describe('date', () => {
  it('parses only ISO UTC values', () => {
    expect(parseUtc('2026-10-04T15:30:00Z')?.toISOString()).toBe('2026-10-04T15:30:00.000Z');
    expect(parseUtc('2026-10-04T15:30:00.1234567Z')?.toISOString()).toBe(
      '2026-10-04T15:30:00.123Z',
    );
    expect(parseUtc('2026-10-04T15:30:00+00:00')).not.toBeNull();
    expect(parseUtc('2026-10-04T15:30:00')).toBeNull();
    expect(parseUtc('2026-10-04T15:30:00-03:00')).toBeNull();
    expect(parseUtc('2026-13-45T99:99:99Z')).toBeNull();
  });

  it('formats UTC instants in the Sao Paulo time zone', () => {
    expect(formatDateTime('2026-10-04T15:30:00Z')).toBe('04/10/2026 12:30');
    expect(formatTime('2026-10-04T15:30:00Z')).toBe('12:30');
    expect(formatDate('2026-10-05T02:00:00Z')).toBe('04/10/2026');
    expect(localDateKey('2026-10-05T02:00:00Z')).toBe('2026-10-04');
  });

  it('rejects dates without time zone', () => {
    expect(() => formatDate('2026-10-04')).toThrow(RangeError);
  });

  it('serializes dates as ISO UTC', () => {
    expect(toUtcIso(new Date(Date.UTC(2026, 9, 4, 12)))).toBe('2026-10-04T12:00:00.000Z');
  });
});
