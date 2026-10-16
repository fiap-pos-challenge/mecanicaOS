import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { QuoteGateway } from '../data/quote.gateway';
import { OpenServiceOrder } from '../data/quote.models';

@Component({
  selector: 'app-new-quote-dialog',
  imports: [UiButton, UiDialogLayout, UiSkeleton],
  templateUrl: './new-quote-dialog.html',
  styleUrl: './quote-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewQuoteDialog {
  protected readonly ref = inject<DialogRef<string>>(DialogRef);
  private readonly gateway = inject(QuoteGateway);

  protected readonly text = messages.quotes.create;
  protected readonly orders = signal<readonly OpenServiceOrder[]>([]);
  protected readonly loading = signal(true);
  protected readonly selected = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      this.orders.set(await firstValueFrom(this.gateway.openServiceOrders()));
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.loading.set(false);
    }
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.selected();
    if (!id) {
      this.error.set(this.text.required);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      const quote = await firstValueFrom(this.gateway.create(id));
      this.ref.close(quote.id);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.saving.set(false);
    }
  }
}
