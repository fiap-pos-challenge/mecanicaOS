import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { provideTestApi, TestApiMode } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { moveOrder } from '../../../../testing/fake-api/service-orders/service-orders.fake-api';
import {
  RealtimeEvent,
  RealtimeGateway,
  RealtimeStatus,
} from '../../../core/realtime/realtime.gateway';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideServiceOrderData } from '../data/service-order.gateway';
import { BoardStore, errorText, SERVICE_ORDER_POLL_MS } from './board.store';

class FakeRealtime extends RealtimeGateway {
  readonly events = new Subject<RealtimeEvent>();
  readonly status = signal<RealtimeStatus>('disconnected');
  readonly events$ = this.events.asObservable();
  connect = vi.fn(async () => undefined);
  disconnect = vi.fn(async () => undefined);
}

function setup(mode: TestApiMode = 'mock', pollMs = 60_000) {
  const realtime = new FakeRealtime();
  TestBed.configureTestingModule({
    providers: [
      ...provideTestApi(mode, [{ path: '**', children: [] }]),
      provideAuthData(),
      provideServiceOrderData(),
      { provide: RealtimeGateway, useValue: realtime },
      { provide: SERVICE_ORDER_POLL_MS, useValue: pollMs },
      BoardStore,
    ],
  });
  signInWithFakeApi('Attendant');
  return { store: TestBed.inject(BoardStore), realtime };
}

function orders() {
  return TestBed.inject(FakeBackend).state.database.serviceOrders.orders;
}

function order(number: number) {
  const found = orders().find((item) => item.number === number);
  if (!found) {
    throw new Error(`Order ${number} not found.`);
  }

  return found;
}

async function ready(store: BoardStore): Promise<void> {
  await vi.waitFor(() => expect(store.loadState()).toBe('ready'));
}

describe('BoardStore', () => {
  it('loads the board, connects realtime and filters', async () => {
    const { store, realtime } = setup();
    await ready(store);

    expect(realtime.connect).toHaveBeenCalled();
    expect(store.cards()).toHaveLength(10);
    expect(store.mechanics()).toContain('Marcos Mecânico');
    store.setFilters({ status: 'InProgress' });
    expect(store.visible().map((card) => card.number)).toEqual([3319, 3318, 3315]);
    store.setFilters({ lateOnly: true, status: null });
    expect(store.visible().map((card) => card.number)).toEqual([3318]);
  });

  it('blocks invalid moves, asks for a reason and moves valid ones', async () => {
    const { store } = setup();
    await ready(store);
    const received = order(3326).id;

    expect(await store.move(received, 'Completed')).toEqual({
      kind: 'blocked',
      block: 'NotAllowed',
    });
    expect(await store.move(order(3327).id, 'WaitingApproval')).toEqual({
      kind: 'blocked',
      block: 'NeedsQuote',
    });
    expect(await store.move(order(3324).id, 'InDiagnosis')).toEqual({ kind: 'needsReason' });
    expect(await store.move('missing', 'InDiagnosis')).toEqual({ kind: 'failed' });
    expect(await store.move(received, 'InDiagnosis')).toEqual({ kind: 'moved' });
    expect(store.cards().find((card) => card.id === received)?.status).toBe('InDiagnosis');
    expect(await store.move(order(3324).id, 'InDiagnosis', 'Achou caro')).toEqual({
      kind: 'moved',
    });
    expect(order(3324).history.at(-1)?.note).toBe('Achou caro');
  });

  it('reloads and explains a version conflict', async () => {
    const { store } = setup();
    await ready(store);
    moveOrder(order(3326), 'InDiagnosis', 'Gabi Gestora', null);

    expect(await store.move(order(3326).id, 'InDiagnosis')).toEqual({ kind: 'conflict' });
    expect(store.changes()[0]).toMatchObject({
      kind: 'conflict',
      number: 3326,
      fields: ['status'],
    });
    store.dismissChange(3326);
    expect(store.changes()).toEqual([]);
  });

  it('shows errors from the server', async () => {
    const { store } = setup();
    await ready(store);
    TestBed.inject(FakeBackend).setScenario('serviceOrders', 'unprocessable');

    expect(await store.move(order(3326).id, 'InDiagnosis')).toEqual({ kind: 'failed' });
    expect(store.error()).not.toBeNull();
    store.dismissError();
    expect(store.error()).toBeNull();
    expect(errorText(new Error('x'))).toBe(errorText(null));
  });

  it('applies realtime events and polls while realtime is offline', async () => {
    const { store, realtime } = setup('mock', 20);
    await ready(store);

    moveOrder(order(3326), 'InDiagnosis', 'Gabi Gestora', null);
    realtime.events.next({
      type: 'serviceOrderStatusChanged',
      serviceOrderId: order(3326).id,
      number: 3326,
      status: 'InDiagnosis',
      occurredAt: new Date().toISOString(),
    });
    await vi.waitFor(() =>
      expect(store.changes()[0]).toMatchObject({ kind: 'remote', number: 3326 }),
    );

    moveOrder(order(3319), 'Completed', 'Gabi Gestora', null);
    await vi.waitFor(() =>
      expect(store.changes().map((change) => change.number)).toEqual([3319, 3326]),
    );
    expect(store.cards().find((card) => card.number === 3319)?.status).toBe('Completed');
  });

  it('adds cards announced by realtime and keeps quiet when nothing changed', async () => {
    const { store, realtime } = setup();
    await ready(store);
    const id = order(3301).id;
    store.setFilters({ term: 'x' });

    realtime.events.next({ type: 'exportReady', exportId: 'e', occurredAt: '' });
    await store.reloadCard(id, 'remote');
    expect(store.changes()).toEqual([]);
    await store.reloadCard('missing', 'remote');
    expect(store.error()).not.toBeNull();
  });

  it('reports the construction notice and load errors', async () => {
    const pending = setup('pending').store;
    await vi.waitFor(() => expect(pending.loadState()).toBe('pending'));
    await pending.refresh();

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const { store } = setup();
    TestBed.inject(FakeBackend).setScenario('serviceOrders', 'serverError');
    await store.load();
    expect(store.loadState()).toBe('error');
  });
});
