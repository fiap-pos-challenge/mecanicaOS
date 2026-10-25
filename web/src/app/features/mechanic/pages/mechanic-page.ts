import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { PwaService } from '../../../core/pwa/pwa.service';
import { UiButton } from '../../../shared/ui/button/button';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { STATUS_TONES } from '../../service-orders/service-order-display';
import { SyncBanner } from '../components/sync-banner';
import { MechanicStore } from '../state/mechanic.store';

@Component({
  selector: 'app-mechanic-page',
  imports: [RouterLink, SyncBanner, UiButton, UiPendingNotice, UiPlate, UiSkeleton, UiStatusPill],
  template: `
    <header class="head">
      <div>
        <h1 class="page-title">{{ screen }}</h1>
        <p class="sub">{{ text.subtitle }}</p>
      </div>
      @if (pwa.canInstall()) {
        <button uiButton type="button" (click)="install()">{{ text.pwa.install }}</button>
      }
    </header>
    <app-sync-banner class="block" />
    @switch (store.listState()) {
      @case ('loading') {
        <ui-skeleton height="200px" />
      }
      @case ('pending') {
        <ui-pending-notice [message]="pendingText" />
      }
      @case ('error') {
        <div class="alert" role="alert">
          <span>{{ text.loadError }}</span>
          <button uiButton size="sm" type="button" (click)="store.loadMine()">
            {{ text.retry }}
          </button>
        </div>
      }
      @case ('ready') {
        <ul class="orders">
          @for (order of store.mine(); track order.id) {
            <li>
              <a
                class="order"
                [routerLink]="['/mechanic', order.id]"
                [attr.aria-label]="text.open(order.number)"
              >
                <span class="row">
                  <strong class="mono">{{ orderText.number(order.number) }}</strong>
                  <ui-status-pill [tone]="tones[order.status]">{{
                    orderText.statuses[order.status]
                  }}</ui-status-pill>
                </span>
                <span class="row">
                  <span>{{ order.vehicle }}</span>
                  <ui-plate [value]="order.plate" />
                </span>
                <span class="muted">{{ order.complaint }}</span>
                @if (store.pendingByOrder()[order.id]; as count) {
                  <span class="pending">{{ syncText.pending(count) }}</span>
                }
              </a>
            </li>
          } @empty {
            <li class="muted">{{ text.empty }}</li>
          }
        </ul>
      }
    }
  `,
  styles: `
    :host {
      display: block;
      padding-top: var(--space-2);
    }
    .head {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      gap: var(--space-3);
      margin-bottom: var(--space-4);
    }
    .page-title {
      margin: 0;
    }
    .sub,
    .muted {
      margin: var(--space-1) 0 0;
      color: var(--muted);
      font-size: var(--text-sm);
    }
    .block {
      margin-bottom: var(--space-4);
    }
    .alert {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
      justify-content: space-between;
      padding: var(--space-3);
      border: 1px solid var(--bad);
      border-radius: var(--radius-md);
      background: var(--bad-soft);
    }
    .orders {
      display: grid;
      gap: var(--space-3);
      margin: 0;
      padding: 0;
      list-style: none;
    }
    .order {
      display: grid;
      gap: var(--space-1);
      padding: var(--space-4);
      border: 1px solid var(--line);
      border-radius: var(--radius-lg);
      background: var(--surface);
      color: inherit;
      text-decoration: none;
    }
    .row {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      gap: var(--space-2);
      align-items: center;
    }
    .mono {
      font-family: var(--f-mono);
    }
    .pending {
      color: var(--amber);
      font-size: var(--text-sm);
      font-weight: 600;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MechanicPage {
  protected readonly store = inject(MechanicStore);
  protected readonly pwa = inject(PwaService);
  private readonly toast = inject(ToastService);

  protected readonly screen = messages.screens.mechanic;
  protected readonly text = messages.mechanic;
  protected readonly syncText = messages.mechanic.sync;
  protected readonly orderText = messages.serviceOrders;
  protected readonly pendingText = messages.pending.serviceOrders;
  protected readonly tones = STATUS_TONES;

  constructor() {
    void this.store.loadMine();
  }

  protected async install(): Promise<void> {
    if (await this.pwa.install()) {
      this.toast.show(this.text.pwa.installed, { tone: 'success' });
    }
  }
}
