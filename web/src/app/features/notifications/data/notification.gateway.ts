import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import {
  AppNotification,
  NotificationDto,
  NotificationPreference,
  NotificationPreferencesData,
  NotificationsData,
  normalizePreferences,
  toNotification,
} from './notification.models';

export abstract class NotificationGateway {
  abstract list(): Observable<readonly AppNotification[]>;
  abstract markRead(id: string): Observable<void>;
  abstract markAllRead(): Observable<void>;
  abstract preferences(): Observable<readonly NotificationPreference[]>;
  abstract savePreferences(
    items: readonly NotificationPreference[],
  ): Observable<readonly NotificationPreference[]>;
}

@Injectable({ providedIn: 'root' })
export class HttpNotificationGateway extends NotificationGateway {
  private readonly api = inject(ApiClient);

  list(): Observable<readonly AppNotification[]> {
    return this.api
      .send<NotificationsData, { items?: readonly NotificationDto[] }>(
        'GET',
        '/api/admin/notifications',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toNotification)));
  }

  markRead(id: string): Observable<void> {
    return this.api
      .send<NotificationsData, unknown>('POST', '/api/admin/notifications/{id}/read', {
        path: { id },
      })
      .pipe(map(() => undefined));
  }

  markAllRead(): Observable<void> {
    return this.api
      .send<NotificationsData, unknown>('POST', '/api/admin/notifications/read-all')
      .pipe(map(() => undefined));
  }

  preferences(): Observable<readonly NotificationPreference[]> {
    return this.api
      .send<NotificationPreferencesData, { items?: readonly Partial<NotificationPreference>[] }>(
        'GET',
        '/api/admin/notifications/preferences',
      )
      .pipe(map((dto) => normalizePreferences(dto.items ?? [])));
  }

  savePreferences(
    items: readonly NotificationPreference[],
  ): Observable<readonly NotificationPreference[]> {
    return this.api
      .send<NotificationPreferencesData, { items?: readonly Partial<NotificationPreference>[] }>(
        'PUT',
        '/api/admin/notifications/preferences',
        { body: { items } },
      )
      .pipe(map((dto) => normalizePreferences(dto.items ?? [])));
  }
}

export function provideNotificationData(): Provider {
  return provideGateway(NotificationGateway, 'notifications', { http: HttpNotificationGateway });
}
