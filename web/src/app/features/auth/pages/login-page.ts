import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Eye as EyeIcon, EyeOff as EyeOffIcon } from 'lucide';
import { SessionService } from '../../../core/auth/session.service';
import { isApiError, NETWORK_ERROR_CODE, TIMEOUT_ERROR_CODE } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { formatCountdown } from '../../../shared/util/duration';
import { AuthStore } from '../state/auth.store';

export const DEFAULT_RETRY_AFTER_SECONDS = 60;

export type LoginFailure =
  | { readonly kind: 'message'; readonly message: string; readonly traceId: string | null }
  | { readonly kind: 'wait'; readonly locked: boolean };

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiField, UiIcon, UiPlate],
  templateUrl: './login-page.html',
  styleUrl: './auth-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly session = inject(SessionService);
  private readonly passwordInput = viewChild.required<ElementRef<HTMLInputElement>>('password');
  private ticker: ReturnType<typeof setInterval> | null = null;

  protected readonly text = messages.auth;
  protected readonly supportCode = messages.errors.supportCode;
  protected readonly icons = { show: EyeIcon, hide: EyeOffIcon };
  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected readonly submitting = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly failure = signal<LoginFailure | null>(null);
  protected readonly secondsLeft = signal(0);
  protected readonly waiting = computed(() => this.secondsLeft() > 0);

  protected readonly notice = computed(() => {
    const reason = this.session.endReason();
    return reason === null ? null : this.text.notices[reason];
  });

  protected readonly errorMessage = computed(() => {
    const failure = this.failure();
    return failure?.kind === 'message' ? failure.message : null;
  });

  protected readonly errorTrace = computed(() => {
    const failure = this.failure();
    return failure?.kind === 'message' ? failure.traceId : null;
  });

  protected readonly waitMessage = computed(() => {
    const failure = this.failure();
    if (failure?.kind !== 'wait') {
      return null;
    }

    if (!this.waiting()) {
      return this.text.errors.waitOver;
    }

    const time = formatCountdown(this.secondsLeft());
    return failure.locked
      ? this.text.errors.accountLocked(time)
      : this.text.errors.tooManyRequests(time);
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stopTicker());
  }

  protected togglePassword(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected async submit(): Promise<void> {
    if (this.submitting() || this.waiting()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.failure.set({ kind: 'message', message: this.text.login.required, traceId: null });
      return;
    }

    this.submitting.set(true);
    this.failure.set(null);
    try {
      await this.auth.login({
        email: this.form.controls.email.value.trim(),
        password: this.form.controls.password.value,
      });
    } catch (error) {
      this.handleFailure(error);
    } finally {
      this.submitting.set(false);
    }
  }

  private handleFailure(error: unknown): void {
    this.form.controls.password.reset();
    if (!isApiError(error)) {
      this.failure.set({ kind: 'message', message: this.text.errors.server, traceId: null });
      return;
    }

    if (error.status === 429) {
      this.failure.set({ kind: 'wait', locked: error.code === 'identity.accountLocked' });
      this.startWait(error.retryAfterSeconds ?? DEFAULT_RETRY_AFTER_SECONDS);
      return;
    }

    const message =
      error.code === 'identity.accountDisabled'
        ? this.text.errors.accountDisabled
        : error.status === 401 || error.status === 400
          ? this.text.errors.invalidCredentials
          : error.code === NETWORK_ERROR_CODE || error.code === TIMEOUT_ERROR_CODE
            ? this.text.errors.network
            : this.text.errors.server;
    const traceId = error.status >= 500 ? error.traceId : null;
    this.failure.set({ kind: 'message', message, traceId });
    this.passwordInput().nativeElement.focus();
  }

  private startWait(seconds: number): void {
    this.stopTicker();
    const deadline = Date.now() + seconds * 1000;
    this.secondsLeft.set(seconds);
    this.ticker = setInterval(() => {
      const left = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
      this.secondsLeft.set(left);
      if (left === 0) {
        this.stopTicker();
      }
    }, 1000);
  }

  private stopTicker(): void {
    if (this.ticker !== null) {
      clearInterval(this.ticker);
      this.ticker = null;
    }
  }
}
