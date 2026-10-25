import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiKpi } from '../../../shared/ui/kpi/kpi';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatDate } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { FinanceGateway, provideFinanceData } from '../data/finance.gateway';
import {
  Commission,
  FINANCE_PERIODS,
  FinancePeriod,
  FinanceSummary,
  MethodTotal,
  Receipt,
  Receivable,
} from '../data/finance.models';

type LoadState = 'loading' | 'ready' | 'pending' | 'error';

const text = messages.finance;

@Component({
  selector: 'app-finance-page',
  imports: [
    RouterLink,
    UiButton,
    UiCard,
    UiCell,
    UiKpi,
    UiPendingNotice,
    UiSegmented,
    UiSkeleton,
    UiTable,
  ],
  providers: [provideFinanceData()],
  templateUrl: './finance-page.html',
  styleUrl: './finance-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinancePage {
  private readonly gateway = inject(FinanceGateway);

  protected readonly text = text;
  protected readonly screen = messages.screens.finance;
  protected readonly pendingText = messages.errors.featurePending;
  protected readonly periods: readonly SegmentedOption<FinancePeriod>[] = FINANCE_PERIODS.map(
    (value) => ({ value, label: text.periods[value] }),
  );
  protected readonly period = signal<FinancePeriod>('month');
  protected readonly state = signal<LoadState>('loading');
  protected readonly summary = signal<FinanceSummary | null>(null);
  protected readonly kpis = computed(() => {
    const kpis = this.summary()?.kpis;
    if (!kpis) {
      return [];
    }

    return [
      {
        label: text.kpis.received,
        value: formatBrl(kpis.received),
        delta: text.orders(kpis.orders),
      },
      {
        label: text.kpis.receivable,
        value: formatBrl(kpis.receivable),
        delta: text.receivableCount(kpis.receivableCount),
      },
      { label: text.kpis.commissions, value: formatBrl(kpis.commissions), delta: null },
      { label: text.kpis.averageTicket, value: formatBrl(kpis.averageTicket), delta: null },
    ];
  });
  protected readonly methodColumns: readonly TableColumn<MethodTotal>[] = [
    { key: 'method', header: text.columns.method, value: (row) => row.method },
    { key: 'count', header: text.columns.count, align: 'end', value: (row) => row.count },
    {
      key: 'amount',
      header: text.columns.amount,
      align: 'end',
      value: (row) => formatBrl(row.amount),
    },
  ];
  protected readonly receivableColumns: readonly TableColumn<Receivable>[] = [
    { key: 'order', header: text.columns.order },
    { key: 'customer', header: text.columns.customer, value: (row) => row.customerName },
    { key: 'waiting', header: text.columns.waiting, value: (row) => text.days(row.daysWaiting) },
    {
      key: 'amount',
      header: text.columns.amount,
      align: 'end',
      value: (row) => formatBrl(row.amount),
    },
  ];
  protected readonly receiptColumns: readonly TableColumn<Receipt>[] = [
    { key: 'order', header: text.columns.order },
    { key: 'customer', header: text.columns.customer, value: (row) => row.customerName },
    { key: 'date', header: text.columns.date, value: (row) => formatDate(row.deliveredAt) },
    { key: 'method', header: text.columns.method, value: (row) => row.method },
    {
      key: 'amount',
      header: text.columns.amount,
      align: 'end',
      value: (row) => formatBrl(row.amount),
    },
  ];
  protected readonly commissionColumns: readonly TableColumn<Commission>[] = [
    { key: 'mechanic', header: text.columns.mechanic, value: (row) => row.mechanicName },
    { key: 'orders', header: text.columns.orders, align: 'end', value: (row) => row.orders },
    {
      key: 'services',
      header: text.columns.services,
      align: 'end',
      value: (row) => formatBrl(row.services),
    },
    {
      key: 'commission',
      header: text.columns.commission,
      align: 'end',
      value: (row) => formatBrl(row.commission),
    },
  ];
  protected readonly trackMethod = (row: MethodTotal): string => row.method;
  protected readonly trackOrder = (row: Receipt | Receivable): string => row.serviceOrderId;
  protected readonly trackCommission = (row: Commission): string => row.mechanicName;

  constructor() {
    void this.load();
  }

  protected setPeriod(period: FinancePeriod): void {
    this.period.set(period);
    void this.load();
  }

  protected async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.summary.set(await firstValueFrom(this.gateway.summary(this.period())));
      this.state.set('ready');
    } catch (error) {
      this.summary.set(null);
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }
}
