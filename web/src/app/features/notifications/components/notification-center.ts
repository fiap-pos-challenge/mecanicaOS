import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDateTime } from '../../../shared/util/date';
import { AppNotification } from '../data/notification.models';
import { NotificationsStore } from '../state/notifications.store';
import { NotificationPreferencesDialog } from './notification-preferences-dialog';

@Component({
  selector: 'app-notification-center',
  imports: [UiAiBadge, UiButton],
  template: `
    <div class="head">
      <h2 class="title">{{ text.title }}</h2>
      @if (store.unread() > 0) {
        <button uiButton size="sm" type="button" (click)="store.markAllRead()">
          {{ text.markAll }}
        </button>
      }
    </div>
    @switch (store.state()) {
      @case ('pending') {
        <p class="note">{{ pendingText }}</p>
      }
      @case ('error') {
        <p class="note" role="alert">{{ text.loadError }}</p>
      }
      @default {
        <ul class="list" [attr.aria-label]="text.label">
          @for (item of store.items(); track item.id) {
            <li class="item" [class.item--unread]="!item.read">
              <button type="button" class="open" (click)="open(item)">
                <strong>
                  @if (!item.read) {
                    <span class="visually-hidden">{{ text.unread }}: </span>
                  }
                  {{ item.title }}
                </strong>
                <span class="body">{{ item.body }}</span>
                <small class="meta">{{ types[item.type] }} · {{ when(item.createdAt) }}</small>
              </button>
              @if (item.aiSuggested) {
                <ui-ai-badge />
              }
              @if (!item.read) {
                <button
                  type="button"
                  class="mark"
                  [attr.aria-label]="text.markRead(item.title)"
                  (click)="store.markRead(item.id)"
                >
                  <span aria-hidden="true" class="dot"></span>
                </button>
              }
            </li>
          } @empty {
            <li class="note">{{ text.empty }}</li>
          }
        </ul>
      }
    }
    <div class="foot">
      <button uiButton size="sm" type="button" (click)="preferences()">
        {{ text.preferences }}
      </button>
    </div>
  `,
  styles: `
    :host {
      display: grid;
      gap: var(--space-2);
      padding: var(--space-3);
    }
    .head,
    .foot {
      display: flex;
      gap: var(--space-2);
      align-items: center;
      justify-content: space-between;
    }
    .title {
      margin: 0;
      font-size: var(--text-md);
    }
    .list {
      display: grid;
      gap: var(--space-1);
      max-height: min(60vh, 420px);
      margin: 0;
      padding: 0;
      overflow-y: auto;
      list-style: none;
    }
    .item {
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto;
      gap: var(--space-1);
      align-items: start;
      padding: var(--space-2);
      border-radius: var(--radius-md);
    }
    .item--unread {
      background: var(--accent-soft);
    }
    .open {
      display: grid;
      gap: 2px;
      padding: 0;
      border: 0;
      background: none;
      color: inherit;
      font: inherit;
      text-align: start;
      cursor: pointer;
    }
    .body,
    .meta,
    .note {
      color: var(--muted);
      font-size: var(--text-sm);
    }
    .mark {
      display: grid;
      place-items: center;
      width: 28px;
      height: 28px;
      padding: 0;
      border: 0;
      background: none;
      cursor: pointer;
    }
    .dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: var(--accent);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationCenter {
  protected readonly store = inject(NotificationsStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);

  readonly navigated = output();

  protected readonly text = messages.notifications;
  protected readonly types = messages.notifications.types;
  protected readonly pendingText = messages.pending.notifications;

  protected when(date: string): string {
    return formatDateTime(date);
  }

  protected async open(item: AppNotification): Promise<void> {
    await this.store.open(item);
    if (item.link) {
      this.navigated.emit();
    }
  }

  protected async preferences(): Promise<void> {
    const ref = this.dialogs.open<NotificationPreferencesDialog, NotificationsStore, boolean>(
      NotificationPreferencesDialog,
      this.store,
    );
    if ((await firstValueFrom(ref.closed)) === true) {
      this.toast.show(this.text.saved, { tone: 'success' });
    }
  }
}
