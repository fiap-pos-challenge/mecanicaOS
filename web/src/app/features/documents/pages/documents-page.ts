import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatDate } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { DocumentDialog, DocumentDialogData } from '../components/document-dialog';
import { DocumentKind, DocumentSummary } from '../data/document.models';
import { DocumentsGateway, provideDocumentsData } from '../data/documents.gateway';

type KindFilter = DocumentKind | 'all';
type LoadState = 'loading' | 'ready' | 'pending' | 'error';

export const DOCUMENTS_PAGE_SIZE = 20;

const text = messages.documents.list;
const kinds = messages.documents.kinds;

@Component({
  selector: 'app-documents-page',
  imports: [
    UiButton,
    UiCell,
    UiControl,
    UiField,
    UiPendingNotice,
    UiSegmented,
    UiStatusPill,
    UiTable,
  ],
  providers: [provideDocumentsData()],
  template: `
    <header class="head">
      <div>
        <h1 class="page-title">{{ screen }}</h1>
        <p class="sub">{{ text.subtitle }}</p>
      </div>
    </header>
    @if (state() === 'pending') {
      <ui-pending-notice [message]="pendingText" />
    } @else {
      <div class="filters">
        <ui-segmented
          [label]="text.kind"
          [options]="kindOptions"
          [value]="kind()"
          (valueChange)="setKind($event)"
        />
        <ui-field class="search" [label]="text.search">
          <input
            uiControl
            type="search"
            maxlength="30"
            [value]="code()"
            (input)="setCode($event)"
          />
        </ui-field>
      </div>
      @if (state() === 'error') {
        <div class="alert" role="alert">
          <span>{{ text.loadError }}</span>
          <button uiButton size="sm" type="button" (click)="load()">{{ text.retry }}</button>
        </div>
      }
      <ui-table
        [caption]="text.caption"
        [columns]="columns"
        [rows]="rows()"
        [trackBy]="trackRow"
        [loading]="state() === 'loading'"
        [emptyMessage]="text.empty"
        [page]="page()"
        [pageSize]="pageSize"
        [totalItems]="total()"
        (pageChange)="goTo($event)"
      >
        <ng-template uiCell="code" let-row>
          <span class="mono">{{ row.code }}</span>
        </ng-template>
        <ng-template uiCell="kind" let-row>
          <ui-status-pill [tone]="tone(row.kind)">{{ kindLabel(row.kind) }}</ui-status-pill>
        </ng-template>
        <ng-template uiCell="actions" let-row>
          <button uiButton size="sm" variant="ghost" type="button" (click)="open(row)">
            {{ text.open }}
          </button>
        </ng-template>
      </ui-table>
    }
  `,
  styles: `
    :host {
      display: block;
      padding-top: var(--space-2);
    }
    .head {
      margin-bottom: var(--space-4);
    }
    .page-title {
      margin: 0;
    }
    .sub {
      margin: var(--space-1) 0 0;
      color: var(--muted);
      font-size: var(--text-sm);
    }
    .filters {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: var(--space-3);
      margin-bottom: var(--space-4);
    }
    .search {
      min-width: 220px;
    }
    .mono {
      font-family: var(--f-mono);
    }
    .alert {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      margin-bottom: var(--space-4);
      padding: var(--space-3);
      border: 1px solid var(--bad);
      border-radius: var(--radius-md);
      background: var(--bad-soft);
      font-size: var(--text-sm);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentsPage {
  private readonly gateway = inject(DocumentsGateway);
  private readonly dialogs = inject(UiDialogService);

  protected readonly text = text;
  protected readonly screen = messages.screens.documents;
  protected readonly pendingText = messages.pending.documents;
  protected readonly pageSize = DOCUMENTS_PAGE_SIZE;
  protected readonly kindOptions: readonly SegmentedOption<KindFilter>[] = [
    { value: 'all', label: text.all },
    { value: 'ServiceOrder', label: kinds.ServiceOrder },
    { value: 'Quote', label: kinds.Quote },
    { value: 'Receipt', label: kinds.Receipt },
  ];
  protected readonly columns: readonly TableColumn<DocumentSummary>[] = [
    { key: 'code', header: text.columns.code },
    { key: 'kind', header: text.columns.kind },
    { key: 'number', header: text.columns.number, value: (row) => `#${row.number}` },
    { key: 'customer', header: text.columns.customer, value: (row) => row.customerName },
    {
      key: 'total',
      header: text.columns.total,
      align: 'end',
      value: (row) => formatBrl(row.total),
    },
    { key: 'issuedAt', header: text.columns.issuedAt, value: (row) => formatDate(row.issuedAt) },
    { key: 'actions', header: text.columns.actions, align: 'end' },
  ];
  protected readonly kind = signal<KindFilter>('all');
  protected readonly code = signal('');
  protected readonly page = signal(1);
  protected readonly rows = signal<readonly DocumentSummary[]>([]);
  protected readonly total = signal(0);
  protected readonly state = signal<LoadState>('loading');
  protected readonly trackRow = (row: DocumentSummary): string => row.code;

  constructor() {
    void this.load();
  }

  protected kindLabel(kind: DocumentKind): string {
    return kinds[kind];
  }

  protected tone(kind: DocumentKind): 'info' | 'violet' | 'ok' {
    return kind === 'Receipt' ? 'ok' : kind === 'Quote' ? 'violet' : 'info';
  }

  protected setKind(kind: KindFilter): void {
    this.kind.set(kind);
    this.page.set(1);
    void this.load();
  }

  protected setCode(event: Event): void {
    this.code.set((event.target as HTMLInputElement).value);
    this.page.set(1);
    void this.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    void this.load();
  }

  protected open(row: DocumentSummary): void {
    this.dialogs.open<DocumentDialog, DocumentDialogData>(DocumentDialog, {
      code: row.code,
      allowSend: true,
    });
  }

  protected async load(): Promise<void> {
    this.state.set('loading');
    try {
      const kind = this.kind();
      const result = await firstValueFrom(
        this.gateway.list({
          kind: kind === 'all' ? null : kind,
          code: this.code(),
          page: this.page(),
          pageSize: DOCUMENTS_PAGE_SIZE,
        }),
      );
      this.rows.set(result.items);
      this.total.set(result.totalItems);
      this.state.set('ready');
    } catch (error) {
      this.rows.set([]);
      this.total.set(0);
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }
}
