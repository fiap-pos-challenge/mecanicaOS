export const FAKE_TWO_FACTOR_CODE = '123456';
export const FAKE_TWO_FACTOR_EMAIL = 'bia@oficina.local';
export const FAKE_MAX_FAILED_LOGINS = 5;
export const FAKE_LOCKOUT_MS = 15 * 60_000;

export interface FakeAuthState {
  readonly failures: Map<string, number>;
  readonly lockouts: Map<string, number>;
  readonly challenges: Map<string, string>;
  readonly twoFactorUsers: Set<string>;
  refreshCookie: string | null;
}

export function createFakeAuthState(): FakeAuthState {
  return {
    failures: new Map(),
    lockouts: new Map(),
    challenges: new Map(),
    twoFactorUsers: new Set(),
    refreshCookie: null,
  };
}
