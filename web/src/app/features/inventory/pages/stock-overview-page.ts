import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  ClipboardList as CountIcon,
  PackagePlus as EntryIcon,
  Search as SearchIcon,
  ShoppingCart as OrderIcon,
} from 'lucide';
import { debounceTime, distinctUntilChanged, firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { KpiTrend, UiKpi } from '../../../shared/ui/kpi/kpi';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { StatusTone, UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatBrl } from '../../../shared/util/money';
import { CanDirective } from '../../auth/can.directive';
import { AuthStore } from '../../auth/state/auth.store';
import { InventoryGateway } from '../data/inventory.gateway';
import { levelPercent, situationOf } from '../data/inventory.math';
import { STOCK_SITUATIONS, StockItem, StockKpis, StockSituation } from '../data/inventory.models';
import { StockEntryDialog } from '../dialogs/stock-entry-dialog';
import { StockExitDialog } from '../dialogs/stock-exit-dialog';
import { SITUATION_TONES } from './inventory-display';

export const STOCK_PAGE_SIZE = 20;
export const SEARCH_DEBOUNCE_MS = 300;

export interface StockRow {
  readonly item: StockItem;
  readonly situation: StockSituation;
  readonly level: number;
}

export interface KpiCard {
  readonly label: string;
  readonly value: string;
  readonly delta: string | null;
  readonly trend: KpiTrend;
}

const text = messages.inventory;

export function toStockRow(item: StockItem, now: number = Date.now()): StockRow {
  return {
    item,
    situation: situationOf(item, now),
    level: levelPercent(item.stockQuantity, item.minimumStock),
  };
}

export function kpiCards(kpis: StockKpis | null): readonly KpiCard[] {
  const labels = text.overview.kpis;
  const none = labels.none;
  const delta = kpis?.outflowMonthDeltaPercent ?? null;
  return [
    {
      label: labels.belowMinimum,
      value: kpis ? String(kpis.belowMinimum) : none,
      delta: kpis ? labels.suggested(formatBrl(kpis.suggestedOrderTotal)) : null,
      trend: 'neutral',
    },
    {
      label: labels.outflow,
      value: kpis ? labels.outflowValue(kpis.outflowMonth) : none,
      delta: delta === null ? null : labels.delta(delta),
      trend: delta === null || delta === 0 ? 'neutral' : delta > 0 ? 'up' : 'down',
    },
    {
      label: labels.turnover,
      value: kpis?.turnoverDays ? labels.turnoverValue(kpis.turnoverDays) : none,
      delta: kpis ? labels.turnoverGoal(kpis.turnoverGoalDays) : null,
      trend: 'neutral',
    },
    {
      label: labels.idle,
      value: kpis ? String(kpis.idleItems) : none,
      delta: kpis ? labels.idleValue(formatBrl(kpis.idleValue)) : null,
      trend: 'neutral',
    },
  ];
}

@Component({
  selector: 'app-stock-overview-page',
  imports: [
    CanDirective,
    ReactiveFormsModule,
    RouterLink,
    UiButton,
    UiCell,
    UiControl,
    UiField,
    UiIcon,
    UiKpi,
    UiPendingNotice,
    UiStatusPill,
    UiTable,
  ],
  templateUrl: './stock-overview-page.html',
  styleUrl: './stock-overview-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockOverviewPage {
  private readonly gateway = inject(InventoryGateway);
  private readonly auth = inject(AuthStore);
  private readonly dialogs = inject(UiDialogService);
  private request = 0;

  protected readonly text = text.overview;
  protected readonly situations = text.situations;
  protected readonly situationOptions = STOCK_SITUATIONS;
  protected readonly tones = SITUATION_TONES;
  protected readonly pendingText = messages.pending.inventory;
  protected readonly icons = {
    search: SearchIcon,
    entry: EntryIcon,
    count: CountIcon,
    order: OrderIcon,
  };
  protected readonly pageSize = STOCK_PAGE_SIZE;
  protected readonly search = new FormControl('', { nonNullable: true });

  protected readonly term = signal('');
  protected readonly situation = signal<StockSituation | null>(null);
  protected readonly location = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly rows = signal<readonly StockRow[]>([]);
  protected readonly totalItems = signal(0);
  protected readonly locations = signal<readonly string[]>([]);
  protected readonly kpis = signal<StockKpis | null>(null);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly pending = signal(false);

  protected readonly canSeeCosts = computed(() => this.auth.can('catalog.viewCosts'));
  protected readonly cards = computed(() => kpiCards(this.kpis()));
  protected readonly hasFilters = computed(
    () => this.term() !== '' || this.situation() !== null || this.location() !== null,
  );
  protected readonly stockValue = computed(() => {
    const kpis = this.kpis();
    return kpis ? this.text.stockValue(kpis.totalItems, formatBrl(kpis.stockValue)) : null;
  });
  protected readonly columns = computed<readonly TableColumn<StockRow>[]>(() => {
    const labels = this.text.columns;
    return [
      { key: 'code', header: labels.code },
      { key: 'name', header: labels.name },
      { key: 'location', header: labels.location, value: (row) => row.item.location ?? '—' },
      { key: 'balance', header: labels.balance },
      { key: 'level', header: labels.level },
      {
        key: 'outflow',
        header: labels.outflow,
        align: 'end',
        value: (row) => text.quantity(row.item.outflow30d, row.item.unitOfMeasure),
      },
      ...(this.canSeeCosts()
        ? [
            {
              key: 'cost',
              header: labels.cost,
              align: 'end' as const,
              value: (row: StockRow) => formatBrl(row.item.unitCost),
            },
          ]
        : []),
      { key: 'situation', header: labels.situation },
      { key: 'actions', header: labels.actions },
    ];
  });
  protected readonly trackRow = (row: StockRow): string => row.item.partId;

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.term.set(term.trim());
        this.page.set(1);
        void this.load();
      });
    void this.load();
  }

  protected setSituation(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.situation.set(STOCK_SITUATIONS.find((situation) => situation === value) ?? null);
    this.page.set(1);
    void this.load();
  }

  protected setLocation(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.location.set(value || null);
    this.page.set(1);
    void this.load();
  }

  protected clear(): void {
    this.search.setValue('', { emitEvent: false });
    this.term.set('');
    this.situation.set(null);
    this.location.set(null);
    this.page.set(1);
    void this.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    void this.load();
  }

  protected situationTone(row: StockRow): StatusTone {
    return SITUATION_TONES[row.situation];
  }

  protected situationLabel(row: StockRow): string {
    return text.situations[row.situation];
  }

  protected balance(row: StockRow): string {
    return this.text.balance(
      text.quantity(row.item.stockQuantity, row.item.unitOfMeasure),
      text.quantity(row.item.minimumStock, row.item.unitOfMeasure),
    );
  }

  protected openEntry(): void {
    this.dialogs.open(StockEntryDialog).closed.subscribe((saved) => {
      if (saved) {
        void this.load();
      }
    });
  }

  protected openExit(row: StockRow): void {
    this.dialogs.open(StockExitDialog, row.item).closed.subscribe((saved) => {
      if (saved) {
        void this.load();
      }
    });
  }

  protected async load(): Promise<void> {
    const current = ++this.request;
    this.loading.set(true);
    this.failed.set(false);
    this.pending.set(false);
    try {
      const overview = await firstValueFrom(
        this.gateway.overview({
          page: this.page(),
          pageSize: STOCK_PAGE_SIZE,
          term: this.term(),
          situation: this.situation(),
          location: this.location(),
        }),
      );
      if (current !== this.request) {
        return;
      }

      const now = Date.now();
      this.rows.set(overview.items.map((item) => toStockRow(item, now)));
      this.totalItems.set(overview.totalItems);
      this.locations.set(overview.locations);
      this.kpis.set(overview.kpis);
    } catch (error) {
      if (current === this.request) {
        this.rows.set([]);
        this.totalItems.set(0);
        this.kpis.set(null);
        this.pending.set(isFeaturePending(error));
        this.failed.set(!isFeaturePending(error));
      }
    } finally {
      if (current === this.request) {
        this.loading.set(false);
      }
    }
  }
}
