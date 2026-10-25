import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { provideGateway } from '../config/provide-gateway';
import { required } from '../data/contract-error';
import { RETRY_ENABLED, SKIP_AUTH, SKIP_GLOBAL_ERROR_HANDLING } from '../http/http-context';
import { StoredSession } from './auth-token.store';
import { LogoutData, RefreshSessionData, RefreshSessionResponse } from './session.contracts';

export abstract class SessionGateway {
  abstract refresh(): Observable<StoredSession | null>;
  abstract logout(): Observable<void>;
}

function toStoredSession(dto: RefreshSessionResponse): StoredSession {
  return {
    accessToken: required(dto.accessToken, 'accessToken'),
    expiresAt: required(dto.expiresAt, 'expiresAt'),
  };
}

@Injectable({ providedIn: 'root' })
export class HttpSessionGateway extends SessionGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext()
      .set(SKIP_AUTH, true)
      .set(SKIP_GLOBAL_ERROR_HANDLING, true)
      .set(RETRY_ENABLED, false);
  }

  refresh(): Observable<StoredSession | null> {
    return this.api
      .send<RefreshSessionData, RefreshSessionResponse>('POST', '/api/auth/refresh', {
        context: this.context(),
      })
      .pipe(
        map(toStoredSession),
        catchError(() => of(null)),
      );
  }

  logout(): Observable<void> {
    return this.api
      .send<LogoutData, unknown>('POST', '/api/auth/logout', { context: this.context() })
      .pipe(
        map(() => undefined),
        catchError(() => of(undefined)),
      );
  }
}

@Injectable({ providedIn: 'root' })
export class PendingSessionGateway extends SessionGateway {
  refresh(): Observable<StoredSession | null> {
    return of(null);
  }

  logout(): Observable<void> {
    return of(undefined);
  }
}

export function provideSessionData(): Provider {
  return provideGateway(SessionGateway, 'session', {
    http: HttpSessionGateway,
    pending: PendingSessionGateway,
  });
}
