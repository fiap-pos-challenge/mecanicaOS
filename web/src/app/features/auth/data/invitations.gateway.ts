import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import type {
  InvitationDto,
  PostApiAuthInvitationsAcceptData,
  PostApiAuthInvitationsInspectData,
  PostApiAuthInvitationsInspectResponse,
} from '../../../core/api/generated/index';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { Role } from './auth.models';

export interface Invitation {
  readonly name: string;
  readonly maskedEmail: string;
  readonly role: Role;
  readonly expiresAt: string;
}

export function toInvitation(dto: InvitationDto): Invitation {
  return {
    name: required(dto.name, 'name'),
    maskedEmail: required(dto.maskedEmail, 'maskedEmail'),
    role: required(dto.role, 'role'),
    expiresAt: required(dto.expiresAt, 'expiresAt'),
  };
}

export abstract class InvitationsGateway {
  abstract inspect(token: string): Observable<Invitation>;
  abstract accept(token: string, password: string): Observable<void>;
}

@Injectable({ providedIn: 'root' })
export class HttpInvitationsGateway extends InvitationsGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  inspect(token: string): Observable<Invitation> {
    return this.api
      .send<PostApiAuthInvitationsInspectData, PostApiAuthInvitationsInspectResponse>(
        'POST',
        '/api/auth/invitations/inspect',
        { body: { token }, context: this.context() },
      )
      .pipe(map(toInvitation));
  }

  accept(token: string, password: string): Observable<void> {
    return this.api
      .send<PostApiAuthInvitationsAcceptData, unknown>('POST', '/api/auth/invitations/accept', {
        body: { token, password },
        context: this.context(),
      })
      .pipe(map(() => undefined));
  }
}

export function provideInvitationsData(): Provider {
  return provideGateway(InvitationsGateway, 'auth', {
    http: HttpInvitationsGateway,
  });
}
