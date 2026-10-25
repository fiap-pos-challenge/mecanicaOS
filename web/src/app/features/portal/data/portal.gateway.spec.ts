import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import {
  expireCharges,
  PORTAL_DONE_TOKEN,
  PORTAL_EXPIRED_TOKEN,
  PORTAL_TOKEN,
  PROVIDER_CHECKOUT,
  SAMPLE_PDF_URL,
} from '../../../../testing/fake-api/portal/portal.fake-api';
import { ContractError } from '../../../core/data/contract-error';
import { ApiError } from '../../../core/http/api-error';
import { cents } from '../../../shared/util/money';
import { DocumentsGateway, toDocument } from '../../documents/data/documents.gateway';
import { HttpPortalGateway, PortalGateway, toPortalOrder } from './portal.gateway';
import { portalSteps, portalTotal } from './portal.models';

async function failure(source: Observable<unknown>): Promise<ApiError> {
  try {
    await firstValueFrom(source);
  } catch (error) {
    return error as ApiError;
  }

  throw new Error('Expected the request to fail.');
}

describe('portal math', () => {
  const items = [
    { id: 'a', name: 'A', kind: 'Service' as const, total: cents(30_000) },
    { id: 'b', name: 'B', kind: 'Part' as const, total: cents(10_000) },
    { id: 'c', name: 'C', kind: 'Part' as const, total: cents(5_001) },
  ];

  it('recalculates the total and prorates the discount', () => {
    expect(portalTotal(items, new Set(['a', 'b', 'c']), cents(4_500))).toEqual({
      subtotal: 45_001,
      discount: 4_500,
      total: 40_501,
    });
    expect(portalTotal(items, new Set(['b']), cents(4_500))).toEqual({
      subtotal: 10_000,
      discount: 1_000,
      total: 9_000,
    });
    expect(portalTotal(items, new Set(), cents(4_500))).toEqual({
      subtotal: 0,
      discount: 0,
      total: 0,
    });
    expect(portalTotal([], new Set(), cents(100)).total).toBe(0);
    expect(portalTotal(items, new Set(['c']), cents(999_999)).total).toBe(0);
  });

  it('builds the timeline with done, current and next steps', () => {
    const steps = portalSteps({
      status: 'WaitingApproval',
      history: [
        { status: 'Received', changedAt: 'r' },
        { status: 'InDiagnosis', changedAt: 'd1' },
        { status: 'WaitingApproval', changedAt: 'w' },
        { status: 'InDiagnosis', changedAt: 'd2' },
      ],
    });
    expect(steps.map((step) => [step.status, step.state, step.at])).toEqual([
      ['Received', 'done', 'r'],
      ['InDiagnosis', 'done', 'd2'],
      ['WaitingApproval', 'current', 'w'],
      ['InProgress', 'next', null],
      ['Completed', 'next', null],
      ['Delivered', 'next', null],
    ]);
  });

  it('requires the contract fields', () => {
    expect(() => toPortalOrder({})).toThrow(ContractError);
    expect(() => toDocument({})).toThrow(ContractError);
    expect(toPortalOrder({ number: 1, licensePlate: 'A', status: 'Received' })).toMatchObject({
      quote: null,
      payment: null,
      photos: [],
    });
  });
});

describe.each(API_MODES)('PortalGateway and DocumentsGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  function setup() {
    harness.configure(mode);
    return { portal: TestBed.inject(PortalGateway), documents: TestBed.inject(DocumentsGateway) };
  }

  async function session(portal: PortalGateway, token = PORTAL_TOKEN): Promise<string> {
    const result = await firstValueFrom(portal.confirm(token, '321'));
    if (result.kind !== 'confirmed') {
      throw new Error('Not confirmed');
    }

    return result.session;
  }

  it('hides invalid and expired links behind the same answer and locks after three errors', async () => {
    const { portal } = setup();

    expect(portal).toBeInstanceOf(HttpPortalGateway);
    const invalid = await failure(portal.access('nope'));
    const expired = await failure(portal.access(PORTAL_EXPIRED_TOKEN));
    expect([invalid.status, invalid.code, invalid.detail]).toEqual([
      expired.status,
      expired.code,
      expired.detail,
    ]);
    expect(await firstValueFrom(portal.access(PORTAL_TOKEN))).toBe(3);
    expect(await firstValueFrom(portal.confirm(PORTAL_TOKEN, '000'))).toEqual({
      kind: 'wrongDigits',
      attemptsLeft: 2,
    });
    expect(await firstValueFrom(portal.confirm(PORTAL_TOKEN, '111'))).toEqual({
      kind: 'wrongDigits',
      attemptsLeft: 1,
    });
    expect((await failure(portal.confirm(PORTAL_TOKEN, '222'))).status).toBe(423);
    expect((await failure(portal.access(PORTAL_TOKEN))).status).toBe(423);
    expect((await failure(portal.confirm(PORTAL_TOKEN, '321'))).status).toBe(423);
    expect((await failure(portal.confirm('nope', '321'))).status).toBe(404);
  });

  it('shows the order and approves part of the quote once', async () => {
    const { portal } = setup();
    const id = await session(portal);

    const order = await firstValueFrom(portal.order(id));
    expect(order).toMatchObject({
      number: 3324,
      customerFirstName: 'Joana',
      status: 'WaitingApproval',
      payment: null,
    });
    expect(order.quote?.items).toHaveLength(3);
    expect(order.photos).toHaveLength(1);
    expect((await failure(portal.approve(id, []))).status).toBe(400);
    const approved = await firstValueFrom(portal.approve(id, [order.quote?.items[0]?.id ?? '']));
    expect(approved.quote).toMatchObject({ status: 'Approved' });
    expect(approved.quote?.items).toHaveLength(1);
    expect(approved).toMatchObject({ status: 'InProgress', payment: { status: 'Pending' } });
    expect((await failure(portal.approve(id, ['x']))).status).toBe(409);
    expect((await failure(portal.reject(id, 'Muito caro'))).status).toBe(409);
    expect((await failure(portal.order('expired-session'))).status).toBe(401);
  });

  it('rejects with a reason', async () => {
    const { portal } = setup();
    const id = await session(portal);

    expect((await failure(portal.reject(id, 'não'))).status).toBe(400);
    const rejected = await firstValueFrom(portal.reject(id, 'Vou fazer depois'));
    expect(rejected).toMatchObject({ status: 'InDiagnosis', quote: { status: 'Rejected' } });
  });

  it('pays with Pix after the bank confirmation and handles expiration and card redirect', async () => {
    const { portal } = setup();
    const id = await session(portal);

    expect((await failure(portal.pix(id))).status).toBe(422);
    expect((await failure(portal.card(id))).status).toBe(422);
    const order = await firstValueFrom(portal.order(id));
    await firstValueFrom(portal.approve(id, order.quote?.items.map((item) => item.id) ?? []));
    const pix = await firstValueFrom(portal.pix(id));
    expect(pix.copyPaste).toContain('BR.GOV.BCB.PIX');
    expect(pix.qrCodeDataUrl.startsWith('data:image/svg+xml')).toBe(true);
    expect(await firstValueFrom(portal.charge(id, pix.paymentId))).toBe('Pending');
    expect(await firstValueFrom(portal.charge(id, pix.paymentId))).toBe('Paid');
    expect((await firstValueFrom(portal.order(id))).payment).toMatchObject({
      status: 'Paid',
      amountDue: 0,
    });
    expect((await failure(portal.charge(id, 'missing'))).status).toBe(404);
    expect((await failure(portal.pix(id))).status).toBe(422);
  });

  it('expires Pix charges and redirects card payments to the provider', async () => {
    const { portal } = setup();
    const id = await session(portal);
    const order = await firstValueFrom(portal.order(id));
    await firstValueFrom(portal.approve(id, order.quote?.items.map((item) => item.id) ?? []));

    const pix = await firstValueFrom(portal.pix(id));
    expireCharges(harness.state(mode));
    expect(await firstValueFrom(portal.charge(id, pix.paymentId))).toBe('Expired');
    expect((await firstValueFrom(portal.card(id))).startsWith(PROVIDER_CHECKOUT)).toBe(true);
  });

  it('serves documents to the portal and staff and verifies authenticity', async () => {
    const { portal, documents } = setup();
    const id = await session(portal, PORTAL_DONE_TOKEN);

    const order = await firstValueFrom(portal.order(id));
    expect(order.documents.map((document) => document.code)).toEqual([
      'OS01-3310-QW8P',
      'REC1-3310-M2XT',
    ]);
    const receipt = await firstValueFrom(documents.get('REC1-3310-M2XT', id));
    expect(receipt).toMatchObject({ kind: 'Receipt', number: 3310, customerName: 'Joana Prado' });
    expect(receipt.paidAt).not.toBeNull();
    expect(await firstValueFrom(documents.download('REC1-3310-M2XT', id))).toEqual({
      url: SAMPLE_PDF_URL,
      fileName: 'REC1-3310-M2XT.pdf',
    });
    expect((await failure(documents.get('ORC1-3324-A7KD', id))).status).toBe(404);
    expect((await failure(documents.download('ORC1-3324-A7KD', id))).status).toBe(404);

    expect(await firstValueFrom(documents.verify('REC1-3310-M2XT'))).toMatchObject({
      valid: true,
      kind: 'Receipt',
      number: 3310,
    });
    expect(await firstValueFrom(documents.verify('FAKE-0000'))).toEqual({
      valid: false,
      kind: null,
      number: null,
      issuedAt: null,
      total: null,
      shopName: null,
    });

    await harness.login();
    expect((await firstValueFrom(documents.get('ORC1-3324-A7KD'))).kind).toBe('Quote');
    expect((await firstValueFrom(documents.download('ORC1-3324-A7KD'))).url).toBe(SAMPLE_PDF_URL);
    await firstValueFrom(documents.send('ORC1-3324-A7KD', 'WhatsApp'));
    expect((await failure(documents.get('missing'))).status).toBe(404);
    expect((await failure(documents.download('missing'))).status).toBe(404);
    expect((await failure(documents.send('missing', 'Email'))).status).toBe(404);
  });
});
