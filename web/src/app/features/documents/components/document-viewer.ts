import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { ToastService } from '../../../shared/ui/toast/toast';
import { DocumentChannel, DocumentModel } from '../data/document.models';
import { DocumentsGateway } from '../data/documents.gateway';
import { DocumentSheet, verificationUrl } from './document-sheet';

@Component({
  selector: 'app-document-viewer',
  imports: [DocumentSheet, UiButton],
  template: `
    <div class="actions" role="group" [attr.aria-label]="text.actions">
      <button uiButton type="button" (click)="print()">{{ text.print }}</button>
      @if (document().code; as code) {
        <button uiButton type="button" [loading]="busy()" (click)="download(code)">
          {{ text.download }}
        </button>
        <button uiButton type="button" (click)="copy(code)">{{ text.copyLink }}</button>
        @if (allowSend()) {
          <button uiButton type="button" (click)="send(code, 'WhatsApp')">
            {{ text.sendWhatsApp }}
          </button>
          <button uiButton type="button" (click)="send(code, 'Email')">{{ text.sendEmail }}</button>
        }
      }
    </div>
    @if (error(); as message) {
      <p class="alert" role="alert">{{ message }}</p>
    }
    <app-document-sheet [document]="document()" />
  `,
  styles: `
    :host {
      display: grid;
      gap: var(--space-3);
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
    }
    .alert {
      margin: 0;
      padding: var(--space-3);
      border: 1px solid var(--bad);
      border-radius: var(--radius-md);
      background: var(--bad-soft);
      font-size: var(--text-sm);
    }
    @media print {
      .actions {
        display: none;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentViewer {
  readonly document = input.required<DocumentModel>();
  readonly portalSession = input<string | null>(null);
  readonly allowSend = input(false);

  private readonly gateway = inject(DocumentsGateway);
  private readonly toast = inject(ToastService);
  private readonly page = inject(DOCUMENT);

  protected readonly text = messages.documents;
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected print(): void {
    this.page.defaultView?.print();
  }

  protected async download(code: string): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      const file = await firstValueFrom(
        this.gateway.download(code, this.portalSession() ?? undefined),
      );
      const link = this.page.createElement('a');
      link.href = file.url;
      link.download = file.fileName;
      link.rel = 'noopener';
      link.click();
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.busy.set(false);
    }
  }

  protected async copy(code: string): Promise<void> {
    const view = this.page.defaultView;
    await view?.navigator.clipboard
      ?.writeText(verificationUrl(view.location.origin, code))
      .catch(() => undefined);
    this.toast.show(this.text.copied, { tone: 'success' });
  }

  protected async send(code: string, channel: DocumentChannel): Promise<void> {
    this.error.set(null);
    try {
      await firstValueFrom(this.gateway.send(code, channel));
      this.toast.show(this.text.queued, { tone: 'success' });
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    }
  }
}
