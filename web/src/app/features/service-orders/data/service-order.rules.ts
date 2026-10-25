import { ServiceOrderCard, ServiceOrderStatus, ServiceOrderTag } from './service-order.models';

export type TransitionAction =
  'StartDiagnosis' | 'SendQuote' | 'ApproveQuote' | 'RejectQuote' | 'Complete' | 'Deliver';

export type TransitionBlock = 'Same' | 'Delivered' | 'NeedsQuote' | 'NotAllowed';

export type TransitionCheck =
  | { readonly allowed: true; readonly action: TransitionAction; readonly needsReason: boolean }
  | { readonly allowed: false; readonly block: TransitionBlock };

const TRANSITIONS: readonly {
  readonly from: ServiceOrderStatus;
  readonly to: ServiceOrderStatus;
  readonly action: TransitionAction;
}[] = [
  { from: 'Received', to: 'InDiagnosis', action: 'StartDiagnosis' },
  { from: 'InDiagnosis', to: 'WaitingApproval', action: 'SendQuote' },
  { from: 'WaitingApproval', to: 'InProgress', action: 'ApproveQuote' },
  { from: 'WaitingApproval', to: 'InDiagnosis', action: 'RejectQuote' },
  { from: 'InProgress', to: 'Completed', action: 'Complete' },
  { from: 'Completed', to: 'Delivered', action: 'Deliver' },
];

export function checkTransition(
  card: Pick<ServiceOrderCard, 'status' | 'hasQuoteItems'>,
  to: ServiceOrderStatus,
): TransitionCheck {
  if (card.status === to) {
    return { allowed: false, block: 'Same' };
  }

  if (card.status === 'Delivered') {
    return { allowed: false, block: 'Delivered' };
  }

  const transition = TRANSITIONS.find((item) => item.from === card.status && item.to === to);
  if (!transition) {
    return { allowed: false, block: 'NotAllowed' };
  }

  if (transition.action === 'SendQuote' && !card.hasQuoteItems) {
    return { allowed: false, block: 'NeedsQuote' };
  }

  return {
    allowed: true,
    action: transition.action,
    needsReason: transition.action === 'RejectQuote',
  };
}

export function nextStatuses(status: ServiceOrderStatus): readonly ServiceOrderStatus[] {
  return TRANSITIONS.filter((item) => item.from === status).map((item) => item.to);
}

export function isLate(
  card: Pick<ServiceOrderCard, 'status' | 'promisedAt'>,
  now: number,
): boolean {
  return (
    card.promisedAt !== null &&
    card.status !== 'Completed' &&
    card.status !== 'Delivered' &&
    Date.parse(card.promisedAt) < now
  );
}

export function tagsOf(card: ServiceOrderCard, now: number): readonly ServiceOrderTag[] {
  const tags = card.tags.filter((tag) => tag !== 'Late');
  return isLate(card, now) ? ['Late', ...tags] : tags;
}

export interface CardFilters {
  readonly status: ServiceOrderStatus | null;
  readonly mechanic: string | null;
  readonly lateOnly: boolean;
  readonly term: string;
}

export const NO_FILTERS: CardFilters = { status: null, mechanic: null, lateOnly: false, term: '' };

function normalize(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]/gi, '')
    .toLowerCase();
}

export function filterCards(
  cards: readonly ServiceOrderCard[],
  filters: CardFilters,
  now: number,
): readonly ServiceOrderCard[] {
  const term = normalize(filters.term);
  return cards.filter(
    (card) =>
      (filters.status === null || card.status === filters.status) &&
      (filters.mechanic === null || card.mechanicName === filters.mechanic) &&
      (!filters.lateOnly || isLate(card, now)) &&
      (term === '' ||
        [String(card.number), card.customerName, card.plate, card.vehicle].some((value) =>
          normalize(value).includes(term),
        )),
  );
}

export interface AgendaDay {
  readonly key: string;
  readonly overdue: boolean;
  readonly cards: readonly ServiceOrderCard[];
}

function dayKey(iso: string): string {
  const date = new Date(iso);
  const pad = (value: number): string => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function agenda(cards: readonly ServiceOrderCard[], now: number): readonly AgendaDay[] {
  const scheduled = cards
    .filter((card) => card.promisedAt !== null && card.status !== 'Delivered')
    .sort((a, b) => Date.parse(a.promisedAt ?? '') - Date.parse(b.promisedAt ?? ''));
  const overdue = scheduled.filter((card) => isLate(card, now));
  const days = new Map<string, ServiceOrderCard[]>();
  for (const card of scheduled.filter((item) => !isLate(item, now))) {
    const key = dayKey(card.promisedAt ?? '');
    days.set(key, [...(days.get(key) ?? []), card]);
  }

  return [
    ...(overdue.length > 0 ? [{ key: 'overdue', overdue: true, cards: overdue }] : []),
    ...[...days.entries()].map(([key, items]) => ({ key, overdue: false, cards: items })),
  ];
}

export type ChangedField = 'status' | 'mechanicName' | 'promisedAt' | 'total';

export function changedFields(
  before: ServiceOrderCard,
  after: ServiceOrderCard,
): readonly ChangedField[] {
  return (['status', 'mechanicName', 'promisedAt', 'total'] as const).filter(
    (field) => before[field] !== after[field],
  );
}

export function mechanicsOf(cards: readonly ServiceOrderCard[]): readonly string[] {
  return [
    ...new Set(cards.flatMap((card) => (card.mechanicName === null ? [] : [card.mechanicName]))),
  ].sort((a, b) => a.localeCompare(b, 'pt-BR'));
}
