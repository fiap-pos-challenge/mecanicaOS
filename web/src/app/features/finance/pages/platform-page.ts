import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiKpi } from '../../../shared/ui/kpi/kpi';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { formatDate } from '../../../shared/util/date';
import { PlatformGateway, providePlatformData } from '../data/finance.gateway';
import { PlatformOverview } from '../data/finance.models';

type LoadState = 'loading' | 'ready' | 'pending' | 'error';

const text = messages.platform;
const NEAR_LIMIT = 0.8;
const USAGE_KEYS = [
  'customers',
  'vehicles',
  'serviceOrdersThisMonth',
  'documentsThisMonth',
  'activeAutomations',
] as const;

@Component({
  selector: 'app-platform-page',
  imports: [RouterLink, UiButton, UiCard, UiKpi, UiPendingNotice, UiSkeleton, UiStatusPill],
  providers: [providePlatformData()],
  template: `
    <header class="head">
      <div>
        <h1 class="page-title">{{ screen }}</h1>
        <p class="sub">{{ text.subtitle }}</p>
      </div>
      <a uiButton routerLink="/settings">{{ text.settings }}</a>
    </header>
    @switch (state()) {
      @case ('loading') {
        <ui-skeleton height="320px" />
      }
      @case ('pending') {
        <ui-pending-notice [message]="pendingText" />
      }
      @case ('error') {
        <div class="alert" role="alert">
          <span>{{ text.loadError }}</span>
          <button uiButton size="sm" type="button" (click)="load()">{{ text.retry }}</button>
        </div>
      }
      @case ('ready') {
        @if (overview(); as data) {
          <div class="grid">
            <ui-card [headingLevel]="2" [title]="text.plan">
              <p class="plan">{{ text.planName(data.plan.name) }}</p>
              <p class="sub">{{ text.renewsAt(renewal()) }}</p>
              <label class="meter">
                <span
                  >{{ text.usersLabel }}:
                  {{ text.users(data.plan.users, data.plan.maxUsers) }}</span
                >
                <progress [value]="data.plan.users" [max]="data.plan.maxUsers || 1"></progress>
              </label>
              @if (nearLimit()) {
                <p class="warn" role="status">{{ text.nearLimit }}</p>
              }
            </ui-card>
            <ui-card [headingLevel]="2" [title]="text.modules">
              <ul class="modules">
                @for (module of data.modules; track module.id) {
                  <li>
                    <span>{{ module.name }}</span>
                    <ui-status-pill [tone]="module.enabled ? 'ok' : 'neutral'">{{
                      module.enabled ? text.enabled : text.disabled
                    }}</ui-status-pill>
                  </li>
                }
              </ul>
            </ui-card>
          </div>
          <section class="kpis" [attr.aria-label]="text.usage">
            @for (item of usage(); track item.label) {
              <ui-kpi [label]="item.label" [value]="item.value" />
            }
          </section>
        }
      }
    }
  `,
  styleUrl: './finance-page.scss',
  styles: `
    .grid {
      margin-bottom: var(--space-4);
    }
    .plan {
      margin: 0;
      font-size: var(--text-lg);
      font-weight: 600;
    }
    .meter {
      display: grid;
      gap: var(--space-1);
      margin-top: var(--space-3);
      font-size: var(--text-sm);
      progress {
        width: 100%;
        accent-color: var(--accent);
      }
    }
    .warn {
      margin: var(--space-2) 0 0;
      color: var(--warn);
      font-size: var(--text-sm);
    }
    .modules {
      display: grid;
      gap: var(--space-2);
      margin: 0;
      padding: 0;
      list-style: none;
      li {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-2);
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlatformPage {
  private readonly gateway = inject(PlatformGateway);

  protected readonly text = text;
  protected readonly screen = messages.screens.platform;
  protected readonly pendingText = messages.errors.featurePending;
  protected readonly state = signal<LoadState>('loading');
  protected readonly overview = signal<PlatformOverview | null>(null);
  protected readonly renewal = computed(() => {
    const data = this.overview();
    return data ? formatDate(data.plan.renewsAt) : '';
  });
  protected readonly nearLimit = computed(() => {
    const plan = this.overview()?.plan;
    return !!plan && plan.maxUsers > 0 && plan.users / plan.maxUsers >= NEAR_LIMIT;
  });
  protected readonly usage = computed(() => {
    const usage = this.overview()?.usage;
    return usage
      ? USAGE_KEYS.map((key) => ({
          label: text.usageItems[key],
          value: usage[key].toLocaleString('pt-BR'),
        }))
      : [];
  });

  constructor() {
    void this.load();
  }

  protected async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.overview.set(await firstValueFrom(this.gateway.overview()));
      this.state.set('ready');
    } catch (error) {
      this.overview.set(null);
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }
}
