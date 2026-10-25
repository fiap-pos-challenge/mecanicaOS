import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { formatBrl } from '../../../shared/util/money';
import {
  SERVICE_ORDER_STATUSES,
  ServiceOrderCard,
  ServiceOrderStatus,
} from '../data/service-order.models';
import { checkTransition, tagsOf } from '../data/service-order.rules';
import { formatWhen, TAG_TONES } from '../service-order-display';

let nextMenu = 0;

export interface MoveRequest {
  readonly card: ServiceOrderCard;
  readonly to: ServiceOrderStatus;
}

@Component({
  selector: 'app-order-card',
  imports: [RouterLink, UiButton, UiPlate, UiStatusPill],
  templateUrl: './order-card.html',
  styleUrl: './order-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(keydown.escape)': 'closeMenu()' },
})
export class OrderCard {
  readonly card = input.required<ServiceOrderCard>();
  readonly now = input(Date.now());
  readonly canMove = input(true);
  readonly busy = input(false);
  readonly moved = output<MoveRequest>();

  protected readonly text = messages.serviceOrders;
  protected readonly menuOpen = signal(false);
  private readonly toggle = viewChild('toggle', { read: ElementRef<HTMLButtonElement> });
  protected readonly menuId = `move-menu-${++nextMenu}`;
  protected readonly tags = computed(() =>
    tagsOf(this.card(), this.now()).map((tag) => ({
      tag,
      tone: TAG_TONES[tag],
      label: this.text.tags[tag],
    })),
  );
  protected readonly total = computed(() => formatBrl(this.card().total));
  protected readonly promised = computed(() =>
    this.card().promisedAt === null ? null : this.text.promised(formatWhen(this.card().promisedAt)),
  );
  protected readonly targets = computed(() =>
    SERVICE_ORDER_STATUSES.filter((status) => status !== this.card().status).map((status) => {
      const check = checkTransition(this.card(), status);
      return {
        status,
        label: this.text.board.moveTo(this.text.statuses[status]),
        allowed: check.allowed,
        reason: check.allowed ? null : this.text.board.blocked[check.block],
      };
    }),
  );

  protected openMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    if (this.menuOpen()) {
      this.menuOpen.set(false);
      this.toggle()?.nativeElement.focus();
    }
  }

  protected choose(to: ServiceOrderStatus): void {
    this.menuOpen.set(false);
    this.moved.emit({ card: this.card(), to });
  }
}
