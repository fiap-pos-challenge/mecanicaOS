import { HttpClient, HttpContext } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TimeoutError } from 'rxjs';
import { API_URL, provideInterceptorTest } from '../../../../testing/http';
import { REQUEST_TIMEOUT_MS } from '../http-context';
import { timeoutInterceptor } from './timeout.interceptor';

describe('timeoutInterceptor', () => {
  afterEach(() => vi.useRealTimers());

  it('fails slow requests with TimeoutError', () => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: provideInterceptorTest(timeoutInterceptor) });
    let error: unknown;

    TestBed.inject(HttpClient)
      .get(API_URL, { context: new HttpContext().set(REQUEST_TIMEOUT_MS, 50) })
      .subscribe({ error: (value: unknown) => (error = value) });
    vi.advanceTimersByTime(49);
    expect(error).toBeUndefined();
    vi.advanceTimersByTime(1);

    expect(error).toBeInstanceOf(TimeoutError);
    expect(TestBed.inject(HttpTestingController).expectOne(API_URL).cancelled).toBe(true);
  });

  it('lets fast requests through with the default limit', () => {
    TestBed.configureTestingModule({ providers: provideInterceptorTest(timeoutInterceptor) });
    let body: unknown;

    TestBed.inject(HttpClient)
      .get(API_URL)
      .subscribe((value) => (body = value));
    TestBed.inject(HttpTestingController).expectOne(API_URL).flush({ ok: true });

    expect(body).toEqual({ ok: true });
  });
});
