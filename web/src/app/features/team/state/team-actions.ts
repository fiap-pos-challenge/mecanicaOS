import { inject, Injectable } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { ConfirmDialogData, UiConfirmDialog } from '../../../shared/ui/dialog/confirm-dialog';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { ToastService } from '../../../shared/ui/toast/toast';
import { User } from '../data/user.models';
import { UsersGateway } from '../data/users.gateway';

@Injectable({ providedIn: 'root' })
export class TeamActions {
  private readonly users = inject(UsersGateway);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);
  private readonly text = messages.team.list;

  async deactivate(user: User): Promise<boolean> {
    const confirmed = await this.confirm({
      title: this.text.deactivateTitle,
      body: this.text.deactivateBody(user.name),
      confirm: this.text.deactivate,
      danger: true,
    });
    return confirmed && this.run(this.users.deactivate(user.id), this.text.deactivated);
  }

  async reactivate(user: User): Promise<boolean> {
    const confirmed = await this.confirm({
      title: this.text.reactivateTitle,
      body: this.text.reactivateBody(user.name),
      confirm: this.text.reactivate,
    });
    return confirmed && this.run(this.users.reactivate(user.id), this.text.reactivated);
  }

  resend(user: User): Promise<boolean> {
    return this.run(this.users.resendInvitation(user.id), this.text.resent);
  }

  private async confirm(data: ConfirmDialogData): Promise<boolean> {
    const ref = this.dialogs.open<UiConfirmDialog, ConfirmDialogData, boolean>(
      UiConfirmDialog,
      data,
    );
    return (await firstValueFrom(ref.closed)) === true;
  }

  private async run(action: Observable<unknown>, success: string): Promise<boolean> {
    try {
      await firstValueFrom(action);
      this.toast.show(success, { tone: 'success' });
      return true;
    } catch (error) {
      this.toast.show(isApiError(error) ? error.detail : messages.errors.unknown, {
        tone: 'error',
      });
      return false;
    }
  }
}
