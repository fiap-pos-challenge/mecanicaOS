import { Provider } from '@angular/core';
import { provideSessionData } from '../../../core/auth/session.gateway';
import { provideGateway } from '../../../core/config/provide-gateway';
import { AuthGateway, HttpAuthGateway } from './auth.gateway';
import { provideInvitationsData } from './invitations.gateway';
import { provideTwoFactorData } from './two-factor.gateway';

export function provideAuthData(): Provider[] {
  return [
    provideGateway(AuthGateway, 'auth', { http: HttpAuthGateway }),
    provideTwoFactorData(),
    provideInvitationsData(),
    provideSessionData(),
  ];
}
