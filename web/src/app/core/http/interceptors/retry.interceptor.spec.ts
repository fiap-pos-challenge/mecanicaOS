import { HttpClient, HttpContext, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TimeoutError } from 'rxjs';
import { API_URL, provideInterceptorTest } from '../../../../testing/http';
import { RETRY_ENABLED } from '../http-context';
import { backoffMs, isRetryable, retryAfterSeconds, retryInterceptor } from './retry.interceptor';

describe('retryInterceptor', () => {
  afterEach(() => vi.useRealTimers());

  function setup(): { client: HttpClient; http: HttpTestingController } {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: provideInterceptorTest(retryInterceptor) });
    return { client: TestBed.inject(HttpClient), http: TestBed.inject(HttpTestingController) };
  }

  it('classifies retryable failures and computes the delay', () => {
    expect(isRetryable(new TimeoutError())).toBe(true);
    expect(isRetryable(new HttpErrorResponse({ status: 503 }))).toBe(true);
    expect(isRetryable(new HttpErrorResponse({ status: 500 }))).toBe(false);
    expect(isRetryable(new Error('x'))).toBe(false);
    expect(
      retryAfterSeconds(
        new HttpErrorResponse({ status: 429, headers: new HttpHeaders({ 'Retry-After': '2' }) }),
      ),
    ).toBe(2);
    expect(
      retryAfterSeconds(
        new HttpErrorResponse({ status: 429, headers: new HttpHeaders({ 'Retry-After': 'soon' }) }),
      ),
    ).toBeNull();
    expect(retryAfterSeconds(new Error('x'))).toBeNull();
    expect([backoffMs(1, null), backoffMs(2, null), backoffMs(1, 3)]).toEqual([300, 600, 3000]);
  });

  it('retries idempotent GET twice with growing delay', () => {
    const { client, http } = setup();
    let body: unknown;

    client.get(API_URL).subscribe((value) => (body = value));
    http.expectOne(API_URL).flush(null, { status: 503, statusText: 'Unavailable' });
    vi.advanceTimersByTime(299);
    http.expectNone(API_URL);
    vi.advanceTimersByTime(1);
    http.expectOne(API_URL).flush(null, { status: 0, statusText: 'Network' });
    vi.advanceTimersByTime(600);
    http.expectOne(API_URL).flush({ ok: true });

    expect(body).toEqual({ ok: true });
  });

  it('gives up after two retries and respects Retry-After', () => {
    const { client, http } = setup();
    let error: HttpErrorResponse | undefined;

    client.get(API_URL).subscribe({ error: (value: HttpErrorResponse) => (error = value) });
    http
      .expectOne(API_URL)
      .flush(null, { status: 429, statusText: 'Too Many', headers: { 'Retry-After': '1' } });
    vi.advanceTimersByTime(999);
    http.expectNone(API_URL);
    vi.advanceTimersByTime(1);
    http.expectOne(API_URL).flush(null, { status: 502, statusText: 'Bad Gateway' });
    vi.advanceTimersByTime(600);
    http.expectOne(API_URL).flush(null, { status: 504, statusText: 'Gateway Timeout' });

    expect(error?.status).toBe(504);
  });

  it('never retries non idempotent methods, client errors or opted-out requests', () => {
    const { client, http } = setup();
    let failures = 0;
    const fail = { error: () => (failures += 1) };

    client.post(API_URL, {}).subscribe(fail);
    http.expectOne(API_URL).flush(null, { status: 503, statusText: 'Unavailable' });
    client.get(API_URL).subscribe(fail);
    http.expectOne(API_URL).flush(null, { status: 409, statusText: 'Conflict' });
    client.get(API_URL, { context: new HttpContext().set(RETRY_ENABLED, false) }).subscribe(fail);
    http.expectOne(API_URL).flush(null, { status: 503, statusText: 'Unavailable' });
    vi.advanceTimersByTime(5000);

    http.expectNone(API_URL);
    expect(failures).toBe(3);
  });
});
