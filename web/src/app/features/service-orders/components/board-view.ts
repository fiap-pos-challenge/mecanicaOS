import { CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import {
  SERVICE_ORDER_STATUSES,
  ServiceOrderCard,
  ServiceOrderStatus,
} from '../data/service-order.models';
import { checkTransition } from '../data/service-order.rules';
import { MoveRequest, OrderCard } from './order-card';

@Component({
  selector: 'app-board-view',
  imports: [CdkDrag, CdkDropList, CdkDropListGroup, OrderCard],
  templateUrl: './board-view.html',
  styleUrl: './board-view.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardView {
  readonly cards = input.required<readonly ServiceOrderCard[]>();
  readonly now = input(Date.now());
  readonly canMove = input(true);
  readonly moving = input<string | null>(null);
  readonly moved = output<MoveRequest>();

  protected readonly text = messages.serviceOrders;
  protected readonly dragging = signal<ServiceOrderCard | null>(null);
  protected readonly columns = computed(() =>
    SERVICE_ORDER_STATUSES.map((status) => {
      const cards = this.cards().filter((card) => card.status === status);
      const dragging = this.dragging();
      return {
        status,
        cards,
        title: this.text.board.column(this.text.statuses[status], cards.length),
        allowed: dragging !== null && checkTransition(dragging, status).allowed,
        blocked:
          dragging !== null &&
          dragging.status !== status &&
          !checkTransition(dragging, status).allowed,
      };
    }),
  );

  protected drop(
    event: CdkDragDrop<ServiceOrderStatus, ServiceOrderStatus, ServiceOrderCard>,
  ): void {
    this.dragging.set(null);
    if (event.previousContainer === event.container) {
      return;
    }

    this.moved.emit({ card: event.item.data, to: event.container.data });
  }
}
