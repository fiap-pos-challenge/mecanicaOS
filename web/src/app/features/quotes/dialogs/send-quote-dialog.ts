import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { ToastService } from '../../../shared/ui/toast/toast';
import { DELIVERY_CHANNELS, DeliveryChannel, Quote } from '../data/quote.models';
import { QuoteEditorStore } from '../state/quote-editor.store';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';

export const MAX_MESSAGE_LENGTH = 1000;

export function channelAllowed(quote: Quote, channel: DeliveryChannel): boolean {
  switch (channel) {
    case 'WhatsApp':
      return quote.customer.consentWhatsApp;
    case 'Email':
      return quote.customer.consentEmail;
    case 'Link':
      return true;
  }
}

@Component({
  selector: 'app-send-quote-dialog',
  imports: [UiAiBadge, UiButton, UiControl, UiDialogLayout, UiField],
  templateUrl: './send-quote-dialog.html',
  styleUrl: './quote-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SendQuoteDialog {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly store = inject<QuoteEditorStore>(DIALOG_DATA);
  private readonly toast = inject(ToastService);
  private readonly document = inject(DOCUMENT);

  protected readonly text = messages.quotes.send;
  protected readonly channelText = messages.quotes.channels;
  protected readonly quote = computed(() => this.store.quote());
  protected readonly channels = computed(() => {
    const quote = this.quote();
    return DELIVERY_CHANNELS.map((channel) => ({
      channel,
      allowed: quote !== null && channelAllowed(quote, channel),
    }));
  });
  protected readonly channel = signal<DeliveryChannel>(
    this.channels().find((item) => item.allowed)?.channel ?? 'Link',
  );
  protected readonly expirationDays = computed(() => this.store.options()?.expirationDays ?? [7]);
  protected readonly expiresInDays = signal(
    this.expirationDays().includes(7) ? 7 : (this.expirationDays()[0] ?? 7),
  );
  protected readonly message = signal('');
  protected readonly loadingMessage = signal(true);
  protected readonly sending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly recipient = computed(() => {
    const quote = this.quote();
    if (!quote) {
      return '';
    }

    const contact =
      this.channel() === 'WhatsApp'
        ? quote.customer.maskedPhone
        : this.channel() === 'Email'
          ? quote.customer.maskedEmail
          : null;
    return contact ? `${contact} · ${quote.customer.name}` : this.text.linkOnly;
  });

  constructor() {
    void this.loadMessage();
  }

  private async loadMessage(): Promise<void> {
    const message = await this.store.messageDraft(this.channel());
    this.message.set(message ?? '');
    this.loadingMessage.set(false);
  }

  protected select(channel: DeliveryChannel): void {
    this.channel.set(channel);
  }

  protected setExpiration(event: Event): void {
    this.expiresInDays.set(Number((event.target as HTMLSelectElement).value));
  }

  protected setMessage(event: Event): void {
    this.message.set((event.target as HTMLTextAreaElement).value);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const message = this.message().trim();
    if (message.length === 0 || message.length > MAX_MESSAGE_LENGTH) {
      this.error.set(this.text.messageRequired);
      return;
    }

    this.sending.set(true);
    this.error.set(null);
    const result = await this.store.send({
      channel: this.channel(),
      expiresInDays: this.expiresInDays(),
      message,
    });
    this.sending.set(false);
    if (!result) {
      this.error.set(this.store.actionError() ?? messages.errors.unknown);
      return;
    }

    const view = this.document.defaultView;
    if (result.whatsAppUrl) {
      view?.open(result.whatsAppUrl, '_blank', 'noopener');
    }

    if (result.delivery.channel === 'Link' && view?.navigator.clipboard) {
      await view.navigator.clipboard.writeText(result.delivery.link).catch(() => undefined);
      this.toast.show(this.text.copied, { tone: 'success' });
    }

    this.toast.show(this.text.queued, { tone: 'success' });
    this.ref.close(true);
  }
}
