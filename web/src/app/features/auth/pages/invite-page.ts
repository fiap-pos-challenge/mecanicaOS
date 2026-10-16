import { DOCUMENT, Location } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Eye as EyeIcon, EyeOff as EyeOffIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { formatDateTime } from '../../../shared/util/date';
import { Invitation, InvitationsGateway } from '../data/invitations.gateway';

export type InviteState =
  'loading' | 'ready' | 'done' | 'missing' | 'notFound' | 'expired' | 'used' | 'failed';

export type PasswordRule = 'length' | 'upper' | 'lower' | 'digit' | 'symbol';

export const PASSWORD_RULES: readonly PasswordRule[] = [
  'length',
  'upper',
  'lower',
  'digit',
  'symbol',
];

export function passwordRules(password: string): Readonly<Record<PasswordRule, boolean>> {
  return {
    length: password.length >= 8 && password.length <= 64,
    upper: /[A-Z]/.test(password),
    lower: /[a-z]/.test(password),
    digit: /\d/.test(password),
    symbol: /[^A-Za-z\d\s]/.test(password),
  };
}

export function tokenFromHash(hash: string): string | null {
  const token = new URLSearchParams(hash.replace(/^#/, '')).get('token');
  return token && token.length <= 128 ? token : null;
}

const STATE_BY_CODE: Readonly<Record<string, InviteState>> = {
  'identity.invitationNotFound': 'notFound',
  'identity.invitationExpired': 'expired',
  'identity.invitationAlreadyUsed': 'used',
};

@Component({
  selector: 'app-invite-page',
  imports: [ReactiveFormsModule, RouterLink, UiButton, UiControl, UiField, UiIcon, UiPlate],
  templateUrl: './invite-page.html',
  styleUrls: ['./auth-layout.scss', './invite-page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InvitePage {
  private readonly gateway = inject(InvitationsGateway);
  private readonly token: string | null;

  protected readonly text = messages.auth;
  protected readonly invite = messages.auth.invitation;
  protected readonly roleLabels = messages.roles;
  protected readonly rules = PASSWORD_RULES;
  protected readonly icons = { show: EyeIcon, hide: EyeOffIcon };

  protected readonly state = signal<InviteState>('loading');
  protected readonly invitation = signal<Invitation | null>(null);
  protected readonly submitting = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly submitted = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected readonly form = new FormGroup({
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    confirm: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  private readonly password = toSignal(this.form.controls.password.valueChanges, {
    initialValue: '',
  });
  protected readonly ruleStatus = computed(() => passwordRules(this.password()));
  protected readonly strong = computed(() => Object.values(this.ruleStatus()).every(Boolean));

  constructor() {
    const document = inject(DOCUMENT);
    this.token = tokenFromHash(document.location.hash);
    inject(Location).replaceState('/invite');
    void this.inspect();
  }

  protected validUntil(invitation: Invitation): string {
    return this.invite.validUntil(formatDateTime(invitation.expiresAt));
  }

  protected passwordError(): string | null {
    if (!this.submitted()) {
      return null;
    }

    return this.serverError() ?? (this.strong() ? null : this.invite.weak);
  }

  protected confirmError(): string | null {
    const value = this.form.getRawValue();
    return this.submitted() && value.password !== value.confirm ? this.invite.mismatch : null;
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    this.serverError.set(null);
    const value = this.form.getRawValue();
    if (!this.token || !this.strong() || value.password !== value.confirm) {
      return;
    }

    this.submitting.set(true);
    try {
      await firstValueFrom(this.gateway.accept(this.token, value.password));
      this.form.reset();
      this.state.set('done');
    } catch (error) {
      if (isApiError(error) && error.status === 400) {
        this.serverError.set(error.fieldErrors[0]?.message ?? error.detail);
      } else {
        this.state.set(this.failureState(error));
      }
    } finally {
      this.submitting.set(false);
    }
  }

  protected toggle(): void {
    this.showPassword.update((value) => !value);
  }

  private async inspect(): Promise<void> {
    if (!this.token) {
      this.state.set('missing');
      return;
    }

    try {
      this.invitation.set(await firstValueFrom(this.gateway.inspect(this.token)));
      this.state.set('ready');
    } catch (error) {
      this.state.set(this.failureState(error));
    }
  }

  private failureState(error: unknown): InviteState {
    return (isApiError(error) ? STATE_BY_CODE[error.code] : undefined) ?? 'failed';
  }
}
