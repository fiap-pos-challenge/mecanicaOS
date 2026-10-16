import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { messages } from '../../core/i18n/messages.pt-BR';
import { InactivityService } from '../../core/session/inactivity.service';
import { UiButton } from '../../shared/ui/button/button';
import { UiDialogLayout } from '../../shared/ui/dialog/dialog';
import { formatCountdown } from '../../shared/util/duration';

export type InactivityChoice = 'stay' | 'leave';

@Component({
  selector: 'app-inactivity-dialog',
  imports: [UiButton, UiDialogLayout],
  template: `
    <ui-dialog-layout [title]="text.title">
      <p aria-live="polite">{{ body() }}</p>
      <div uiDialogFooter>
        <button uiButton type="button" (click)="ref.close('leave')">{{ text.leave }}</button>
        <button uiButton variant="primary" type="button" (click)="ref.close('stay')">
          {{ text.stay }}
        </button>
      </div>
    </ui-dialog-layout>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InactivityDialog {
  protected readonly ref = inject<DialogRef<InactivityChoice>>(DialogRef);
  protected readonly text = messages.auth.inactivity;
  private readonly inactivity = inject(InactivityService);

  protected readonly body = computed(() =>
    this.text.body(formatCountdown(this.inactivity.remainingSeconds())),
  );
}
