import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import {
  NOTIFICATION_CHANNELS,
  NotificationChannel,
  NotificationPreference,
  NotificationType,
} from '../data/notification.models';
import { NotificationsStore } from '../state/notifications.store';

@Component({
  selector: 'app-notification-preferences-dialog',
  imports: [UiButton, UiDialogLayout],
  template: `
    <ui-dialog-layout [title]="text.preferencesTitle">
      <p class="hint">{{ text.preferencesHint }}</p>
      @if (error(); as message) {
        <p class="alert" role="alert">{{ message }}</p>
      }
      <div class="scroll">
        <table>
          <caption class="visually-hidden">
            {{
              text.preferencesTitle
            }}
          </caption>
          <thead>
            <tr>
              <th scope="col">{{ text.type }}</th>
              @for (channel of channels; track channel) {
                <th scope="col">{{ text.channels[channel] }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (item of items(); track item.type) {
              <tr>
                <th scope="row">{{ text.types[item.type] }}</th>
                @for (channel of channels; track channel) {
                  <td>
                    <input
                      type="checkbox"
                      [checked]="item[channel]"
                      [attr.aria-label]="text.toggle(text.types[item.type], text.channels[channel])"
                      (change)="toggle(item.type, channel)"
                    />
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div uiDialogFooter>
        <button uiButton type="button" (click)="ref.close(false)">{{ text.cancel }}</button>
        <button
          uiButton
          variant="primary"
          type="button"
          [disabled]="items().length === 0"
          [loading]="saving()"
          (click)="save()"
        >
          {{ text.save }}
        </button>
      </div>
    </ui-dialog-layout>
  `,
  styles: `
    .hint {
      margin: 0 0 var(--space-3);
      color: var(--muted);
      font-size: var(--text-sm);
    }
    .alert {
      padding: var(--space-3);
      border: 1px solid var(--bad);
      border-radius: var(--radius-md);
      background: var(--bad-soft);
    }
    .scroll {
      overflow-x: auto;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: var(--text-sm);
    }
    th,
    td {
      padding: var(--space-2);
      border-bottom: 1px solid var(--line);
      text-align: start;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationPreferencesDialog {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  private readonly store = inject<NotificationsStore>(DIALOG_DATA);

  protected readonly text = messages.notifications;
  protected readonly channels = NOTIFICATION_CHANNELS;
  protected readonly items = signal<readonly NotificationPreference[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  protected toggle(type: NotificationType, channel: NotificationChannel): void {
    this.items.update((items) =>
      items.map((item) => (item.type === type ? { ...item, [channel]: !item[channel] } : item)),
    );
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    const outcome = await this.store.savePreferences(this.items());
    this.saving.set(false);
    if (outcome.ok) {
      this.ref.close(true);
    } else {
      this.error.set(outcome.error);
    }
  }

  private async load(): Promise<void> {
    const result = await this.store.preferences();
    if (typeof result === 'string') {
      this.error.set(result);
    } else {
      this.items.set(result);
    }
  }
}
