let sequence = 0;

export function fakeId(): string {
  sequence += 1;
  return `00000000-0000-4000-8000-${sequence.toString(16).padStart(12, '0')}`;
}

export function fakeNow(): string {
  return new Date().toISOString();
}
