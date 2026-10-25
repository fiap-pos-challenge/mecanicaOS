import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject } from '@angular/core';
import { InactivityService } from '../../core/session/inactivity.service';
import { AuthStore } from '../../features/auth/state/auth.store';
import { UiDialogService } from '../../shared/ui/dialog/dialog';
import { InactivityChoice, InactivityDialog } from './inactivity-dialog';

@Component({
  selector: 'app-session-watch',
  template: '',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SessionWatch {
  private readonly inactivity = inject(InactivityService);
  private readonly auth = inject(AuthStore);
  private readonly dialogs = inject(UiDialogService);
  private dialog: DialogRef<InactivityChoice, InactivityDialog> | null = null;

  constructor() {
    this.inactivity.start(() => this.auth.logout('inactivity'));
    inject(DestroyRef).onDestroy(() => {
      this.inactivity.stop();
      this.dialog?.close();
    });

    effect(() => {
      if (this.inactivity.warning()) {
        this.openWarning();
      } else {
        this.dialog?.close();
      }
    });
  }

  private openWarning(): void {
    if (this.dialog) {
      return;
    }

    const dialog = this.dialogs.open<InactivityDialog, null, InactivityChoice>(InactivityDialog);
    this.dialog = dialog;
    dialog.closed.subscribe((choice) => {
      this.dialog = null;
      if (choice === 'leave') {
        this.auth.logout();
      } else if (this.inactivity.warning()) {
        this.inactivity.stay();
      }
    });
  }
}
