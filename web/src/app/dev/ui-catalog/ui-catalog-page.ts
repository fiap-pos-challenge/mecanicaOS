import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { UiAvatar } from '../../shared/ui/avatar/avatar';
import { UiButton } from '../../shared/ui/button/button';
import { UiCard } from '../../shared/ui/card/card';
import { UiChip } from '../../shared/ui/chip/chip';
import { UiDialogService } from '../../shared/ui/dialog/dialog';
import { UiEmptyState } from '../../shared/ui/empty-state/empty-state';
import { UiControl, UiField } from '../../shared/ui/field/field';
import { KpiTrend, UiKpi } from '../../shared/ui/kpi/kpi';
import { UiPlate } from '../../shared/ui/plate/plate';
import { UiSegmented } from '../../shared/ui/segmented/segmented';
import { UiSkeleton } from '../../shared/ui/skeleton/skeleton';
import { StatusTone, UiStatusPill } from '../../shared/ui/status-pill/status-pill';
import { TableColumn, TableSort, UiCell, UiTable } from '../../shared/ui/table/table';
import { UiTabs } from '../../shared/ui/tabs/tabs';
import { ToastService } from '../../shared/ui/toast/toast';
import { UiToggle } from '../../shared/ui/toggle/toggle';
import { DevDialogExample } from './dev-dialog-example';
import { uiCatalogTexts } from './ui-catalog-texts';

interface SampleOrder {
  readonly number: number;
  readonly vehicle: string;
  readonly plate: string;
  readonly status: StatusTone;
  readonly statusLabel: string;
  readonly totalCents: number;
}

type Period = 'today' | 'week' | 'month';

const sampleOrders: readonly SampleOrder[] = [
  {
    number: 3321,
    vehicle: 'VW Polo',
    plate: 'FJK4E21',
    status: 'info',
    statusLabel: uiCatalogTexts.pills.info,
    totalCents: 128_000,
  },
  {
    number: 3318,
    vehicle: 'Honda Civic',
    plate: 'BRA2E19',
    status: 'bad',
    statusLabel: uiCatalogTexts.pills.bad,
    totalCents: 254_990,
  },
  {
    number: 3310,
    vehicle: 'Fiat Uno',
    plate: 'ABC1234',
    status: 'ok',
    statusLabel: uiCatalogTexts.pills.ok,
    totalCents: 46_500,
  },
  {
    number: 3302,
    vehicle: 'Renault Kwid',
    plate: 'QWE9R87',
    status: 'warn',
    statusLabel: uiCatalogTexts.pills.warn,
    totalCents: 89_900,
  },
];

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

@Component({
  selector: 'app-ui-catalog-page',
  imports: [
    UiAvatar,
    UiButton,
    UiCard,
    UiCell,
    UiChip,
    UiControl,
    UiEmptyState,
    UiField,
    UiKpi,
    UiPlate,
    UiSegmented,
    UiSkeleton,
    UiStatusPill,
    UiTable,
    UiTabs,
    UiToggle,
  ],
  templateUrl: './ui-catalog-page.html',
  styleUrl: './ui-catalog-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiCatalogPage {
  private readonly dialog = inject(UiDialogService);
  private readonly toast = inject(ToastService);

  protected readonly text = uiCatalogTexts;
  protected readonly period = signal<Period>('week');
  protected readonly periodOptions = [
    { value: 'today' as const, label: uiCatalogTexts.segmented.today },
    { value: 'week' as const, label: uiCatalogTexts.segmented.week },
    { value: 'month' as const, label: uiCatalogTexts.segmented.month },
  ];
  protected readonly notify = signal(true);
  protected readonly tab = signal('packages');
  protected readonly tabLabel = computed(
    () => this.text.tabs.find((tab) => tab.id === this.tab())?.label ?? '',
  );
  protected readonly kpiTrends: readonly KpiTrend[] = this.text.kpis.map((kpi) => kpi.trend);

  protected readonly tableLoading = signal(false);
  protected readonly tableEmpty = signal(false);
  protected readonly sort = signal<TableSort | null>({ key: 'number', direction: 'desc' });
  protected readonly page = signal(1);
  protected readonly columns: readonly TableColumn<SampleOrder>[] = [
    {
      key: 'number',
      header: uiCatalogTexts.table.number,
      sortable: true,
      value: (order) => order.number,
    },
    { key: 'vehicle', header: uiCatalogTexts.table.vehicle, sortable: true },
    { key: 'status', header: uiCatalogTexts.table.status },
    {
      key: 'total',
      header: uiCatalogTexts.table.total,
      sortable: true,
      align: 'end',
      value: (order) => brl.format(order.totalCents / 100),
    },
  ];
  protected readonly rows = computed(() => {
    if (this.tableEmpty()) {
      return [];
    }

    const sort = this.sort();
    const sorted = [...sampleOrders];
    if (sort) {
      const value = (order: SampleOrder): string | number =>
        sort.key === 'vehicle'
          ? order.vehicle
          : sort.key === 'total'
            ? order.totalCents
            : order.number;
      sorted.sort(
        (first, second) =>
          (value(first) > value(second) ? 1 : -1) * (sort.direction === 'asc' ? 1 : -1),
      );
    }

    return sorted;
  });
  protected readonly trackOrder = (order: SampleOrder): number => order.number;

  protected openDialog(): void {
    this.dialog.open(DevDialogExample);
  }

  protected showInfo(): void {
    this.toast.show(this.text.toast.infoMessage, { tone: 'success' });
  }

  protected showError(): void {
    this.toast.show(this.text.toast.errorMessage, {
      tone: 'error',
      detail: this.text.toast.errorDetail,
    });
  }
}
