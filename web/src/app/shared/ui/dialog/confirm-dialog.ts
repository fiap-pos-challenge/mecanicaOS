import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../button/button';
import { UiDialogLayout } from './dialog';

export interface ConfirmDialogData {
  readonly title: string;
  readonly body: string;
  readonly confirm: string;
  readonly danger?: boolean;
  readonly alert?: boolean;
}

@Component({
  selector: 'ui-confirm-dialog',
  imports: [UiButton, UiDialogLayout],
  template: `
    <ui-dialog-layout [title]="data.title">
      <p>{{ data.body }}</p>
      <div uiDialogFooter>
        @if (!data.alert) {
          <button uiButton type="button" (click)="ref.close(false)">{{ cancel }}</button>
        }
        <button
          uiButton
          type="button"
          [variant]="data.danger ? 'danger' : 'primary'"
          (click)="ref.close(true)"
        >
          {{ data.confirm }}
        </button>
      </div>
    </ui-dialog-layout>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiConfirmDialog {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly data = inject<ConfirmDialogData>(DIALOG_DATA);
  protected readonly cancel = messages.ui.dialog.cancel;
}
