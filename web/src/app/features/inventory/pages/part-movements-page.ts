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
import { RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { StatusTone, UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatDateTime } from '../../../shared/util/date';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { InventoryGateway } from '../data/inventory.gateway';
import { situationOf } from '../data/inventory.math';
import { PartMovements, StockMovement } from '../data/inventory.models';
import { MOVEMENT_TONES, SITUATION_TONES } from './inventory-display';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error' | 'pending';

const text = messages.inventory;

export function signedQuantity(movement: StockMovement, unit: string): string {
  const negative = movement.type === 'Out' || movement.quantity < 0;
  return `${negative ? '−' : '+'}${text.quantity(Math.abs(movement.quantity), unit)}`;
}

@Component({
  selector: 'app-part-movements-page',
  imports: [RouterLink, UiCell, UiIcon, UiPendingNotice, UiSkeleton, UiStatusPill, UiTable],
  templateUrl: './part-movements-page.html',
  styleUrl: './part-movements-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PartMovementsPage {
  readonly partId = input.required<string>();

  private readonly gateway = inject(InventoryGateway);
  private request = 0;

  protected readonly text = text.movements;
  protected readonly situations = text.situations;
  protected readonly situationTones = SITUATION_TONES;
  protected readonly movementTones = MOVEMENT_TONES;
  protected readonly pendingText = messages.pending.inventory;
  protected readonly icons = { back: ArrowLeftIcon };
  protected readonly state = signal<LoadState>('loading');
  protected readonly data = signal<PartMovements | null>(null);
  protected readonly situation = computed(() => {
    const data = this.data();
    return data ? situationOf(data.item) : null;
  });
  protected readonly columns: readonly TableColumn<StockMovement>[] = [
    { key: 'when', header: this.text.columns.when, value: (row) => formatDateTime(row.occurredAt) },
    { key: 'type', header: this.text.columns.type },
    { key: 'quantity', header: this.text.columns.quantity, align: 'end' },
    { key: 'balance', header: this.text.columns.balance, align: 'end' },
    { key: 'reason', header: this.text.columns.reason, value: (row) => row.reason },
    { key: 'user', header: this.text.columns.user, value: (row) => row.userName },
    {
      key: 'order',
      header: this.text.columns.order,
      value: (row) => (row.serviceOrderNumber ? this.text.order(row.serviceOrderNumber) : '—'),
    },
  ];
  protected readonly trackRow = (row: StockMovement): string => row.id;

  constructor() {
    effect(() => {
      const id = this.partId();
      untracked(() => void this.load(id));
    });
  }

  protected quantity(row: StockMovement): string {
    return signedQuantity(row, this.data()?.item.unitOfMeasure ?? '');
  }

  protected movementTone(row: StockMovement): StatusTone {
    return MOVEMENT_TONES[row.type];
  }

  protected movementLabel(row: StockMovement): string {
    return this.text.types[row.type];
  }

  protected amount(value: number): string {
    return text.quantity(value, this.data()?.item.unitOfMeasure ?? '');
  }

  private async load(id: string): Promise<void> {
    const current = ++this.request;
    this.state.set('loading');
    try {
      const data = await firstValueFrom(this.gateway.movements(id));
      if (current === this.request) {
        this.data.set(data);
        this.state.set('ready');
      }
    } catch (error) {
      if (current === this.request) {
        this.state.set(
          isFeaturePending(error)
            ? 'pending'
            : isApiError(error) && error.status === 404
              ? 'notFound'
              : 'error',
        );
      }
    }
  }
}
