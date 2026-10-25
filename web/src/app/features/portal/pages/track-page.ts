import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { formatDate } from '../../../shared/util/date';
import { documentKindOf } from '../../../shared/util/document';
import { formatBrl } from '../../../shared/util/money';
import { formatPlate } from '../../../shared/util/plate';
import { STATUS_TONES } from '../../service-orders/service-order-display';
import { provideTrackingData, TrackedOrder, TrackingGateway } from '../data/tracking.gateway';

const text = messages.track;

@Component({
  selector: 'app-track-page',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiField, UiPendingNotice, UiStatusPill],
  providers: [provideTrackingData()],
  template: `
    <main class="track">
      <h1>{{ screen }}</h1>
      <h2>{{ text.title }}</h2>
      <p class="sub">{{ text.subtitle }}</p>
      @if (pending()) {
        <ui-pending-notice [message]="pendingText" />
      }
      <form class="form" (submit)="submit($event)">
        <ui-field [label]="text.number">
          <input uiControl inputmode="numeric" maxlength="12" [formControl]="number" />
        </ui-field>
        <ui-field [label]="text.document">
          <input
            uiControl
            inputmode="numeric"
            maxlength="18"
            autocomplete="off"
            [formControl]="document"
          />
        </ui-field>
        <button uiButton variant="primary" type="submit" [loading]="busy()">
          {{ text.submit }}
        </button>
      </form>
      @if (error(); as message) {
        <p class="alert" role="alert">{{ message }}</p>
      }
      @if (order(); as found) {
        <section class="result" [attr.aria-label]="text.result(found.number)">
          <h2>{{ text.result(found.number) }}</h2>
          <dl>
            <div>
              <dt>{{ text.status }}</dt>
              <dd>
                <ui-status-pill [tone]="tone(found)">{{ statusLabel(found) }}</ui-status-pill>
              </dd>
            </div>
            <div>
              <dt>{{ text.vehicle }}</dt>
              <dd>{{ found.vehicle }} · {{ plate(found.plate) }}</dd>
            </div>
            <div>
              <dt>{{ text.problem }}</dt>
              <dd>{{ found.problemDescription }}</dd>
            </div>
            <div>
              <dt>{{ text.total }}</dt>
              <dd>{{ money(found) }}</dd>
            </div>
            @if (found.quoteStatus; as quote) {
              <div>
                <dt>{{ text.quote }}</dt>
                <dd>{{ quoteLabel(quote) }}</dd>
              </div>
            }
          </dl>
          <p class="sub">{{ text.openedAt(date(found.openedAt)) }}</p>
          @if (found.deliveredAt; as delivered) {
            <p class="sub">{{ text.deliveredAt(date(delivered)) }}</p>
          }
        </section>
      }
      <section class="link">
        <h2>{{ text.linkTitle }}</h2>
        <p class="sub">{{ text.linkHint }}</p>
      </section>
    </main>
  `,
  styles: `
    :host {
      display: block;
      min-height: 100dvh;
      background: var(--bg);
    }
    .track {
      display: grid;
      gap: var(--space-4);
      max-width: 520px;
      margin: 0 auto;
      padding: var(--space-6) var(--space-4);
    }
    h1,
    h2 {
      margin: 0;
    }
    h1 {
      font-size: var(--text-xl);
    }
    h2 {
      font-size: var(--text-md);
    }
    .sub {
      margin: 0;
      color: var(--muted);
      font-size: var(--text-sm);
    }
    .form {
      display: grid;
      gap: var(--space-3);
    }
    .alert {
      margin: 0;
      padding: var(--space-3);
      border: 1px solid var(--bad);
      border-radius: var(--radius-md);
      background: var(--bad-soft);
      font-size: var(--text-sm);
    }
    .result,
    .link {
      display: grid;
      gap: var(--space-2);
      padding: var(--space-4);
      border: 1px solid var(--line);
      border-radius: var(--radius-lg);
      background: var(--surface);
    }
    dl {
      display: grid;
      gap: var(--space-2);
      margin: 0;
    }
    dt {
      color: var(--muted);
      font-size: var(--text-xs);
    }
    dd {
      margin: 0;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrackPage {
  private readonly gateway = inject(TrackingGateway);

  protected readonly text = text;
  protected readonly screen = messages.screens.portal;
  protected readonly pendingText = messages.pending.portal;
  protected readonly number = new FormControl('', { nonNullable: true });
  protected readonly document = new FormControl('', { nonNullable: true });
  protected readonly busy = signal(false);
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly order = signal<TrackedOrder | null>(null);

  protected statusLabel(order: TrackedOrder): string {
    return messages.serviceOrders.statuses[order.status];
  }

  protected tone(order: TrackedOrder) {
    return STATUS_TONES[order.status];
  }

  protected quoteLabel(status: 'Pending' | 'Approved' | 'Rejected'): string {
    return text.quoteStatuses[status];
  }

  protected plate(value: string): string {
    return formatPlate(value);
  }

  protected money(order: TrackedOrder): string {
    return formatBrl(order.total);
  }

  protected date(value: string): string {
    return formatDate(value);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const number = Number(this.number.value.trim());
    const document = this.document.value;
    this.order.set(null);
    if (!Number.isInteger(number) || number <= 0 || !documentKindOf(document)) {
      this.error.set(text.invalid);
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      this.order.set(await firstValueFrom(this.gateway.lookup(number, document)));
    } catch (error) {
      this.pending.set(isFeaturePending(error));
      this.error.set(isApiError(error) && error.status === 404 ? text.notFound : text.error);
    } finally {
      this.busy.set(false);
    }
  }
}
