import { HttpClient, HttpContext, HttpRequest } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { throwError, TimeoutError } from 'rxjs';
import { API_URL, provideInterceptorTest } from '../../../../testing/http';

const REFRESH_URL = API_URL.replace('/api/admin/customers', '/api/auth/refresh');
import { AuthTokenStore } from '../../auth/auth-token.store';
import { LOGIN_ROUTE, SessionService } from '../../auth/session.service';
import { messages } from '../../i18n/messages.pt-BR';
import { ToastService } from '../../../shared/ui/toast/toast';
import { ApiError, TIMEOUT_ERROR_CODE } from '../api-error';
import { SKIP_GLOBAL_ERROR_HANDLING } from '../http-context';
import { correlationInterceptor } from './correlation.interceptor';
import { errorInterceptor, FORBIDDEN_ROUTE } from './error.interceptor';

describe('errorInterceptor', () => {
  function setup(): {
    client: HttpClient;
    http: HttpTestingController;
    navigate: ReturnType<typeof vi.spyOn>;
    toast: ToastService;
  } {
    TestBed.configureTestingModule({
      providers: provideInterceptorTest(correlationInterceptor, errorInterceptor),
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    return {
      client: TestBed.inject(HttpClient),
      http: TestBed.inject(HttpTestingController),
      navigate,
      toast: TestBed.inject(ToastService),
    };
  }

  function failWith(
    status: number,
    body: object | null = null,
    context = new HttpContext(),
  ): ApiError | undefined {
    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);
    let error: ApiError | undefined;
    client.get(API_URL, { context }).subscribe({ error: (value: ApiError) => (error = value) });
    http.expectOne(API_URL).flush(body, { status, statusText: 'Error' });
    return error;
  }

  it('ends the session on 401 when it cannot be renewed', () => {
    const { navigate, http } = setup();
    TestBed.inject(AuthTokenStore).set({ accessToken: 'abc', expiresAt: '2099-01-01T00:00:00Z' });
    let error: ApiError | undefined;

    TestBed.inject(HttpClient)
      .get(API_URL)
      .subscribe({ error: (value: ApiError) => (error = value) });
    http.expectOne(API_URL).flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne(REFRESH_URL).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(error?.status).toBe(401);
    expect(TestBed.inject(AuthTokenStore).token()).toBeNull();
    expect(TestBed.inject(SessionService).endReason()).toBe('expired');
    expect(navigate).toHaveBeenCalledWith(LOGIN_ROUTE);
  });

  it('renews the session and repeats the request once on 401', () => {
    const { http } = setup();
    let body: unknown;

    TestBed.inject(HttpClient)
      .get(API_URL)
      .subscribe((value) => (body = value));
    http.expectOne(API_URL).flush(null, { status: 401, statusText: 'Unauthorized' });
    http
      .expectOne(REFRESH_URL)
      .flush({ accessToken: 'renewed', expiresAt: '2099-01-01T00:00:00Z' });
    const retry = http.expectOne(API_URL);
    retry.flush({ ok: true });

    expect(retry.request.headers.get('Authorization')).toBe('Bearer renewed');
    expect(TestBed.inject(AuthTokenStore).token()).toBe('renewed');
    expect(body).toEqual({ ok: true });
  });

  it('expires the session when the repeated request is still unauthorized', () => {
    const { http, navigate } = setup();
    let error: ApiError | undefined;

    TestBed.inject(HttpClient)
      .get(API_URL)
      .subscribe({ error: (value: ApiError) => (error = value) });
    http.expectOne(API_URL).flush(null, { status: 401, statusText: 'Unauthorized' });
    http
      .expectOne(REFRESH_URL)
      .flush({ accessToken: 'renewed', expiresAt: '2099-01-01T00:00:00Z' });
    http.expectOne(API_URL).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(error?.status).toBe(401);
    expect(navigate).toHaveBeenCalledWith(LOGIN_ROUTE);
  });

  it('opens the access denied screen on 403', () => {
    const { navigate } = setup();

    expect(failWith(403)?.code).toBe('common.forbidden');
    expect(navigate).toHaveBeenCalledWith(FORBIDDEN_ROUTE);
  });

  it('shows a toast with the trace id on 5xx', () => {
    const { toast } = setup();

    const error = failWith(500, {
      detail: 'Falhou.',
      code: 'common.unexpected',
      traceId: 'trace-1',
    });

    expect(error?.traceId).toBe('trace-1');
    expect(toast.toasts()).toEqual([
      expect.objectContaining({
        message: 'Falhou.',
        tone: 'error',
        detail: messages.errors.supportCode('trace-1'),
      }),
    ]);
  });

  it('uses the request trace when the server sends none', () => {
    const { toast } = setup();

    const error = failWith(502);

    expect(error?.traceId).toMatch(/^00-/);
    expect(toast.toasts()[0]?.detail).toContain(error?.traceId ?? 'missing');
  });

  it('leaves 404, 409, 422 and 429 to the screen', () => {
    const { navigate, toast } = setup();

    expect([404, 409, 422, 429].map((status) => failWith(status)?.status)).toEqual([
      404, 409, 422, 429,
    ]);
    expect(navigate).not.toHaveBeenCalled();
    expect(toast.toasts()).toEqual([]);
  });

  it('skips global handling when the request asks for it', () => {
    const { navigate, toast } = setup();
    const context = new HttpContext().set(SKIP_GLOBAL_ERROR_HANDLING, true);

    expect(failWith(401, null, context)?.status).toBe(401);
    expect(failWith(500, null, context)?.status).toBe(500);
    expect(navigate).not.toHaveBeenCalled();
    expect(toast.toasts()).toEqual([]);
  });

  it('shows a toast for network failure and timeout', () => {
    const { client, http, toast } = setup();
    let timeoutError: ApiError | undefined;

    client.get(API_URL).subscribe({ error: () => undefined });
    http.expectOne(API_URL).error(new ProgressEvent('error'));
    TestBed.runInInjectionContext(() =>
      errorInterceptor(new HttpRequest('GET', API_URL), () => throwError(() => new TimeoutError())),
    ).subscribe({ error: (value: ApiError) => (timeoutError = value) });

    expect(timeoutError?.code).toBe(TIMEOUT_ERROR_CODE);
    expect(toast.toasts().map((item) => item.message)).toEqual([
      messages.errors.network,
      messages.errors.timeout,
    ]);
  });
});
