import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Plus as PlusIcon, Search as SearchIcon } from 'lucide';
import { debounceTime, distinctUntilChanged, firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { ConfirmDialogData, UiConfirmDialog } from '../../../shared/ui/dialog/confirm-dialog';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatBrl } from '../../../shared/util/money';
import { CanDirective } from '../../auth/can.directive';
import { AuthStore } from '../../auth/state/auth.store';
import { CatalogGateway } from '../data/catalog.gateway';
import { marginPercent, savingsPercent } from '../data/catalog.math';
import {
  CatalogItemMap,
  CatalogKind,
  CatalogPackage,
  CatalogPart,
  CatalogService,
  CatalogStatus,
} from '../data/catalog.models';

export const CATALOG_PAGE_SIZE = 20;
export const SEARCH_DEBOUNCE_MS = 300;

export interface CatalogRow {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
  readonly values: Readonly<Record<string, string>>;
  readonly belowMinimum: boolean;
  readonly negativeSavings: boolean;
}

const text = messages.catalog;
const quantity = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });

export function serviceRow(service: CatalogService): CatalogRow {
  return {
    id: service.id,
    name: service.name,
    isActive: service.isActive,
    values: {
      minutes: text.duration(service.estimatedMinutes),
      basePrice: formatBrl(service.basePrice),
      commission: text.percent(service.commissionPercent),
    },
    belowMinimum: false,
    negativeSavings: false,
  };
}

export function packageRow(pack: CatalogPackage): CatalogRow {
  const percent = savingsPercent(pack.itemsTotal, pack.price);
  return {
    id: pack.id,
    name: pack.name,
    isActive: pack.isActive,
    values: {
      items: text.list.itemsCount(pack.items.length),
      price: formatBrl(pack.price),
      savings: `${formatBrl(pack.savings)}${percent === null ? '' : ` (${text.percent(percent)})`}`,
    },
    belowMinimum: false,
    negativeSavings: pack.savings < 0,
  };
}

export function partRow(part: CatalogPart): CatalogRow {
  const margin = marginPercent(part.unitPrice, part.cost);
  return {
    id: part.id,
    name: part.name,
    isActive: part.isActive,
    values: {
      code: part.code,
      type: text.partTypes[part.type],
      unitPrice: formatBrl(part.unitPrice),
      margin: margin === null ? '—' : text.percent(margin),
      stock: text.list.stock(
        `${quantity.format(part.stockQuantity)} ${part.unitOfMeasure}`,
        quantity.format(part.minimumStock),
      ),
    },
    belowMinimum: part.stockQuantity < part.minimumStock,
    negativeSavings: false,
  };
}

export function toRow<K extends CatalogKind>(kind: K, item: CatalogItemMap[K]): CatalogRow {
  switch (kind) {
    case 'services':
      return serviceRow(item as CatalogService);
    case 'packages':
      return packageRow(item as CatalogPackage);
    default:
      return partRow(item as CatalogPart);
  }
}

const columnKeys: Readonly<Record<CatalogKind, readonly string[]>> = {
  services: ['name', 'minutes', 'basePrice', 'commission', 'status', 'actions'],
  packages: ['name', 'items', 'price', 'savings', 'status', 'actions'],
  parts: ['name', 'code', 'type', 'unitPrice', 'margin', 'stock', 'status', 'actions'],
};
const endAligned = new Set(['basePrice', 'price', 'savings', 'unitPrice', 'margin', 'commission']);

@Component({
  selector: 'app-catalog-list-page',
  imports: [
    CanDirective,
    ReactiveFormsModule,
    RouterLink,
    UiButton,
    UiCell,
    UiControl,
    UiField,
    UiIcon,
    UiPendingNotice,
    UiStatusPill,
    UiTable,
  ],
  templateUrl: './catalog-list-page.html',
  styleUrl: './catalog-list-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogListPage {
  readonly kind = input.required<CatalogKind>();

  private readonly gateway = inject(CatalogGateway);
  private readonly auth = inject(AuthStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);
  private request = 0;

  protected readonly text = text.list;
  protected readonly statuses = text.statuses;
  protected readonly icons = { plus: PlusIcon, search: SearchIcon };
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly pageSize = CATALOG_PAGE_SIZE;

  protected readonly term = signal('');
  protected readonly status = signal<CatalogStatus | null>(null);
  protected readonly page = signal(1);
  protected readonly rows = signal<readonly CatalogRow[]>([]);
  protected readonly totalItems = signal(0);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly pending = signal(false);
  protected readonly pendingText = messages.pending.catalog;

  protected readonly canSeeCosts = computed(() => this.auth.can('catalog.viewCosts'));
  protected readonly label = computed(() => text.kinds[this.kind()]);
  protected readonly newLabel = computed(() => text.newItem[this.kind()]);
  protected readonly columns = computed<readonly TableColumn<CatalogRow>[]>(() =>
    columnKeys[this.kind()]
      .filter((key) => key !== 'margin' || this.canSeeCosts())
      .map((key) => ({
        key,
        header: this.text.columns[key as keyof typeof this.text.columns],
        ...(endAligned.has(key) ? { align: 'end' as const } : {}),
        value: (row: CatalogRow) => row.values[key] ?? '',
      })),
  );
  protected readonly trackRow = (row: CatalogRow): string => row.id;

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.term.set(term);
        this.page.set(1);
        void this.load();
      });
    effect(() => {
      this.kind();
      untracked(() => {
        this.search.setValue('', { emitEvent: false });
        this.term.set('');
        this.status.set(null);
        this.page.set(1);
        void this.load();
      });
    });
  }

  protected setStatus(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.status.set(value === 'Active' || value === 'Inactive' ? value : null);
    this.page.set(1);
    void this.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    void this.load();
  }

  protected link(row: CatalogRow): string[] {
    return ['/catalog', this.kind(), row.id];
  }

  protected async toggle(row: CatalogRow): Promise<void> {
    if (row.isActive) {
      const confirmed = await this.confirm({
        title: this.text.deactivate,
        body: this.text.deactivateOf(row.name),
        confirm: this.text.deactivate,
        danger: true,
      });
      if (!confirmed) {
        return;
      }
    }

    try {
      await firstValueFrom(this.gateway.setActive(this.kind(), row.id, !row.isActive));
      this.toast.show(row.isActive ? this.text.deactivated : this.text.reactivated, {
        tone: 'success',
      });
      await this.load();
    } catch (error) {
      if (isApiError(error) && error.code === 'catalog.usedByActivePackage') {
        await this.confirm({
          title: this.text.blockedTitle,
          body: error.detail,
          confirm: this.text.close,
          alert: true,
        });
        return;
      }

      this.toast.show(isApiError(error) ? error.detail : messages.errors.unknown, {
        tone: 'error',
      });
    }
  }

  protected async load(): Promise<void> {
    const current = ++this.request;
    const kind = this.kind();
    this.loading.set(true);
    this.failed.set(false);
    this.pending.set(false);
    try {
      const page = await firstValueFrom(
        this.gateway.list(kind, {
          page: this.page(),
          pageSize: CATALOG_PAGE_SIZE,
          term: this.term(),
          status: this.status(),
        }),
      );
      if (current !== this.request) {
        return;
      }

      this.rows.set(page.items.map((item) => toRow(kind, item)));
      this.totalItems.set(page.totalItems);
    } catch (error) {
      if (current === this.request) {
        this.pending.set(isFeaturePending(error));
        this.failed.set(!isFeaturePending(error));
        this.rows.set([]);
        this.totalItems.set(0);
      }
    } finally {
      if (current === this.request) {
        this.loading.set(false);
      }
    }
  }

  private async confirm(data: ConfirmDialogData): Promise<boolean> {
    const ref = this.dialogs.open<UiConfirmDialog, ConfirmDialogData, boolean>(
      UiConfirmDialog,
      data,
    );
    return (await firstValueFrom(ref.closed)) === true;
  }
}
