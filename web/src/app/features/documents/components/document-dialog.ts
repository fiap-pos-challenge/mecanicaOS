import { DIALOG_DATA } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { DocumentModel } from '../data/document.models';
import { DocumentsGateway, provideDocumentsData } from '../data/documents.gateway';
import { DocumentViewer } from './document-viewer';

export interface DocumentDialogData {
  readonly code?: string;
  readonly document?: DocumentModel;
  readonly portalSession?: string;
  readonly allowSend?: boolean;
}

@Component({
  selector: 'app-document-dialog',
  imports: [DocumentViewer, UiDialogLayout, UiSkeleton],
  providers: [provideDocumentsData()],
  template: `
    <ui-dialog-layout [title]="text.preview">
      @if (document(); as current) {
        <app-document-viewer
          [document]="current"
          [portalSession]="data.portalSession ?? null"
          [allowSend]="data.allowSend ?? false"
        />
      } @else if (error(); as message) {
        <p role="alert">{{ message }}</p>
      } @else {
        <ui-skeleton height="240px" />
      }
    </ui-dialog-layout>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentDialog {
  protected readonly data = inject<DocumentDialogData>(DIALOG_DATA);
  private readonly gateway = inject(DocumentsGateway);

  protected readonly text = messages.documents;
  protected readonly document = signal<DocumentModel | null>(this.data.document ?? null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    if (!this.data.document && this.data.code) {
      void this.load(this.data.code);
    }
  }

  private async load(code: string): Promise<void> {
    try {
      this.document.set(await firstValueFrom(this.gateway.get(code, this.data.portalSession)));
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : this.text.loadError);
    }
  }
}
