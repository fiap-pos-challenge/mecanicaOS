import { HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import type {
  GetApiAuthMeData,
  GetApiAuthMeResponse,
  PostApiAuthLoginData,
  PostApiAuthLoginResponse,
} from '../../../core/api/generated/index';
import {
  RETRY_ENABLED,
  SKIP_AUTH,
  SKIP_GLOBAL_ERROR_HANDLING,
} from '../../../core/http/http-context';
import { LoginResponseWithChallenge } from './auth.contracts';
import { toCurrentUser, toLoginResult } from './auth.mappers';
import { Credentials, CurrentUser, LoginResult } from './auth.models';

export abstract class AuthGateway {
  abstract login(credentials: Credentials): Observable<LoginResult>;
  abstract me(): Observable<CurrentUser>;
}

@Injectable({ providedIn: 'root' })
export class HttpAuthGateway extends AuthGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  login(credentials: Credentials): Observable<LoginResult> {
    return this.api
      .send<PostApiAuthLoginData, PostApiAuthLoginResponse & LoginResponseWithChallenge>(
        'POST',
        '/api/auth/login',
        {
          body: credentials,
          context: this.context()
            .set(SKIP_AUTH, true)
            .set(SKIP_GLOBAL_ERROR_HANDLING, true)
            .set(RETRY_ENABLED, false),
        },
      )
      .pipe(map(toLoginResult));
  }

  me(): Observable<CurrentUser> {
    return this.api
      .send<GetApiAuthMeData, GetApiAuthMeResponse>('GET', '/api/auth/me', {
        context: this.context(),
      })
      .pipe(map(toCurrentUser));
  }
}
