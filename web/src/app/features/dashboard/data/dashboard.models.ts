import { Cents } from '../../../shared/util/money';
import { ServiceOrderStatus } from '../../service-orders/data/service-order.models';

export type DashboardPeriod = 'today' | 'week' | 'month';

export const DASHBOARD_PERIODS: readonly DashboardPeriod[] = ['today', 'week', 'month'];

export type DashboardCardId = 'kpis' | 'dailyRevenue' | 'attention' | 'topServices' | 'yard';

export const DASHBOARD_CARDS: readonly DashboardCardId[] = [
  'kpis',
  'dailyRevenue',
  'attention',
  'topServices',
  'yard',
];

export interface DashboardCardSetting {
  readonly id: DashboardCardId;
  readonly visible: boolean;
}

export interface DashboardLayout {
  readonly cards: readonly DashboardCardSetting[];
}

export type KpiId = 'revenue' | 'openOrders' | 'approvalRate' | 'receivable';

export const KPI_IDS: readonly KpiId[] = ['revenue', 'openOrders', 'approvalRate', 'receivable'];

export interface DashboardKpi {
  readonly id: KpiId;
  readonly value: number;
  readonly previous: number | null;
  readonly detail: number | null;
}

export interface DailyRevenue {
  readonly date: string;
  readonly services: Cents;
  readonly parts: Cents;
}

export interface TopService {
  readonly name: string;
  readonly quantity: number;
}

export interface YardCount {
  readonly status: ServiceOrderStatus;
  readonly count: number;
}

export type AttentionKind = 'LowStock' | 'QuoteWithoutAnswer' | 'LateOrder' | 'MaintenanceDue';

export type AttentionSeverity = 'bad' | 'warn' | 'info';

export type AttentionAction = 'CreatePurchaseOrder' | 'SendNewDeadline' | 'SendReminder';

export type AttentionTarget =
  | { readonly kind: 'serviceOrder'; readonly id: string }
  | { readonly kind: 'quote'; readonly id: string }
  | { readonly kind: 'inventory' }
  | { readonly kind: 'customers' };

export interface AttentionItem {
  readonly id: string;
  readonly kind: AttentionKind;
  readonly severity: AttentionSeverity;
  readonly title: string;
  readonly detail: string;
  readonly action: AttentionAction | null;
  readonly target: AttentionTarget | null;
  readonly aiSuggested: boolean;
}

export interface DashboardSnapshot {
  readonly kpis: readonly DashboardKpi[];
  readonly dailyRevenue: readonly DailyRevenue[];
  readonly topServices: readonly TopService[];
  readonly yard: readonly YardCount[];
  readonly attention: readonly AttentionItem[];
}

export function defaultLayout(): DashboardLayout {
  return { cards: DASHBOARD_CARDS.map((id) => ({ id, visible: true })) };
}

export function normalizeLayout(cards: readonly Partial<DashboardCardSetting>[]): DashboardLayout {
  const known = cards.filter(
    (card): card is DashboardCardSetting =>
      card.id !== undefined &&
      DASHBOARD_CARDS.includes(card.id) &&
      typeof card.visible === 'boolean',
  );
  const unique = known.filter((card, index) => known.findIndex((c) => c.id === card.id) === index);
  const missing = DASHBOARD_CARDS.filter((id) => !unique.some((card) => card.id === id)).map(
    (id) => ({ id, visible: true }),
  );
  return { cards: [...unique, ...missing] };
}

export function moveCard(layout: DashboardLayout, id: DashboardCardId, offset: -1 | 1) {
  const cards = [...layout.cards];
  const index = cards.findIndex((card) => card.id === id);
  const target = index + offset;
  const current = cards[index];
  const other = cards[target];
  if (!current || !other) {
    return layout;
  }

  cards[index] = other;
  cards[target] = current;
  return { cards };
}

export function toggleCard(layout: DashboardLayout, id: DashboardCardId): DashboardLayout {
  return {
    cards: layout.cards.map((card) =>
      card.id === id ? { ...card, visible: !card.visible } : card,
    ),
  };
}

export function variation(kpi: DashboardKpi): number | null {
  if (kpi.previous === null || kpi.previous === 0) {
    return null;
  }

  return Math.round(((kpi.value - kpi.previous) / Math.abs(kpi.previous)) * 1000) / 10;
}
