import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { moveOrder } from '../../../../testing/fake-api/service-orders/service-orders.fake-api';
import { ApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { MechanicGateway, toMechanicOrder } from '../../mechanic/data/mechanic.gateway';
import { HttpServiceOrderGateway, ServiceOrderGateway } from './service-order.gateway';

const MECHANIC = 'marcos@oficina.local';
const ATTENDANT = 'ana@oficina.local';
const PNG = 'data:image/png;base64,iVBORw0KGgo=';

async function failure(source: Observable<unknown>): Promise<ApiError> {
  try {
    await firstValueFrom(source);
  } catch (error) {
    return error as ApiError;
  }

  throw new Error('Expected the request to fail.');
}

describe.each(API_MODES)('ServiceOrderGateway and MechanicGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string) {
    harness.configure(mode);
    await harness.login(email);
    return {
      orders: TestBed.inject(ServiceOrderGateway),
      mechanic: TestBed.inject(MechanicGateway),
    };
  }

  function orderId(number: number): string {
    return (
      harness.state(mode).database.serviceOrders.orders.find((order) => order.number === number)
        ?.id ?? ''
    );
  }

  it('loads the board and an order with history, quote, parts, time and documents', async () => {
    const { orders } = await setup(ATTENDANT);

    expect(orders).toBeInstanceOf(HttpServiceOrderGateway);
    const board = await firstValueFrom(orders.board());
    expect(board).toHaveLength(10);
    expect(new Set(board.map((card) => card.status)).size).toBe(6);
    expect(board.find((card) => card.number === 3324)).toMatchObject({
      status: 'WaitingApproval',
      tags: ['ReminderSent'],
      hasQuoteItems: true,
      customerName: 'Joana Prado',
    });
    const detail = await firstValueFrom(orders.get(orderId(3315)));
    expect(detail.history.map((entry) => entry.to)).toEqual([
      'Received',
      'InDiagnosis',
      'WaitingApproval',
      'InProgress',
    ]);
    expect(detail.parts).toEqual([
      expect.objectContaining({ reserved: false, leadTimeDays: 3 }),
      expect.objectContaining({ reserved: true }),
    ]);
    expect(detail.quote?.items.length).toBe(2);
    expect(
      (await firstValueFrom(orders.get(orderId(3310)))).documents.map((item) => item.kind),
    ).toEqual(['ServiceOrder', 'Receipt']);
    expect((await failure(orders.get('missing'))).status).toBe(404);
  });

  it('applies the domain transitions with version checks', async () => {
    const { orders } = await setup(ATTENDANT);
    const id = orderId(3326);

    const moved = await firstValueFrom(
      orders.transition(id, { to: 'InDiagnosis', version: 1, reason: null }),
    );
    expect(moved).toMatchObject({ status: 'InDiagnosis', version: 2 });
    expect(moved.history.at(-1)).toMatchObject({
      from: 'Received',
      to: 'InDiagnosis',
      changedBy: 'Ana Atendente',
    });
    const stale = await failure(
      orders.transition(id, { to: 'WaitingApproval', version: 1, reason: null }),
    );
    expect(stale).toMatchObject({ status: 409, code: 'serviceOrders.versionConflict' });
    const noQuote = await failure(
      orders.transition(id, { to: 'WaitingApproval', version: 2, reason: null }),
    );
    expect(noQuote.code).toBe('serviceOrders.quoteWithoutItems');
    const skip = await failure(
      orders.transition(id, { to: 'Completed', version: 2, reason: null }),
    );
    expect(skip).toMatchObject({
      status: 422,
      detail: messages.fake.serviceOrderInvalidTransition,
    });

    const waiting = orderId(3324);
    expect(
      (await failure(orders.transition(waiting, { to: 'InDiagnosis', version: 3, reason: 'x' })))
        .status,
    ).toBe(400);
    const rejected = await firstValueFrom(
      orders.transition(waiting, { to: 'InDiagnosis', version: 3, reason: 'Cliente achou caro' }),
    );
    expect(rejected.history.at(-1)?.note).toBe('Cliente achou caro');
    expect(rejected.quote?.status).toBe('Rejected');
  });

  it('starts and stops the time entry', async () => {
    const { orders } = await setup(MECHANIC);
    const id = orderId(3319);

    const started = await firstValueFrom(orders.startTime(id));
    expect(started.timeEntries.at(-1)?.endedAt).toBeNull();
    const stopped = await firstValueFrom(orders.stopTime(id));
    expect(stopped.timeEntries.at(-1)?.endedAt).not.toBeNull();
    expect((await failure(orders.startTime(orderId(3326)))).status).toBe(422);
  });

  it('serves the mechanic work flow', async () => {
    const { mechanic } = await setup(MECHANIC);

    const mine = await firstValueFrom(mechanic.mine());
    expect(mine.map((order) => order.number).sort()).toEqual([3315, 3318, 3327]);
    const id = orderId(3327);
    const work = await firstValueFrom(mechanic.work(id));
    expect(work.checklist).toHaveLength(6);
    const first = work.checklist[0];
    if (!first) {
      throw new Error('Missing checklist');
    }

    const checked = await firstValueFrom(
      mechanic.perform(id, { kind: 'checklist', items: [{ ...first, state: 'Replace' }] }),
    );
    expect(checked.checklist[0]?.state).toBe('Replace');
    const photo = { id: 'p1', fileName: 'roda.png', contentType: 'image/png', dataUrl: PNG };
    await firstValueFrom(mechanic.perform(id, { kind: 'photo', photo }));
    const again = await firstValueFrom(mechanic.perform(id, { kind: 'photo', photo }));
    expect(again.photos).toHaveLength(1);
    expect(
      (
        await failure(
          mechanic.perform(id, { kind: 'photo', photo: { ...photo, contentType: 'image/gif' } }),
        )
      ).status,
    ).toBe(400);
    expect(
      (
        await failure(
          mechanic.perform(id, {
            kind: 'photo',
            photo: { ...photo, id: 'big', dataUrl: PNG + 'A'.repeat(7_000_000) },
          }),
        )
      ).fieldErrors[0]?.message,
    ).toBe(messages.fake.serviceOrderPhotoSize);

    const suggestions = await firstValueFrom(
      mechanic.suggest(id, 'bieleta com folga e pastilha gasta'),
    );
    expect(suggestions.map((item) => item.name)).toEqual([
      'Alinhamento 3D',
      'Pastilha de freio dianteira',
    ]);
    expect((await failure(mechanic.suggest(id, 'x'))).status).toBe(400);
    await firstValueFrom(
      mechanic.perform(id, { kind: 'diagnosis', description: 'Bieleta com folga' }),
    );
    const sent = await firstValueFrom(
      mechanic.perform(id, {
        kind: 'sendToQuote',
        request: {
          description: 'Bieleta com folga',
          items: suggestions.map(({ kind, itemId }) => ({ kind, itemId })),
        },
      }),
    );
    expect(sent).toMatchObject({ sentToQuote: true, diagnosis: 'Bieleta com folga' });
    expect(
      (
        await failure(
          mechanic.perform(orderId(3318), {
            kind: 'sendToQuote',
            request: { description: 'x', items: [] },
          }),
        )
      ).status,
    ).toBe(422);
  });

  it('starts diagnosis, runs and finishes the service', async () => {
    const { mechanic } = await setup(MECHANIC);
    const received = orderId(3326);

    expect((await firstValueFrom(mechanic.perform(received, { kind: 'start' }))).status).toBe(
      'InDiagnosis',
    );
    const id = orderId(3318);
    expect((await firstValueFrom(mechanic.perform(id, { kind: 'start' }))).timerRunning).toBe(true);
    const finished = await firstValueFrom(mechanic.perform(id, { kind: 'finish' }));
    expect(finished).toMatchObject({ status: 'Completed', timerRunning: false });
    expect((await failure(mechanic.perform(id, { kind: 'finish' }))).status).toBe(422);
    expect((await failure(mechanic.perform(id, { kind: 'start' }))).status).toBe(422);
  });

  it('keeps attendants out of the mechanic routes and mechanics out of transitions', async () => {
    const { orders, mechanic } = await setup(ATTENDANT);
    expect((await failure(mechanic.mine())).status).toBe(403);

    await harness.login(MECHANIC);
    expect(
      (
        await failure(
          orders.transition(orderId(3326), { to: 'InDiagnosis', version: 1, reason: null }),
        )
      ).status,
    ).toBe(403);
  });
});

describe('fake service order helpers', () => {
  it('moves orders and maps mechanic DTOs', () => {
    const order = {
      status: 'WaitingApproval',
      version: 3,
      history: [],
      quoteStatus: 'Pending',
      updatedAt: '',
    } as unknown as Parameters<typeof moveOrder>[0];
    moveOrder(order, 'InProgress', 'Ana', null);
    expect(order).toMatchObject({ status: 'InProgress', version: 4, quoteStatus: 'Approved' });
    expect(
      toMechanicOrder({ id: 'o', number: 1, status: 'Received', licensePlate: 'A' }),
    ).toMatchObject({
      checklist: [],
      photos: [],
      timerRunning: false,
    });
  });
});
