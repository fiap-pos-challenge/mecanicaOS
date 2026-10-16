import { cents } from '../../../shared/util/money';
import { toServiceOrderCard, toServiceOrderDetail } from './service-order.mappers';
import { ServiceOrderCard, ServiceOrderStatus } from './service-order.models';
import {
  agenda,
  changedFields,
  checkTransition,
  filterCards,
  isLate,
  mechanicsOf,
  nextStatuses,
  NO_FILTERS,
  tagsOf,
} from './service-order.rules';
import { ContractError } from '../../../core/data/contract-error';

const NOW = Date.parse('2026-10-14T12:00:00Z');

function card(overrides: Partial<ServiceOrderCard> = {}): ServiceOrderCard {
  return {
    id: 'o1',
    number: 3321,
    status: 'Received',
    version: 1,
    customerName: 'Joana Prado',
    plate: 'FJK4E21',
    vehicle: 'VW Polo',
    summary: 'Revisão',
    mechanicName: null,
    promisedAt: null,
    total: cents(0),
    hasQuoteItems: false,
    tags: [],
    updatedAt: '2026-10-14T10:00:00Z',
    ...overrides,
  };
}

describe('service order transitions', () => {
  const allowed: readonly [ServiceOrderStatus, ServiceOrderStatus, string, boolean][] = [
    ['Received', 'InDiagnosis', 'StartDiagnosis', false],
    ['InDiagnosis', 'WaitingApproval', 'SendQuote', false],
    ['WaitingApproval', 'InProgress', 'ApproveQuote', false],
    ['WaitingApproval', 'InDiagnosis', 'RejectQuote', true],
    ['InProgress', 'Completed', 'Complete', false],
    ['Completed', 'Delivered', 'Deliver', false],
  ];

  it.each(allowed)('allows %s to %s (%s)', (from, to, action, needsReason) => {
    expect(checkTransition(card({ status: from, hasQuoteItems: true }), to)).toEqual({
      allowed: true,
      action,
      needsReason,
    });
  });

  it.each([
    ['Received', 'InProgress'],
    ['Received', 'WaitingApproval'],
    ['InDiagnosis', 'Received'],
    ['InProgress', 'WaitingApproval'],
    ['Completed', 'InProgress'],
  ] as const)('blocks %s to %s', (from, to) => {
    expect(checkTransition(card({ status: from }), to)).toEqual({
      allowed: false,
      block: 'NotAllowed',
    });
  });

  it('explains same status, delivered orders and missing quote items', () => {
    expect(checkTransition(card(), 'Received')).toEqual({ allowed: false, block: 'Same' });
    expect(checkTransition(card({ status: 'Delivered' }), 'Completed')).toEqual({
      allowed: false,
      block: 'Delivered',
    });
    expect(checkTransition(card({ status: 'InDiagnosis' }), 'WaitingApproval')).toEqual({
      allowed: false,
      block: 'NeedsQuote',
    });
    expect(nextStatuses('WaitingApproval')).toEqual(['InProgress', 'InDiagnosis']);
    expect(nextStatuses('Delivered')).toEqual([]);
  });
});

describe('tags, filters and agenda', () => {
  it('computes late orders and keeps server tags', () => {
    const late = card({
      status: 'InProgress',
      promisedAt: '2026-10-14T08:00:00Z',
      tags: ['MissingPart', 'Late'],
    });
    expect(isLate(late, NOW)).toBe(true);
    expect(isLate({ ...late, status: 'Completed' }, NOW)).toBe(false);
    expect(isLate({ ...late, promisedAt: null }, NOW)).toBe(false);
    expect(tagsOf(late, NOW)).toEqual(['Late', 'MissingPart']);
    expect(tagsOf({ ...late, promisedAt: '2026-10-15T08:00:00Z' }, NOW)).toEqual(['MissingPart']);
  });

  it('filters by status, mechanic, late and accent-insensitive term', () => {
    const cards = [
      card({
        id: 'a',
        number: 1,
        status: 'InProgress',
        mechanicName: 'Marcos',
        promisedAt: '2026-10-14T08:00:00Z',
      }),
      card({ id: 'b', number: 2, customerName: 'José Álvares', plate: 'ABC1D23' }),
    ];
    expect(filterCards(cards, NO_FILTERS, NOW)).toHaveLength(2);
    expect(
      filterCards(cards, { ...NO_FILTERS, status: 'InProgress' }, NOW).map((c) => c.id),
    ).toEqual(['a']);
    expect(filterCards(cards, { ...NO_FILTERS, mechanic: 'Marcos' }, NOW).map((c) => c.id)).toEqual(
      ['a'],
    );
    expect(filterCards(cards, { ...NO_FILTERS, lateOnly: true }, NOW).map((c) => c.id)).toEqual([
      'a',
    ]);
    expect(filterCards(cards, { ...NO_FILTERS, term: 'alvares' }, NOW).map((c) => c.id)).toEqual([
      'b',
    ]);
    expect(filterCards(cards, { ...NO_FILTERS, term: 'abc-1d23' }, NOW).map((c) => c.id)).toEqual([
      'b',
    ]);
    expect(mechanicsOf([...cards, card({ id: 'c', mechanicName: 'Ana' })])).toEqual([
      'Ana',
      'Marcos',
    ]);
  });

  it('groups promised deliveries by day with overdue first', () => {
    const days = agenda(
      [
        card({ id: 'late', status: 'InProgress', promisedAt: '2026-10-14T08:00:00Z' }),
        card({ id: 'b', promisedAt: '2026-10-16T15:00:00Z' }),
        card({ id: 'a', promisedAt: '2026-10-15T15:00:00Z' }),
        card({ id: 'a2', promisedAt: '2026-10-15T17:00:00Z' }),
        card({ id: 'none' }),
        card({ id: 'gone', status: 'Delivered', promisedAt: '2026-10-15T09:00:00Z' }),
      ],
      NOW,
    );
    expect(days.map((day) => [day.overdue, day.cards.map((item) => item.id)])).toEqual([
      [true, ['late']],
      [false, ['a', 'a2']],
      [false, ['b']],
    ]);
    expect(agenda([], NOW)).toEqual([]);
  });

  it('lists the fields another user changed', () => {
    const before = card();
    expect(changedFields(before, before)).toEqual([]);
    expect(
      changedFields(before, {
        ...before,
        status: 'InDiagnosis',
        mechanicName: 'Marcos',
        total: cents(100),
      }),
    ).toEqual(['status', 'mechanicName', 'total']);
    expect(changedFields(before, { ...before, promisedAt: '2026-10-20T00:00:00Z' })).toEqual([
      'promisedAt',
    ]);
  });
});

describe('service order mappers', () => {
  it('requires identifiers and fills optional fields', () => {
    expect(() => toServiceOrderCard({ id: 'x' })).toThrow(ContractError);
    const detail = toServiceOrderDetail({
      id: 'o',
      number: 1,
      status: 'Received',
      version: 1,
      customerName: 'A',
      licensePlate: 'ABC1234',
      updatedAt: 'now',
      quote: { id: 'q', number: 2, version: 1, status: 'Pending', items: [{ name: 'X' }] },
    });
    expect(detail).toMatchObject({
      total: 0,
      tags: [],
      history: [],
      photos: [],
      quote: { total: 0, items: [{ name: 'X', quantity: 1, total: 0 }] },
    });
  });
});
