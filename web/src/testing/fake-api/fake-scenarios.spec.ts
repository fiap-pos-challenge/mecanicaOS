import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TimeoutError, firstValueFrom } from 'rxjs';
import { provideTestApi, TEST_ORIGIN } from '../api';
import { TIMEOUT_ERROR_CODE } from '../../app/core/http/api-error';
import { FAKE_SCENARIOS, FakeScenarios, scenarioResult } from './fake-api/fake-scenarios';
import { FakeRouter, readBody } from './fake-api/fake-http';
import { FakeBackend } from './fake-backend.service';
import { toMswHandler } from './msw-handlers';
import { createFakeApis } from './fake-backend.service';
import { setupServer } from 'msw/node';

describe('fake scenarios', () => {
  it('produces one result per scenario', () => {
    expect(
      FAKE_SCENARIOS.map((scenario) => {
        const result = scenarioResult(scenario);
        return result === null ? null : 'timeout' in result ? 'timeout' : result.status;
      }),
    ).toEqual([null, 403, 404, 409, 422, 429, 500, 'timeout']);
    expect(scenarioResult('tooManyRequests')).toMatchObject({ headers: { 'Retry-After': '1' } });
  });

  it('keeps one active scenario per feature', () => {
    const scenarios = new FakeScenarios();
    scenarios.set('customers', 'conflict');

    expect(scenarios.get('customers')).toBe('conflict');
    expect(scenarios.get('users')).toBe('none');
    scenarios.set('customers', 'none');
    expect(scenarios.get('customers')).toBe('none');
    scenarios.set('users', 'timeout');
    scenarios.reset();
    expect(scenarios.get('users')).toBe('none');
  });

  it('lets the app switch scenarios and reset data', () => {
    const backend = TestBed.inject(FakeBackend);
    backend.setScenario('users', 'serverError');
    backend.state.database.customers.length = 0;

    expect(backend.scenario('users')).toBe('serverError');
    backend.reset();
    expect(backend.scenario('users')).toBe('none');
    expect(backend.state.database.customers).toHaveLength(3);
  });

  it('times out in mock mode after the retries', async () => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: provideTestApi('mock') });
    const backend = TestBed.inject(FakeBackend);
    backend.setScenario('customers', 'timeout');
    const result = firstValueFrom(
      TestBed.inject(HttpClient).get(`${TEST_ORIGIN}/api/admin/customers`),
    ).catch((error: unknown) => error);

    await vi.advanceTimersByTimeAsync(3 * 15_000 + 900);
    vi.useRealTimers();

    expect(await result).toMatchObject({ code: TIMEOUT_ERROR_CODE });
    expect(new TimeoutError()).toBeInstanceOf(Error);
  });

  it('never answers through MSW in the timeout scenario', async () => {
    const apis = createFakeApis();
    apis.state.scenarios.set('customers', 'timeout');
    const server = setupServer(toMswHandler(apis.customers, { latencyMs: 0 }));
    server.listen({ onUnhandledRequest: 'error' });

    await expect(
      fetch(`${TEST_ORIGIN}/api/admin/customers`, { signal: AbortSignal.timeout(50) }),
    ).rejects.toThrow();
    server.close();
  });

  it('applies latency, ignores unknown methods and answers problems through MSW', async () => {
    const apis = createFakeApis();
    const server = setupServer(
      toMswHandler(apis.customers, { latencyMs: 5 }),
      toMswHandler(apis.auth, { latencyMs: 0 }),
    );
    server.listen({ onUnhandledRequest: 'bypass' });

    const unauthorized = await fetch(`${TEST_ORIGIN}/api/admin/customers`);

    expect(apis.customers.handles('/api/admin/customers', 'PATCH')).toBe(false);
    expect(apis.customers.handles('/api/admin/customers/summaries', 'POST')).toBe(false);
    expect(apis.insights.handles('/api/admin/customers/summaries', 'POST')).toBe(true);
    expect(unauthorized.status).toBe(401);
    expect(unauthorized.headers.get('Content-Type')).toBe('application/problem+json');
    server.close();
  });

  it('routes with decoded params and reads string bodies', () => {
    const router = new FakeRouter().on('GET', '/api/items/:id', (_request, params) => ({
      status: 200,
      body: params['id'],
    }));
    const request = {
      method: 'GET',
      path: '/api/items/a%20b',
      query: new URLSearchParams(),
      body: '{"x":1}',
      authorization: null,
    };

    expect(router.dispatch(request)).toEqual({ status: 200, body: 'a b' });
    expect(router.dispatch({ ...request, method: 'POST' })).toBeNull();
    expect(readBody(request)).toEqual({ x: 1 });
    expect(readBody({ ...request, body: null })).toEqual({});
  });
});
