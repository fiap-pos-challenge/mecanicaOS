import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { MIN_REASON_LENGTH } from '../data/quote.math';
import { QuoteEditorStore } from '../state/quote-editor.store';

@Component({
  selector: 'app-reject-quote-dialog',
  imports: [UiButton, UiControl, UiDialogLayout, UiField],
  templateUrl: './reject-quote-dialog.html',
  styleUrl: './quote-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RejectQuoteDialog {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  private readonly store = inject<QuoteEditorStore>(DIALOG_DATA);

  protected readonly text = messages.quotes.reject;
  protected readonly reason = signal('');
  protected readonly invalid = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected setReason(event: Event): void {
    this.reason.set((event.target as HTMLTextAreaElement).value);
    this.invalid.set(false);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    if (this.reason().trim().length < MIN_REASON_LENGTH) {
      this.invalid.set(true);
      return;
    }

    this.saving.set(true);
    const done = await this.store.reject(this.reason());
    this.saving.set(false);
    if (done) {
      this.ref.close(true);
    } else {
      this.error.set(this.store.actionError());
    }
  }
}
