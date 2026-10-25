import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  signal,
  viewChildren,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { isApiError, NETWORK_ERROR_CODE, TIMEOUT_ERROR_CODE } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { AuthStore } from '../state/auth.store';

export const CODE_LENGTH = 6;

@Component({
  selector: 'app-two-factor-page',
  imports: [RouterLink, UiButton, UiPlate],
  templateUrl: './two-factor-page.html',
  styleUrls: ['./auth-layout.scss', './two-factor-page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TwoFactorPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly inputs = viewChildren<ElementRef<HTMLInputElement>>('digit');

  protected readonly text = messages.auth;
  protected readonly positions = Array.from({ length: CODE_LENGTH }, (_, index) => index);
  protected readonly digits = signal<readonly string[]>(Array<string>(CODE_LENGTH).fill(''));
  protected readonly code = computed(() => this.digits().join(''));
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected onSubmit(event: Event): void {
    event.preventDefault();
    void this.submit();
  }

  protected onInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    if (this.submitting()) {
      input.value = this.digits()[index] ?? '';
      return;
    }

    const typed = input.value.replace(/\D/g, '');
    if (typed.length > 1) {
      this.fill(index, typed);
      return;
    }

    this.setDigit(index, typed);
    input.value = typed;
    if (typed && index < CODE_LENGTH - 1) {
      this.focus(index + 1);
    }

    if (this.code().length === CODE_LENGTH) {
      void this.submit();
    }
  }

  protected onPaste(index: number, event: ClipboardEvent): void {
    event.preventDefault();
    this.fill(index, (event.clipboardData?.getData('text') ?? '').replace(/\D/g, ''));
  }

  protected onKeydown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && !this.digits()[index] && index > 0) {
      event.preventDefault();
      this.setDigit(index - 1, '');
      this.focus(index - 1);
    } else if (event.key === 'ArrowLeft' && index > 0) {
      this.focus(index - 1);
    } else if (event.key === 'ArrowRight' && index < CODE_LENGTH - 1) {
      this.focus(index + 1);
    }
  }

  protected async submit(): Promise<void> {
    if (this.submitting()) {
      return;
    }

    if (this.code().length !== CODE_LENGTH) {
      this.error.set(this.text.twoFactor.incomplete);
      this.focus(this.digits().findIndex((digit) => !digit));
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.verifyTwoFactor(this.code());
    } catch (error) {
      this.error.set(this.messageFor(error));
      this.digits.set(Array<string>(CODE_LENGTH).fill(''));
      this.syncInputs();
      this.focus(0);
    } finally {
      this.submitting.set(false);
    }
  }

  protected back(): void {
    this.auth.cancelTwoFactor();
    void this.router.navigateByUrl('/login');
  }

  private messageFor(error: unknown): string {
    if (
      isApiError(error) &&
      (error.code === NETWORK_ERROR_CODE || error.code === TIMEOUT_ERROR_CODE)
    ) {
      return this.text.errors.network;
    }

    return isApiError(error) && error.status < 500
      ? this.text.twoFactor.invalid
      : this.text.errors.server;
  }

  private fill(start: number, value: string): void {
    const next = [...this.digits()];
    for (let offset = 0; offset < value.length && start + offset < CODE_LENGTH; offset++) {
      next[start + offset] = value.charAt(offset);
    }

    this.digits.set(next);
    this.syncInputs();
    const firstEmpty = next.findIndex((digit) => !digit);
    this.focus(firstEmpty === -1 ? CODE_LENGTH - 1 : firstEmpty);
    if (firstEmpty === -1) {
      void this.submit();
    }
  }

  private setDigit(index: number, value: string): void {
    this.digits.update((digits) =>
      digits.map((digit, position) => (position === index ? value : digit)),
    );
  }

  private syncInputs(): void {
    this.inputs().forEach(
      (input, index) => (input.nativeElement.value = this.digits()[index] ?? ''),
    );
  }

  private focus(index: number): void {
    this.inputs()[Math.max(0, index)]?.nativeElement.focus();
  }
}
