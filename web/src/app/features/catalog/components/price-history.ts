import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiCard } from '../../../shared/ui/card/card';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { formatDateTime } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { CatalogGateway } from '../data/catalog.gateway';
import { CatalogKind, PriceChange } from '../data/catalog.models';

type HistoryState = 'loading' | 'ready' | 'error' | 'pending';

@Component({
  selector: 'app-price-history',
  imports: [UiCard, UiPendingNotice],
  template: `
    <ui-card [title]="text.title" [headingLevel]="2">
      @switch (state()) {
        @case ('loading') {
          <p class="muted" role="status">{{ loadingText }}</p>
        }
        @case ('pending') {
          <ui-pending-notice class="pending" [message]="pendingText" />
        }
        @case ('error') {
          <p class="muted" role="alert">{{ text.loadError }}</p>
        }
        @default {
          @if (entries().length === 0) {
            <p class="muted">{{ text.empty }}</p>
          } @else {
            <ol class="timeline" [attr.aria-label]="text.caption">
              @for (entry of entries(); track $index) {
                <li class="entry">
                  <span class="dot" aria-hidden="true"></span>
                  <div class="body">
                    <p class="what">
                      <strong>{{ text.fields[entry.field] }}</strong>
                      @if (entry.oldValue === null) {
                        <span class="badge">{{ text.created }}</span>
                        <span class="mono">{{ money(entry.newValue) }}</span>
                      } @else {
                        <span class="mono old">{{ money(entry.oldValue) }}</span>
                        <span aria-hidden="true">→</span>
                        <span class="visually-hidden">{{ text.to }}</span>
                        <span class="mono">{{ money(entry.newValue) }}</span>
                      }
                    </p>
                    <p class="meta">{{ entry.changedBy }} · {{ when(entry.changedAt) }}</p>
                  </div>
                </li>
              }
            </ol>
          }
        }
      }
    </ui-card>
  `,
  styleUrl: './price-history.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PriceHistory {
  readonly kind = input.required<CatalogKind>();
  readonly itemId = input.required<string>();
  readonly version = input(0);

  private readonly gateway = inject(CatalogGateway);

  protected readonly text = messages.catalog.history;
  protected readonly loadingText = messages.ui.loading;
  protected readonly pendingText = messages.pending.priceHistory;
  protected readonly state = signal<HistoryState>('loading');
  protected readonly entries = signal<readonly PriceChange[]>([]);

  constructor() {
    effect(() => {
      const kind = this.kind();
      const id = this.itemId();
      this.version();
      untracked(() => void this.load(kind, id));
    });
  }

  protected money(value: PriceChange['newValue']): string {
    return formatBrl(value);
  }

  protected when(value: string): string {
    return formatDateTime(value);
  }

  private async load(kind: CatalogKind, id: string): Promise<void> {
    this.state.set('loading');
    try {
      this.entries.set(await firstValueFrom(this.gateway.priceHistory(kind, id)));
      this.state.set('ready');
    } catch (error) {
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }
}
