import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChevronDown as ChevronDownIcon, ChevronUp as ChevronUpIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiChart } from '../../../shared/ui/chart/chart';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { ChartData } from '../../../shared/ui/chart/chart.models';
import { KpiTrend, UiKpi } from '../../../shared/ui/kpi/kpi';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { ToastService } from '../../../shared/ui/toast/toast';
import { cents, formatBrl } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { DashboardGateway } from '../data/dashboard.gateway';
import {
  AttentionItem,
  AttentionTarget,
  DASHBOARD_PERIODS,
  DashboardCardId,
  DashboardKpi,
  DashboardLayout,
  DashboardPeriod,
  DashboardSnapshot,
  defaultLayout,
  moveCard,
  toggleCard,
  variation,
} from '../data/dashboard.models';

const text = messages.dashboard;

export function greetingFor(hour: number): string {
  if (hour < 12) {
    return text.greeting.morning;
  }

  return hour < 18 ? text.greeting.afternoon : text.greeting.evening;
}

export function kpiView(kpi: DashboardKpi): {
  readonly label: string;
  readonly value: string;
  readonly delta: string | null;
  readonly trend: KpiTrend;
} {
  const label = text.kpis[kpi.id];
  if (kpi.id === 'approvalRate') {
    const points = kpi.previous === null ? null : kpi.value - kpi.previous;
    return {
      label,
      value: text.rate(kpi.value),
      delta: points === null ? null : text.versus(text.points(points)),
      trend: points === null || points === 0 ? 'neutral' : points > 0 ? 'up' : 'down',
    };
  }

  if (kpi.id === 'openOrders') {
    return {
      label,
      value: kpi.value.toLocaleString('pt-BR'),
      delta: kpi.detail === null ? null : text.kpiDetail.openOrders(kpi.detail),
      trend: 'neutral',
    };
  }

  const change = variation(kpi);
  const detail =
    kpi.id === 'receivable' && kpi.detail !== null ? text.kpiDetail.receivable(kpi.detail) : null;
  return {
    label,
    value: formatBrl(cents(kpi.value)),
    delta: change === null ? detail : text.versus(text.percent(change)),
    trend:
      kpi.id === 'receivable' && (kpi.detail ?? 0) > 0
        ? 'down'
        : change === null || change === 0
          ? 'neutral'
          : change > 0
            ? 'up'
            : 'down',
  };
}

export function targetLink(target: AttentionTarget): readonly string[] {
  switch (target.kind) {
    case 'serviceOrder':
      return ['/service-orders', target.id];
    case 'quote':
      return ['/quotes', target.id];
    case 'inventory':
      return ['/inventory'];
    case 'customers':
      return ['/customers'];
  }
}

function shortDay(date: string): string {
  const [, month, day] = date.split('-');
  return `${day}/${month}`;
}

@Component({
  selector: 'app-dashboard-page',
  imports: [
    RouterLink,
    UiAiBadge,
    UiButton,
    UiCard,
    UiChart,
    UiIcon,
    UiKpi,
    UiPendingNotice,
    UiSegmented,
    UiSkeleton,
  ],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  private readonly gateway = inject(DashboardGateway);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthStore);

  protected readonly text = text;
  protected readonly screen = messages.screens.dashboard;
  protected readonly icons = { up: ChevronUpIcon, down: ChevronDownIcon };
  protected readonly pendingText = messages.pending.dashboard;
  protected readonly statuses = messages.serviceOrders.statuses;
  protected readonly periods: readonly SegmentedOption<DashboardPeriod>[] = DASHBOARD_PERIODS.map(
    (value) => ({ value, label: text.periods[value] }),
  );
  protected readonly period = signal<DashboardPeriod>('today');
  protected readonly snapshot = signal<DashboardSnapshot | null>(null);
  protected readonly layout = signal<DashboardLayout>(defaultLayout());
  protected readonly draft = signal<DashboardLayout | null>(null);
  protected readonly loading = signal(true);
  protected readonly pending = signal(false);
  protected readonly failed = signal(false);
  protected readonly saving = signal(false);
  protected readonly acting = signal<string | null>(null);
  protected readonly money = (value: number): string => formatBrl(cents(value));
  protected readonly title = computed(() => {
    const name = this.auth.user()?.name.split(' ')[0] ?? '';
    return text.hello(greetingFor(new Date().getHours()), name);
  });
  protected readonly visibleCards = computed(() =>
    this.layout()
      .cards.filter((card) => card.visible)
      .map((card) => card.id),
  );
  protected readonly yardTotal = computed(
    () => this.snapshot()?.yard.reduce((sum, slice) => sum + slice.count, 0) ?? 0,
  );
  protected readonly kpis = computed(() => (this.snapshot()?.kpis ?? []).map(kpiView));
  protected readonly revenueChart = computed<ChartData>(() => {
    const days = this.snapshot()?.dailyRevenue ?? [];
    return {
      categories: days.map((day) => shortDay(day.date)),
      series: [
        { name: text.series.services, tone: 'accent', values: days.map((day) => day.services) },
        { name: text.series.parts, tone: 'amber', values: days.map((day) => day.parts) },
      ],
    };
  });
  protected readonly servicesChart = computed<ChartData>(() => {
    const services = this.snapshot()?.topServices ?? [];
    return {
      categories: services.map((service) => service.name),
      series: [
        {
          name: text.series.quantity,
          tone: 'accent',
          values: services.map((service) => service.quantity),
        },
      ],
    };
  });
  protected readonly yardChart = computed<ChartData>(() => {
    const yard = this.snapshot()?.yard ?? [];
    return {
      categories: yard.map((slice) => this.statuses[slice.status]),
      series: [{ name: text.series.orders, tone: 'accent', values: yard.map((s) => s.count) }],
    };
  });

  constructor() {
    void this.load();
    void this.loadLayout();
  }

  protected setPeriod(period: DashboardPeriod): void {
    this.period.set(period);
    void this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);
    try {
      this.snapshot.set(await firstValueFrom(this.gateway.snapshot(this.period())));
      this.pending.set(false);
    } catch (error) {
      this.snapshot.set(null);
      this.pending.set(isFeaturePending(error));
      this.failed.set(!isFeaturePending(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected customize(): void {
    this.draft.set(this.layout());
  }

  protected cancel(): void {
    this.draft.set(null);
  }

  protected toggle(id: DashboardCardId): void {
    this.draft.update((draft) => (draft ? toggleCard(draft, id) : draft));
  }

  protected move(id: DashboardCardId, offset: -1 | 1): void {
    this.draft.update((draft) => (draft ? moveCard(draft, id, offset) : draft));
  }

  protected async save(): Promise<void> {
    const draft = this.draft();
    if (!draft) {
      return;
    }

    this.saving.set(true);
    try {
      this.layout.set(await firstValueFrom(this.gateway.saveLayout(draft)));
      this.draft.set(null);
      this.toast.show(text.saved, { tone: 'success' });
    } catch (error) {
      this.toast.show(isApiError(error) ? error.detail : text.saveError, { tone: 'error' });
    } finally {
      this.saving.set(false);
    }
  }

  protected async act(item: AttentionItem): Promise<void> {
    const action = item.action;
    if (!action) {
      return;
    }

    this.acting.set(item.id);
    try {
      await firstValueFrom(this.gateway.act(item.id, action));
      this.snapshot.update((snapshot) =>
        snapshot
          ? { ...snapshot, attention: snapshot.attention.filter((a) => a.id !== item.id) }
          : snapshot,
      );
      this.toast.show(text.actionDone[action], { tone: 'success' });
    } catch (error) {
      this.toast.show(isApiError(error) ? error.detail : messages.errors.unknown, {
        tone: 'error',
      });
    } finally {
      this.acting.set(null);
    }
  }

  protected link(target: AttentionTarget): readonly string[] {
    return targetLink(target);
  }

  private async loadLayout(): Promise<void> {
    try {
      this.layout.set(await firstValueFrom(this.gateway.layout()));
    } catch {
      this.layout.set(defaultLayout());
    }
  }
}
