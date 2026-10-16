import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { UiToggle } from '../../../shared/ui/toggle/toggle';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatBrl } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { MoveRequest } from '../components/order-card';
import { BoardView } from '../components/board-view';
import { OrderCard } from '../components/order-card';
import {
  SERVICE_ORDER_STATUSES,
  ServiceOrderCard,
  ServiceOrderStatus,
} from '../data/service-order.models';
import { agenda, NO_FILTERS, tagsOf } from '../data/service-order.rules';
import { provideServiceOrderData } from '../data/service-order.gateway';
import { ReasonDialog } from '../dialogs/reason-dialog';
import {
  fieldValue,
  formatDay,
  formatWhen,
  STATUS_TONES,
  TAG_TONES,
} from '../service-order-display';
import { BoardStore, OrderChange } from '../state/board.store';

type View = 'board' | 'list' | 'agenda';

@Component({
  selector: 'app-service-orders-page',
  imports: [
    BoardView,
    OrderCard,
    RouterLink,
    UiButton,
    UiCell,
    UiControl,
    UiField,
    UiPendingNotice,
    UiSegmented,
    UiSkeleton,
    UiStatusPill,
    UiTable,
    UiToggle,
  ],
  providers: [provideServiceOrderData(), BoardStore],
  templateUrl: './service-orders-page.html',
  styleUrl: './service-orders-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceOrdersPage {
  protected readonly store = inject(BoardStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthStore);

  protected readonly screen = messages.screens.serviceOrders;
  protected readonly text = messages.serviceOrders;
  protected readonly pendingText = messages.pending.serviceOrders;
  protected readonly canMove = computed(() => this.auth.can('serviceOrders.manage'));
  protected readonly view = signal<View>('board');
  protected readonly views: readonly SegmentedOption<View>[] = (
    ['board', 'list', 'agenda'] as const
  ).map((value) => ({ value, label: this.text.views[value] }));
  protected readonly statuses = SERVICE_ORDER_STATUSES.map((status) => ({
    status,
    label: this.text.statuses[status],
  }));
  protected readonly blocked = signal<string | null>(null);
  protected readonly days = computed(() =>
    agenda(this.store.visible(), this.store.now()).map((day) => ({
      ...day,
      title: day.overdue ? this.text.agenda.overdue : formatDay(day.key),
    })),
  );
  protected readonly notices = computed(() =>
    this.store.changes().map((change) => ({
      number: change.number,
      title:
        change.kind === 'conflict'
          ? this.text.changes.conflict(change.number)
          : this.text.changes.remote(change.number),
      lines: this.describe(change),
    })),
  );
  protected readonly columns: readonly TableColumn<ServiceOrderCard>[] = [
    { key: 'number', header: this.text.list.columns.number },
    { key: 'customer', header: this.text.list.columns.customer, value: (row) => row.customerName },
    {
      key: 'vehicle',
      header: this.text.list.columns.vehicle,
      value: (row) => `${row.plate} · ${row.vehicle}`,
    },
    { key: 'status', header: this.text.list.columns.status },
    {
      key: 'mechanic',
      header: this.text.list.columns.mechanic,
      value: (row) => row.mechanicName ?? this.text.unassigned,
    },
    {
      key: 'promised',
      header: this.text.list.columns.promised,
      value: (row) => formatWhen(row.promisedAt),
    },
    {
      key: 'total',
      header: this.text.list.columns.total,
      align: 'end',
      value: (row) => formatBrl(row.total),
    },
  ];
  protected readonly trackRow = (row: ServiceOrderCard): string => row.id;
  protected readonly hasFilters = computed(
    () => JSON.stringify(this.store.filters()) !== JSON.stringify(NO_FILTERS),
  );

  protected statusOf(status: ServiceOrderStatus) {
    return { label: this.text.statuses[status], tone: STATUS_TONES[status] };
  }

  protected tagsOf(card: ServiceOrderCard) {
    return tagsOf(card, this.store.now()).map((tag) => ({
      tag,
      label: this.text.tags[tag],
      tone: TAG_TONES[tag],
    }));
  }

  protected describe(change: OrderChange): readonly string[] {
    return change.fields.map((field) =>
      this.text.changes.change(
        this.text.changes.fields[field],
        fieldValue(change.before, field),
        fieldValue(change.after, field),
      ),
    );
  }

  protected setTerm(event: Event): void {
    this.store.setFilters({ term: (event.target as HTMLInputElement).value });
  }

  protected setStatus(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.store.setFilters({
      status: SERVICE_ORDER_STATUSES.find((status) => status === value) ?? null,
    });
  }

  protected setMechanic(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.store.setFilters({ mechanic: value === '' ? null : value });
  }

  protected async move(request: MoveRequest): Promise<void> {
    this.blocked.set(null);
    const statusLabel = this.text.statuses[request.to];
    let result = await this.store.move(request.card.id, request.to);
    if (result.kind === 'needsReason') {
      const reason = await firstValueFrom(
        this.dialogs.open<ReasonDialog, null, string>(ReasonDialog).closed,
      );
      if (!reason) {
        return;
      }

      result = await this.store.move(request.card.id, request.to, reason);
    }

    if (result.kind === 'blocked') {
      this.blocked.set(
        `${this.text.board.blockedTitle(request.card.number, statusLabel)} ${this.text.board.blocked[result.block]}`,
      );
    } else if (result.kind === 'moved') {
      this.toast.show(this.text.board.moved(request.card.number, statusLabel), { tone: 'success' });
    }
  }
}
