import { ChangeDetectionStrategy, Component, inject, Injectable, signal } from '@angular/core';
import { X as XIcon } from 'lucide';
import { UiIcon } from '../icon/icon';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiIconButton } from '../icon-button/icon-button';

export type ToastTone = 'info' | 'success' | 'error';

export interface Toast {
  readonly id: number;
  readonly message: string;
  readonly tone: ToastTone;
  readonly detail: string | null;
}

export interface ToastOptions {
  readonly tone?: ToastTone;
  readonly detail?: string;
  readonly durationMs?: number;
}

const defaultDurationMs = 5000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly toasts = signal<readonly Toast[]>([]);

  show(message: string, options: ToastOptions = {}): number {
    const id = ++this.nextId;
    const toast: Toast = {
      id,
      message,
      tone: options.tone ?? 'info',
      detail: options.detail ?? null,
    };
    this.toasts.update((toasts) => [...toasts, toast]);

    const duration = options.durationMs ?? defaultDurationMs;
    if (duration > 0) {
      setTimeout(() => this.dismiss(id), duration);
    }

    return id;
  }

  dismiss(id: number): void {
    this.toasts.update((toasts) => toasts.filter((toast) => toast.id !== id));
  }
}

@Component({
  selector: 'ui-toast-region',
  imports: [UiIcon, UiIconButton],
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiToastRegion {
  protected readonly service = inject(ToastService);
  protected readonly text = messages.ui.toast;
  protected readonly closeIcon = XIcon;
}
