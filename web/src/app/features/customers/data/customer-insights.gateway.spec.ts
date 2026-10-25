import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { serviceHistoryOf, seedOf } from '../../../../testing/fake-api/customers/insights.fake-api';
import {
  CustomerInsightsGateway,
  HttpCustomerInsightsGateway,
  toCustomerSummary,
  toServiceHistoryEntry,
} from './customer-insights.gateway';
import { CustomersGateway } from './customers.gateway';

describe('insights mappers and fake data', () => {
  it('fills defaults, converts money to cents and rejects missing fields', () => {
    expect(toCustomerSummary({ customerId: 'c1', totalSpent: 10.5 })).toMatchObject({
      vehicleCount: 0,
      totalSpent: 1050,
      averageTicket: 0,
      situation: 'Active',
      fleet: false,
    });
    expect(() => toCustomerSummary({})).toThrow(ContractError);
    expect(() => toServiceHistoryEntry({ number: 1 })).toThrow(ContractError);
  });

  it('generates the same history for the same customer', () => {
    const now = Date.parse('2026-10-05T12:00:00Z');

    expect(serviceHistoryOf('abc', now)).toEqual(serviceHistoryOf('abc', now));
    expect(seedOf('abc')).not.toBe(seedOf('abd'));
  });
});

describe.each(API_MODES)('CustomerInsightsGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(
    email?: string,
  ): Promise<{ gateway: CustomerInsightsGateway; ids: string[] }> {
    harness.configure(mode);
    await harness.login(email);
    const page = await firstValueFrom(
      TestBed.inject(CustomersGateway).list({ page: 1, pageSize: 20 }),
    ).catch(() => ({ items: [] }));
    return {
      gateway: TestBed.inject(CustomerInsightsGateway),
      ids: page.items.map((item) => item.id),
    };
  }

  it('summarizes customers with vehicles, totals and situation', async () => {
    const { gateway, ids } = await setup();

    expect(gateway).toBeInstanceOf(HttpCustomerInsightsGateway);
    const summaries = await firstValueFrom(gateway.summaries(ids));
    const fleet = summaries.get(ids[2] ?? '');

    expect(summaries.size).toBe(3);
    expect(fleet).toMatchObject({ vehicleCount: 3, fleet: true });
    expect(['Active', 'OpenServiceOrder', 'NoReturn']).toContain(fleet?.situation);
    expect(Number.isInteger(fleet?.totalSpent)).toBe(true);
  });

  it('skips the request without ids and reads the service history', async () => {
    const { gateway, ids } = await setup();

    expect((await firstValueFrom(gateway.summaries([]))).size).toBe(0);
    const history = await firstValueFrom(gateway.history(ids[0] ?? ''));
    expect(history).toEqual(
      serviceHistoryOf(ids[0] ?? '').map((entry) =>
        expect.objectContaining({ number: entry.number }),
      ),
    );
  });

  it('requires a session and blocks the mechanic', async () => {
    const { gateway } = await setup();
    harness.state(mode).scenarios.set('insights', 'none');

    if (mode === 'http') {
      const response = await fetch('http://localhost/api/admin/customers/summaries', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ customerIds: 'x' }),
      });
      expect(response.status).toBe(401);
    }

    TestBed.resetTestingModule();
    const mechanic = await setup('marcos@oficina.local');
    await expect(firstValueFrom(mechanic.gateway.history('x'))).rejects.toMatchObject({
      status: 403,
    });
    expect(gateway).toBeTruthy();
  });
});
