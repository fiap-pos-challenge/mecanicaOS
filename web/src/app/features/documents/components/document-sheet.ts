import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { formatBrl } from '../../../shared/util/money';
import { DocumentModel } from '../data/document.models';

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
});
const quantityFormatter = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });

export function formatDocumentDate(iso: string): string {
  return dateFormatter.format(new Date(iso));
}

export function verificationUrl(origin: string, code: string): string {
  return `${origin}/v/${encodeURIComponent(code)}`;
}

@Component({
  selector: 'app-document-sheet',
  templateUrl: './document-sheet.html',
  styleUrl: './document-sheet.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentSheet {
  readonly document = input.required<DocumentModel>();

  protected readonly text = messages.documents;
  private readonly origin = inject(DOCUMENT).defaultView?.location.origin ?? '';
  protected readonly view = computed(() => {
    const document = this.document();
    const kind = this.text.kinds[document.kind];
    return {
      title: this.text.title(kind, document.number, document.version),
      issued: this.text.issuedAt(formatDocumentDate(document.issuedAt)),
      lines: document.lines.map((line, index) => ({
        index,
        name: line.name,
        quantity: quantityFormatter.format(line.quantity),
        unit: formatBrl(line.unitPrice),
        total: formatBrl(line.total),
      })),
      services: formatBrl(document.services),
      parts: formatBrl(document.parts),
      discount: `− ${formatBrl(document.discount)}`,
      total: formatBrl(document.total),
      paid: document.paidAt ? this.text.paidAt(formatDocumentDate(document.paidAt)) : null,
      methods: document.paymentMethods
        .map((method) => messages.quotes.paymentMethods[method])
        .join(' · '),
      verification: document.code
        ? {
            code: this.text.verification(document.code),
            url: this.text.verifyAt(verificationUrl(this.origin, document.code)),
          }
        : null,
    };
  });
}
