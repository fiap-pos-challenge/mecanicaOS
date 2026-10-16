import { HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import type {
  DeleteApiAdminUsersByIdData,
  GetApiAdminUsersByIdData,
  GetApiAdminUsersByIdResponse,
  GetApiAdminUsersData,
  GetApiAdminUsersResponse,
  PostApiAdminUsersByIdDeactivateData,
  PostApiAdminUsersByIdInviteResendData,
  PostApiAdminUsersByIdInviteResendResponse,
  PostApiAdminUsersByIdReactivateData,
  PostApiAdminUsersInviteData,
  PostApiAdminUsersInviteResponse,
  PostApiAdminUsersSearchData,
  PostApiAdminUsersSearchResponse,
  PutApiAdminUsersByIdData,
  PutApiAdminUsersByIdResponse,
} from '../../../core/api/generated/index';
import { Page } from '../../../core/data/page';
import { PersonalDataSearch } from '../../../core/data/search';
import { toUser, toUserPage } from './user.mappers';
import { User, UserInvite, UserListQuery, UserUpdate } from './user.models';

export abstract class UsersGateway {
  abstract list(query: UserListQuery): Observable<Page<User>>;
  abstract get(id: string): Observable<User>;
  abstract findByEmail(search: PersonalDataSearch<'email'>): Observable<User>;
  abstract invite(input: UserInvite): Observable<User>;
  abstract resendInvitation(id: string): Observable<User>;
  abstract update(id: string, input: UserUpdate): Observable<User>;
  abstract remove(id: string): Observable<void>;
  abstract deactivate(id: string): Observable<void>;
  abstract reactivate(id: string): Observable<void>;
}

@Injectable({ providedIn: 'root' })
export class HttpUsersGateway extends UsersGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  list(query: UserListQuery): Observable<Page<User>> {
    return this.api
      .send<GetApiAdminUsersData, GetApiAdminUsersResponse>('GET', '/api/admin/users', {
        query: {
          page: query.page,
          pageSize: query.pageSize,
          ...(query.role ? { role: query.role } : {}),
          ...(query.status ? { status: query.status } : {}),
        },
        context: this.context(),
      })
      .pipe(map(toUserPage));
  }

  get(id: string): Observable<User> {
    return this.api
      .send<GetApiAdminUsersByIdData, GetApiAdminUsersByIdResponse>(
        'GET',
        '/api/admin/users/{id}',
        {
          path: { id },
          context: this.context(),
        },
      )
      .pipe(map(toUser));
  }

  findByEmail(search: PersonalDataSearch<'email'>): Observable<User> {
    return this.api
      .send<PostApiAdminUsersSearchData, PostApiAdminUsersSearchResponse>(
        'POST',
        '/api/admin/users/search',
        {
          body: { email: search.email },
          context: this.context(),
        },
      )
      .pipe(map(toUser));
  }

  invite(input: UserInvite): Observable<User> {
    return this.api
      .send<PostApiAdminUsersInviteData, PostApiAdminUsersInviteResponse>(
        'POST',
        '/api/admin/users/invite',
        {
          body: input,
          context: this.context(),
        },
      )
      .pipe(map(toUser));
  }

  resendInvitation(id: string): Observable<User> {
    return this.api
      .send<PostApiAdminUsersByIdInviteResendData, PostApiAdminUsersByIdInviteResendResponse>(
        'POST',
        '/api/admin/users/{id}/invite/resend',
        {
          path: { id },
          context: this.context(),
        },
      )
      .pipe(map(toUser));
  }

  update(id: string, input: UserUpdate): Observable<User> {
    return this.api
      .send<PutApiAdminUsersByIdData, PutApiAdminUsersByIdResponse>(
        'PUT',
        '/api/admin/users/{id}',
        {
          path: { id },
          body: input,
          context: this.context(),
        },
      )
      .pipe(map(toUser));
  }

  remove(id: string): Observable<void> {
    return this.api
      .send<DeleteApiAdminUsersByIdData, unknown>('DELETE', '/api/admin/users/{id}', {
        path: { id },
        context: this.context(),
      })
      .pipe(map(() => undefined));
  }

  deactivate(id: string): Observable<void> {
    return this.api
      .send<PostApiAdminUsersByIdDeactivateData, unknown>(
        'POST',
        '/api/admin/users/{id}/deactivate',
        {
          path: { id },
          context: this.context(),
        },
      )
      .pipe(map(() => undefined));
  }

  reactivate(id: string): Observable<void> {
    return this.api
      .send<PostApiAdminUsersByIdReactivateData, unknown>(
        'POST',
        '/api/admin/users/{id}/reactivate',
        {
          path: { id },
          context: this.context(),
        },
      )
      .pipe(map(() => undefined));
  }
}
