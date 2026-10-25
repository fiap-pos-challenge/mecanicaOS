import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { BACKEND_ROLES, BackendRole } from '../../../core/auth/permissions';
import { applyApiErrors, serverErrorOf } from '../../../core/forms/apply-api-errors';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { ToastService } from '../../../shared/ui/toast/toast';
import { User } from '../data/user.models';
import { UsersGateway } from '../data/users.gateway';

@Component({
  selector: 'app-invite-member-dialog',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiDialogLayout, UiField],
  templateUrl: './invite-member-dialog.html',
  styleUrl: './team-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InviteMemberDialog {
  protected readonly ref = inject<DialogRef<User>>(DialogRef);
  private readonly users = inject(UsersGateway);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.team.invite;
  protected readonly roleLabels = messages.roles;
  protected readonly roles = BACKEND_ROLES;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(160)],
    }),
    role: new FormControl<BackendRole>('Mechanic', { nonNullable: true }),
  });

  protected errorOf(name: 'name' | 'email'): string | null {
    const control = this.form.controls[name];
    if (!control.touched || control.valid) {
      return null;
    }

    const server = serverErrorOf(control);
    if (server) {
      return server;
    }

    if (control.hasError('required')) {
      return this.text.required;
    }

    return name === 'name' ? this.text.invalidName : this.text.invalidEmail;
  }

  protected async send(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    try {
      const user = await firstValueFrom(
        this.users.invite({ name: value.name.trim(), email: value.email.trim(), role: value.role }),
      );
      this.toast.show(this.text.sent(user.name), { tone: 'success' });
      this.ref.close(user);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.saving.set(false);
    }
  }

  private handleError(error: unknown): void {
    if (!isApiError(error)) {
      this.error.set(messages.errors.unknown);
      return;
    }

    if (error.code === 'identity.duplicateEmail') {
      const email = this.form.controls.email;
      email.setErrors({ server: error.detail });
      email.markAsTouched();
      return;
    }

    const unmatched = applyApiErrors(this.form, error);
    this.error.set(error.fieldErrors.length > 0 && unmatched.length === 0 ? null : error.detail);
  }
}
