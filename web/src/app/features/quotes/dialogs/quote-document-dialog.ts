import { DIALOG_DATA } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { DocumentViewer } from '../../documents/components/document-viewer';
import { DocumentModel } from '../../documents/data/document.models';
import { lineTotal, QuotePricing } from '../data/quote.math';
import { PaymentPlan, Quote, QuoteLine } from '../data/quote.models';
import { QuoteEditorStore } from '../state/quote-editor.store';

export function quoteDocument(
  quote: Quote,
  lines: readonly QuoteLine[],
  pricing: QuotePricing,
  payment: PaymentPlan,
): DocumentModel {
  return {
    kind: 'Quote',
    code: null,
    number: quote.number,
    version: quote.version,
    issuedAt: quote.updatedAt,
    shopName: messages.app.name,
    customerName: quote.customer.name,
    vehicle: `${quote.vehicle.plate} · ${quote.vehicle.description}`,
    lines: lines.map((line) => ({
      name: line.name,
      quantity: line.quantity,
      unitPrice: line.unitPrice,
      total: lineTotal(line),
    })),
    services: pricing.services,
    parts: pricing.parts,
    discount: pricing.discount,
    total: pricing.total,
    paidAt: null,
    paymentMethods: payment.methods,
  };
}

@Component({
  selector: 'app-quote-document-dialog',
  imports: [DocumentViewer, UiDialogLayout],
  template: `
    <ui-dialog-layout [title]="text.preview">
      @if (document(); as current) {
        <app-document-viewer [document]="current" />
      }
    </ui-dialog-layout>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuoteDocumentDialog {
  private readonly store = inject<QuoteEditorStore>(DIALOG_DATA);

  protected readonly text = messages.quotes.document;
  protected readonly document = computed(() => {
    const quote = this.store.quote();
    const draft = this.store.draft();
    return quote && draft
      ? quoteDocument(quote, draft.lines, this.store.pricing(), draft.payment)
      : null;
  });
}
