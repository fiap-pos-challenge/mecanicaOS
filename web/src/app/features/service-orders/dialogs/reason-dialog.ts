import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';

export const MIN_REASON = 5;

@Component({
  selector: 'app-reason-dialog',
  imports: [UiButton, UiControl, UiDialogLayout, UiField],
  template: `
    <ui-dialog-layout [title]="text.title">
      <form class="form" novalidate (submit)="submit($event)">
        <p class="hint">{{ text.description }}</p>
        <ui-field [label]="text.label" [required]="true" [error]="invalid() ? text.required : null">
          <textarea
            uiControl
            rows="3"
            maxlength="500"
            [value]="reason()"
            (input)="setReason($event)"
          ></textarea>
        </ui-field>
        <div class="footer">
          <button uiButton type="button" (click)="ref.close()">{{ text.cancel }}</button>
          <button uiButton variant="primary" type="submit">{{ text.submit }}</button>
        </div>
      </form>
    </ui-dialog-layout>
  `,
  styles: `
    .form {
      display: grid;
      gap: var(--space-4);
    }
    .hint {
      margin: 0;
      color: var(--muted);
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
export class ReasonDialog {
  protected readonly ref = inject<DialogRef<string>>(DialogRef);
  protected readonly text = messages.serviceOrders.reason;
  protected readonly reason = signal('');
  protected readonly invalid = signal(false);

  protected setReason(event: Event): void {
    this.reason.set((event.target as HTMLTextAreaElement).value);
    this.invalid.set(false);
  }

  protected submit(event: Event): void {
    event.preventDefault();
    const reason = this.reason().trim();
    if (reason.length < MIN_REASON) {
      this.invalid.set(true);
      return;
    }

    this.ref.close(reason);
  }
}
