import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { Reauthentication } from '../data/settings.gateway';
import type { ChangeOutcome } from '../state/settings.store';

export interface ReauthDialogData<T> {
  readonly change: string;
  readonly run: (proof: Reauthentication) => Promise<ChangeOutcome<T>>;
}

type ProofKind = 'password' | 'code';

@Component({
  selector: 'app-reauth-dialog',
  imports: [UiButton, UiControl, UiDialogLayout, UiField, UiSegmented],
  template: `
    <ui-dialog-layout [title]="text.title">
      <form class="form" novalidate (submit)="submit($event)">
        <p class="body">{{ text.body(data.change) }}</p>
        <ui-segmented [label]="text.title" [options]="kinds" [(value)]="kind" />
        @if (error(); as message) {
          <p class="alert" role="alert">{{ message }}</p>
        }
        @if (kind() === 'password') {
          <ui-field [label]="text.password" [required]="true">
            <input
              uiControl
              type="password"
              autocomplete="current-password"
              [value]="secret()"
              (input)="setSecret($event)"
            />
          </ui-field>
        } @else {
          <ui-field [label]="text.code" [required]="true">
            <input
              uiControl
              inputmode="numeric"
              autocomplete="one-time-code"
              maxlength="6"
              [value]="secret()"
              (input)="setSecret($event)"
            />
          </ui-field>
        }
        <div class="footer">
          <button uiButton type="button" (click)="ref.close(undefined)">{{ text.cancel }}</button>
          <button uiButton variant="primary" type="submit" [loading]="busy()">
            {{ text.confirm }}
          </button>
        </div>
      </form>
    </ui-dialog-layout>
  `,
  styles: `
    .form {
      display: grid;
      gap: var(--space-3);
    }
    .body {
      margin: 0;
    }
    .alert {
      margin: 0;
      padding: var(--space-3);
      border: 1px solid var(--bad);
      border-radius: var(--radius-md);
      background: var(--bad-soft);
      font-size: var(--text-sm);
    }
    .footer {
      display: flex;
      flex-wrap: wrap;
      justify-content: flex-end;
      gap: var(--space-2);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReauthDialog<T> {
  protected readonly ref = inject<DialogRef<ChangeOutcome<T>>>(DialogRef);
  protected readonly data = inject<ReauthDialogData<T>>(DIALOG_DATA);
  protected readonly text = messages.settings.reauth;
  protected readonly kinds: readonly SegmentedOption<ProofKind>[] = [
    { value: 'password', label: this.text.usePassword },
    { value: 'code', label: this.text.useCode },
  ];
  protected readonly kind = signal<ProofKind>('password');
  protected readonly secret = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected setSecret(event: Event): void {
    this.secret.set((event.target as HTMLInputElement).value);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const secret = this.secret().trim();
    if (secret === '') {
      this.error.set(this.text.empty);
      return;
    }

    this.busy.set(true);
    const outcome = await this.data.run(
      this.kind() === 'password' ? { password: secret } : { code: secret },
    );
    this.busy.set(false);
    this.secret.set('');
    if (outcome.ok) {
      this.ref.close(outcome);
    } else {
      this.error.set(outcome.error);
    }
  }
}
