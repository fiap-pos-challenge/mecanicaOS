import type { LoginResponse } from '../../../core/api/generated/index';

export interface TwoFactorChallenge {
  readonly requiresTwoFactor?: boolean;
  readonly challengeId?: string | null;
}

export type LoginResponseWithChallenge = LoginResponse & TwoFactorChallenge;

export interface VerifyTwoFactorData {
  readonly url: '/api/auth/2fa/verify';
  readonly body: { readonly challengeId: string; readonly code: string };
}

export type VerifyTwoFactorResponse = LoginResponse;
