import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import {
  RETRY_ENABLED,
  SKIP_AUTH,
  SKIP_GLOBAL_ERROR_HANDLING,
} from '../../../core/http/http-context';
import { VerifyTwoFactorData, VerifyTwoFactorResponse } from './auth.contracts';
import { toAuthSession } from './auth.mappers';
import { AuthSession } from './auth.models';

export abstract class TwoFactorGateway {
  abstract verify(challengeId: string, code: string): Observable<AuthSession>;
}

@Injectable({ providedIn: 'root' })
export class HttpTwoFactorGateway extends TwoFactorGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext()
      .set(SKIP_AUTH, true)
      .set(SKIP_GLOBAL_ERROR_HANDLING, true)
      .set(RETRY_ENABLED, false);
  }

  verify(challengeId: string, code: string): Observable<AuthSession> {
    return this.api
      .send<VerifyTwoFactorData, VerifyTwoFactorResponse>('POST', '/api/auth/2fa/verify', {
        body: { challengeId, code },
        context: this.context(),
      })
      .pipe(map(toAuthSession));
  }
}

export function provideTwoFactorData(): Provider {
  return provideGateway(TwoFactorGateway, 'twoFactor', {
    http: HttpTwoFactorGateway,
  });
}
