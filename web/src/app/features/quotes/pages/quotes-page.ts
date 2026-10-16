import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Plus as PlusIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatBrl } from '../../../shared/util/money';
import { CanDirective } from '../../auth/can.directive';
import { QuoteGateway } from '../data/quote.gateway';
import { QuoteStatus, QuoteSummary } from '../data/quote.models';
import { NewQuoteDialog } from '../dialogs/new-quote-dialog';
import { formatDateTime, quoteStage, STAGE_TONES } from '../quote-display';

type StatusFilter = QuoteStatus | 'all';

@Component({
  selector: 'app-quotes-page',
  imports: [
    CanDirective,
    RouterLink,
    UiButton,
    UiCell,
    UiIcon,
    UiPendingNotice,
    UiSegmented,
    UiStatusPill,
    UiTable,
  ],
  templateUrl: './quotes-page.html',
  styleUrl: './quotes-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuotesPage {
  private readonly gateway = inject(QuoteGateway);
  private readonly dialogs = inject(UiDialogService);
  private readonly router = inject(Router);

  protected readonly screen = messages.screens.quotes;
  protected readonly text = messages.quotes;
  protected readonly pendingText = messages.pending.quotes;
  protected readonly icons = { plus: PlusIcon };
  protected readonly rows = signal<readonly QuoteSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly pending = signal(false);
  protected readonly failed = signal(false);
  protected readonly filter = signal<StatusFilter>('all');
  protected readonly filters: readonly SegmentedOption<StatusFilter>[] = [
    { value: 'all', label: this.text.list.all },
    { value: 'Pending', label: this.text.statuses.Pending },
    { value: 'Approved', label: this.text.statuses.Approved },
    { value: 'Rejected', label: this.text.statuses.Rejected },
  ];
  protected readonly columns: readonly TableColumn<QuoteSummary>[] = [
    { key: 'number', header: this.text.list.columns.number },
    { key: 'customer', header: this.text.list.columns.customer, value: (row) => row.customerName },
    { key: 'plate', header: this.text.list.columns.plate, value: (row) => row.plate },
    {
      key: 'version',
      header: this.text.list.columns.version,
      value: (row) => this.text.list.version(row.version),
    },
    { key: 'status', header: this.text.list.columns.status },
    {
      key: 'total',
      header: this.text.list.columns.total,
      align: 'end',
      value: (row) => formatBrl(row.total),
    },
    {
      key: 'updated',
      header: this.text.list.columns.updated,
      value: (row) => formatDateTime(row.updatedAt),
    },
  ];
  protected readonly empty = computed(() => !this.loading() && this.rows().length === 0);
  protected readonly trackRow = (row: QuoteSummary): string => row.id;

  constructor() {
    void this.load();
  }

  protected stage(row: QuoteSummary) {
    const stage = quoteStage(row.status, row.deliveryStatus);
    return { label: this.text.statuses[stage], tone: STAGE_TONES[stage] };
  }

  protected setFilter(filter: StatusFilter): void {
    this.filter.set(filter);
    void this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);
    const filter = this.filter();
    try {
      const page = await firstValueFrom(
        this.gateway.list({ status: filter === 'all' ? null : filter, page: 1, pageSize: 50 }),
      );
      this.rows.set(page.items);
      this.pending.set(false);
    } catch (error) {
      this.rows.set([]);
      this.pending.set(isFeaturePending(error));
      this.failed.set(!isFeaturePending(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async create(): Promise<void> {
    const id = await firstValueFrom(
      this.dialogs.open<NewQuoteDialog, null, string>(NewQuoteDialog).closed,
    );
    if (id) {
      await this.router.navigateByUrl(`/quotes/${id}`);
    }
  }
}
