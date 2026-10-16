import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { StatusTone, UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDateTime } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { InventoryGateway } from '../data/inventory.gateway';
import { isValidQuantityFor, orderTotal } from '../data/inventory.math';
import { PurchaseOrder, PurchaseSuggestion, Supplier } from '../data/inventory.models';
import { ORDER_TONES } from './inventory-display';

export interface SuggestionLine {
  readonly suggestion: PurchaseSuggestion;
  readonly selected: boolean;
  readonly quantity: number;
}

export interface SupplierGroup {
  readonly supplierId: string | null;
  readonly supplierName: string;
  readonly lines: readonly SuggestionLine[];
}

const text = messages.inventory;

export function groupBySupplier(
  lines: readonly SuggestionLine[],
  suppliers: readonly Supplier[],
): readonly SupplierGroup[] {
  const groups = new Map<string, SuggestionLine[]>();
  for (const line of lines) {
    const key = line.suggestion.supplierId ?? '';
    groups.set(key, [...(groups.get(key) ?? []), line]);
  }

  return [...groups].map(([key, items]) => ({
    supplierId: key || null,
    supplierName: suppliers.find((supplier) => supplier.id === key)?.name ?? text.orders.noSupplier,
    lines: items,
  }));
}

@Component({
  selector: 'app-purchase-orders-page',
  imports: [UiButton, UiCell, UiPendingNotice, UiStatusPill, UiTable],
  templateUrl: './purchase-orders-page.html',
  styleUrl: './purchase-orders-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseOrdersPage {
  private readonly gateway = inject(InventoryGateway);
  private readonly toast = inject(ToastService);

  protected readonly text = text.orders;
  protected readonly tones = ORDER_TONES;
  protected readonly pendingText = messages.pending.inventory;
  protected readonly lines = signal<readonly SuggestionLine[]>([]);
  protected readonly suppliers = signal<readonly Supplier[]>([]);
  protected readonly orders = signal<readonly PurchaseOrder[]>([]);
  protected readonly loading = signal(true);
  protected readonly pending = signal(false);
  protected readonly failed = signal(false);
  protected readonly busy = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly groups = computed(() => groupBySupplier(this.lines(), this.suppliers()));
  protected readonly orderColumns: readonly TableColumn<PurchaseOrder>[] = [
    {
      key: 'number',
      header: this.text.columns.number,
      value: (row) => this.text.orderNumber(row.number),
    },
    { key: 'supplier', header: this.text.columns.supplier, value: (row) => row.supplierName },
    {
      key: 'items',
      header: this.text.columns.items,
      value: (row) => this.text.items(row.lines.length),
    },
    {
      key: 'total',
      header: this.text.columns.total,
      align: 'end',
      value: (row) => formatBrl(row.total),
    },
    { key: 'status', header: this.text.columns.status },
    { key: 'actions', header: this.text.columns.actions },
  ];
  protected readonly trackOrder = (row: PurchaseOrder): string => row.id;

  constructor() {
    void this.load();
  }

  protected amount(value: number, unit: string): string {
    return text.quantity(value, unit);
  }

  protected cost(line: SuggestionLine): string {
    return formatBrl(line.suggestion.unitCost);
  }

  protected groupTotal(group: SupplierGroup): string {
    return formatBrl(
      orderTotal(
        group.lines
          .filter((line) => line.selected)
          .map((line) => ({ unitCost: line.suggestion.unitCost, quantity: line.quantity })),
      ),
    );
  }

  protected toggle(partId: string, event: Event): void {
    const selected = (event.target as HTMLInputElement).checked;
    this.lines.update((lines) =>
      lines.map((line) => (line.suggestion.partId === partId ? { ...line, selected } : line)),
    );
  }

  protected setQuantity(partId: string, event: Event): void {
    const quantity = Number((event.target as HTMLInputElement).value);
    this.lines.update((lines) =>
      lines.map((line) => (line.suggestion.partId === partId ? { ...line, quantity } : line)),
    );
  }

  protected invalid(line: SuggestionLine): boolean {
    return line.selected && !isValidQuantityFor(line.suggestion.type, line.quantity);
  }

  protected canCreate(group: SupplierGroup): boolean {
    const selected = group.lines.filter((line) => line.selected);
    return (
      group.supplierId !== null &&
      selected.length > 0 &&
      selected.every((line) => !this.invalid(line))
    );
  }

  protected async create(group: SupplierGroup): Promise<void> {
    if (!group.supplierId || !this.canCreate(group)) {
      return;
    }

    this.busy.set(group.supplierId);
    this.error.set(null);
    try {
      const order = await firstValueFrom(
        this.gateway.createPurchaseOrder({
          supplierId: group.supplierId,
          lines: group.lines
            .filter((line) => line.selected)
            .map((line) => ({
              partId: line.suggestion.partId,
              quantity: line.quantity,
              unitCost: line.suggestion.unitCost,
            })),
        }),
      );
      this.toast.show(this.text.draftCreated(order.number), { tone: 'success' });
      this.orders.update((orders) => [order, ...orders]);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.busy.set(null);
    }
  }

  protected async send(order: PurchaseOrder): Promise<void> {
    this.busy.set(order.id);
    this.error.set(null);
    try {
      const sent = await firstValueFrom(this.gateway.sendPurchaseOrder(order.id));
      this.toast.show(this.text.sent, { tone: 'success' });
      this.orders.update((orders) => orders.map((item) => (item.id === sent.id ? sent : item)));
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.busy.set(null);
    }
  }

  protected statusTone(order: PurchaseOrder): StatusTone {
    return ORDER_TONES[order.status];
  }

  protected statusLabel(order: PurchaseOrder): string {
    return this.text.statuses[order.status];
  }

  protected created(order: PurchaseOrder): string {
    return formatDateTime(order.createdAt);
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.pending.set(false);
    this.failed.set(false);
    try {
      const [suggestions, suppliers, orders] = await Promise.all([
        firstValueFrom(this.gateway.suggestions()),
        firstValueFrom(this.gateway.suppliers()),
        firstValueFrom(this.gateway.purchaseOrders()),
      ]);
      this.lines.set(
        suggestions.map((suggestion) => ({
          suggestion,
          selected: true,
          quantity: suggestion.suggestedQuantity,
        })),
      );
      this.suppliers.set(suppliers);
      this.orders.set(orders);
    } catch (error) {
      this.pending.set(isFeaturePending(error));
      this.failed.set(!isFeaturePending(error));
    } finally {
      this.loading.set(false);
    }
  }
}
