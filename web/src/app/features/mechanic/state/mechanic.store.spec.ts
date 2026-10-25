import { Injector } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { IDBFactory as FakeIndexedDb } from 'fake-indexeddb';
import { throwError } from 'rxjs';
import { provideTestApi, TestApiMode } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { SessionStateRegistry } from '../../../core/auth/session-state.registry';
import { ApiError, NETWORK_ERROR_CODE } from '../../../core/http/api-error';
import { OFFLINE_INDEXED_DB } from '../../../core/pwa/offline-queue.storage';
import { provideAuthData } from '../../auth/data/auth.providers';
import { MechanicGateway, provideMechanicData } from '../data/mechanic.gateway';
import { MechanicOrder } from '../data/mechanic.models';
import { applyAction, isOfflineError, MechanicStore } from './mechanic.store';

const PNG = 'data:image/png;base64,iVBORw0KGgo=';

const offlineError: ApiError = {
  kind: 'api-error',
  status: 0,
  code: NETWORK_ERROR_CODE,
  title: '',
  detail: '',
  traceId: null,
  fieldErrors: [],
  retryAfterSeconds: null,
};

function setup(mode: TestApiMode = 'mock', indexedDb: IDBFactory | null = null) {
  TestBed.configureTestingModule({
    providers: [
      { provide: OFFLINE_INDEXED_DB, useValue: indexedDb },
      ...provideTestApi(mode, [{ path: '**', children: [] }]),
      provideAuthData(),
      provideMechanicData(),
      MechanicStore,
    ],
  });
  signInWithFakeApi('Mechanic');
  return TestBed.inject(MechanicStore);
}

function server(number: number) {
  const order = TestBed.inject(FakeBackend).state.database.serviceOrders.orders.find(
    (item) => item.number === number,
  );
  if (!order) {
    throw new Error('Missing order');
  }

  return order;
}

function go(online: boolean): void {
  Object.defineProperty(navigator, 'onLine', { value: online, configurable: true });
  window.dispatchEvent(new Event(online ? 'online' : 'offline'));
}

afterEach(() => go(true));

describe('mechanic helpers', () => {
  const order: MechanicOrder = {
    id: 'o',
    number: 1,
    status: 'Received',
    vehicle: 'Argo',
    plate: 'A',
    complaint: '',
    promisedAt: null,
    checklist: [],
    photos: [],
    diagnosis: '',
    sentToQuote: false,
    timerRunning: false,
  };

  it('applies actions locally', () => {
    expect(applyAction(order, { kind: 'start' }, 'now').status).toBe('InDiagnosis');
    expect(
      applyAction({ ...order, status: 'InProgress' }, { kind: 'start' }, 'now').timerRunning,
    ).toBe(true);
    expect(applyAction(order, { kind: 'finish' }, 'now')).toMatchObject({
      status: 'Completed',
      timerRunning: false,
    });
    expect(
      applyAction(order, { kind: 'sendToQuote', request: { description: 'x', items: [] } }, 'now'),
    ).toMatchObject({
      sentToQuote: true,
      diagnosis: 'x',
    });
    expect(isOfflineError(offlineError)).toBe(true);
    expect(isOfflineError({ ...offlineError, status: 500, code: 'x' })).toBe(false);
  });
});

describe('MechanicStore', () => {
  it('loads my orders and performs actions online', async () => {
    const store = setup();
    await store.loadMine();

    expect(store.listState()).toBe('ready');
    expect(
      store
        .mine()
        .map((order) => order.number)
        .sort(),
    ).toEqual([3315, 3318, 3327]);
    const id = server(3327).id;
    const order = await store.loadOrder(id);
    const first = order.checklist[0];
    if (!first) {
      throw new Error('Missing checklist');
    }

    expect(await store.perform(id, { kind: 'checklist', items: [{ ...first, state: 'Ok' }] })).toBe(
      'done',
    );
    expect(server(3327).checklist[0]?.state).toBe('Ok');
    expect(await store.perform('missing', { kind: 'start' })).toBe('failed');
  });

  it('queues actions offline, coalesces them and syncs when the network returns', async () => {
    const store = setup();
    await store.loadMine();
    const id = server(3327).id;
    const order = await store.loadOrder(id);
    const [first, second] = order.checklist;
    if (!first || !second) {
      throw new Error('Missing checklist');
    }

    go(false);
    expect(store.online()).toBe(false);
    expect(
      await store.perform(id, {
        kind: 'checklist',
        items: [{ ...first, state: 'Attention' }, second],
      }),
    ).toBe('queued');
    expect(
      await store.perform(id, {
        kind: 'checklist',
        items: [
          { ...first, state: 'Attention' },
          { ...second, state: 'Replace' },
        ],
      }),
    ).toBe('queued');
    await store.perform(id, {
      kind: 'photo',
      photo: { id: 'p1', fileName: 'roda.png', contentType: 'image/png', dataUrl: PNG },
    });
    await store.perform(id, { kind: 'diagnosis', description: 'Bieleta com folga' });
    expect(store.queue().map((item) => item.action.kind)).toEqual([
      'checklist',
      'photo',
      'diagnosis',
    ]);
    expect(store.pendingByOrder()[id]).toBe(3);
    const photoAction = store.queue()[1];
    expect(photoAction && store.label(photoAction)).toContain('foto');
    expect(store.orders()[id]?.photos[0]?.pending).toBe(true);
    expect(await store.loadOrder(id)).toBe(store.orders()[id]);
    expect(server(3327).diagnosis).toBe('');

    go(true);
    await vi.waitFor(() => expect(store.queue()).toEqual([]));
    expect(
      server(3327)
        .checklist.slice(0, 2)
        .map((item) => item.state),
    ).toEqual(['Attention', 'Replace']);
    expect(server(3327).photos).toHaveLength(1);
    expect(server(3327).diagnosis).toBe('Bieleta com folga');
    expect(store.orders()[id]?.photos[0]?.pending).toBe(false);
  });

  it('queues when the request fails for lack of network and records server rejections', async () => {
    const store = setup();
    await store.loadMine();
    const id = server(3327).id;
    await store.loadOrder(id);
    const gateway = TestBed.inject(MechanicGateway);
    const spy = vi.spyOn(gateway, 'perform').mockReturnValueOnce(throwError(() => offlineError));

    expect(await store.perform(id, { kind: 'diagnosis', description: 'Sem rede' })).toBe('queued');
    expect(store.online()).toBe(false);
    spy.mockRestore();

    await store.perform(id, { kind: 'finish' });
    go(true);
    await vi.waitFor(() => expect(store.queue()).toEqual([]));
    expect(store.failures()).toHaveLength(1);
    expect(store.failures()[0]?.text).toContain('fim do serviço');
    store.dismissFailure(store.failures()[0]?.id ?? '');
    expect(store.failures()).toEqual([]);
    expect(
      await store.perform(server(3318).id, {
        kind: 'sendToQuote',
        request: { description: 'x', items: [] },
      }),
    ).toBe('failed');
  });

  it('keeps the cached list when the network drops and clears everything on logout', async () => {
    const store = setup();
    await store.loadMine();
    const gateway = TestBed.inject(MechanicGateway);
    vi.spyOn(gateway, 'mine').mockReturnValueOnce(throwError(() => offlineError));
    vi.spyOn(gateway, 'work').mockReturnValueOnce(throwError(() => offlineError));

    await store.loadMine();
    expect(store.listState()).toBe('ready');
    go(true);
    const id = server(3318).id;
    expect((await store.loadOrder(id)).number).toBe(3318);
    expect(store.online()).toBe(false);

    TestBed.inject(SessionStateRegistry).resetAll();
    expect(store.mine()).toEqual([]);
    expect(store.queue()).toEqual([]);
    await expect(store.loadOrder('missing')).rejects.toBeTruthy();
  });

  it('keeps the offline queue across reloads for the same user and syncs it later', async () => {
    const store = setup('mock', new FakeIndexedDb());
    await store.loadMine();
    const id = server(3327).id;
    await store.loadOrder(id);

    go(false);
    expect(await store.perform(id, { kind: 'diagnosis', description: 'Ruído na roda' })).toBe(
      'queued',
    );
    await vi.waitFor(async () => {
      const reloaded = Injector.create({
        providers: [MechanicStore],
        parent: TestBed.inject(Injector),
      }).get(MechanicStore);
      await reloaded.restored;
      expect(reloaded.queue()).toHaveLength(1);
    });

    const reloaded = Injector.create({
      providers: [MechanicStore],
      parent: TestBed.inject(Injector),
    }).get(MechanicStore);
    await reloaded.loadMine();
    expect(reloaded.orders()[id]?.diagnosis).toBe('Ruído na roda');
    go(true);
    await vi.waitFor(() => expect(reloaded.queue()).toEqual([]));
    expect(server(3327).diagnosis).toBe('Ruído na roda');
  });

  it('reports pending and failing lists', async () => {
    const pending = setup('pending');
    await pending.loadMine();
    expect(pending.listState()).toBe('pending');

    TestBed.resetTestingModule();
    const store = setup();
    TestBed.inject(FakeBackend).setScenario('serviceOrders', 'serverError');
    await store.loadMine();
    expect(store.listState()).toBe('error');
  });
});
