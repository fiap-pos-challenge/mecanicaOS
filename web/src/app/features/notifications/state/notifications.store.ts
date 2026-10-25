import { computed, DestroyRef, inject, Injectable, InjectionToken, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { RealtimeGateway } from '../../../core/realtime/realtime.gateway';
import { NotificationGateway } from '../data/notification.gateway';
import {
  AppNotification,
  NotificationDto,
  NotificationPreference,
  toNotification,
} from '../data/notification.models';

export const NOTIFICATION_POLL_MS = new InjectionToken<number>('NOTIFICATION_POLL_MS', {
  providedIn: 'root',
  factory: () => 60_000,
});

export type NotificationsLoad = 'idle' | 'loading' | 'ready' | 'pending' | 'error';

const text = messages.notifications;

@Injectable()
export class NotificationsStore {
  private readonly gateway = inject(NotificationGateway);
  private readonly router = inject(Router);

  readonly items = signal<readonly AppNotification[]>([]);
  readonly state = signal<NotificationsLoad>('idle');
  readonly announcement = signal('');
  readonly unread = computed(() => this.items().filter((item) => !item.read).length);

  constructor() {
    inject(RealtimeGateway)
      .events$.pipe(
        filter((event) => event.type === 'notification'),
        takeUntilDestroyed(),
      )
      .subscribe((event) => {
        if (event.type === 'notification') {
          this.receive(event.notification as NotificationDto);
        }
      });
    const every = inject(NOTIFICATION_POLL_MS);
    if (every > 0) {
      const timer = setInterval(() => void this.load(), every);
      inject(DestroyRef).onDestroy(() => clearInterval(timer));
    }
  }

  async load(): Promise<void> {
    if (this.state() === 'idle') {
      this.state.set('loading');
    }

    try {
      const items = await firstValueFrom(this.gateway.list());
      const known = new Set(this.items().map((item) => item.id));
      const fresh = items.find((item) => !item.read && !known.has(item.id));
      if (this.state() === 'ready' && fresh) {
        this.announcement.set(text.live(fresh.title));
      }

      this.items.set(items);
      this.state.set('ready');
    } catch (error) {
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }

  receive(dto: NotificationDto): void {
    try {
      const notification = toNotification(dto);
      this.items.update((items) => [
        notification,
        ...items.filter((item) => item.id !== notification.id),
      ]);
      this.announcement.set(text.live(notification.title));
    } catch {
      return;
    }
  }

  async markRead(id: string): Promise<void> {
    this.items.update((items) =>
      items.map((item) => (item.id === id ? { ...item, read: true } : item)),
    );
    await firstValueFrom(this.gateway.markRead(id)).catch(() => undefined);
  }

  async markAllRead(): Promise<void> {
    this.items.update((items) => items.map((item) => ({ ...item, read: true })));
    await firstValueFrom(this.gateway.markAllRead()).catch(() => undefined);
  }

  async open(notification: AppNotification): Promise<void> {
    await this.markRead(notification.id);
    if (notification.link) {
      await this.router.navigateByUrl(notification.link);
    }
  }

  async preferences(): Promise<readonly NotificationPreference[] | string> {
    try {
      return await firstValueFrom(this.gateway.preferences());
    } catch (error) {
      return isApiError(error) ? error.detail : text.loadError;
    }
  }

  async savePreferences(
    items: readonly NotificationPreference[],
  ): Promise<{ readonly ok: true } | { readonly ok: false; readonly error: string }> {
    try {
      await firstValueFrom(this.gateway.savePreferences(items));
      return { ok: true };
    } catch (error) {
      return { ok: false, error: isApiError(error) ? error.detail : messages.errors.unknown };
    }
  }
}
