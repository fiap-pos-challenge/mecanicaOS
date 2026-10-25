import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { TimeoutError } from 'rxjs';
import { messages } from '../i18n/messages.pt-BR';
import { isApiError, NETWORK_ERROR_CODE, TIMEOUT_ERROR_CODE, toApiError } from './api-error';

const trace = '00-abc-def-01';

function response(status: number, error: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error, headers: new HttpHeaders() });
}

describe('toApiError', () => {
  it('maps backend ProblemDetails with code, traceId and field errors', () => {
    const error = toApiError(
      response(400, {
        title: 'Bad Request',
        detail: 'Dados inválidos.',
        code: 'common.validationFailed',
        traceId: 'server-trace',
        errors: [
          { field: 'document', code: 'customer.invalidDocument', message: 'Documento inválido.' },
          { field: 'name' },
        ],
      }),
      trace,
    );

    expect(error).toEqual({
      kind: 'api-error',
      status: 400,
      code: 'common.validationFailed',
      title: 'Bad Request',
      detail: 'Dados inválidos.',
      traceId: 'server-trace',
      fieldErrors: [
        { field: 'document', code: 'customer.invalidDocument', message: 'Documento inválido.' },
      ],
      retryAfterSeconds: null,
    });
    expect(isApiError(error)).toBe(true);
  });

  it('maps ASP.NET validation dictionaries to camelCase fields', () => {
    const error = toApiError(
      response(400, { errors: { Name: ['Obrigatório.'], Email: 'Inválido.' } }),
      trace,
    );

    expect(error.fieldErrors).toEqual([
      { field: 'name', code: 'common.validationFailed', message: 'Obrigatório.' },
      { field: 'email', code: 'common.validationFailed', message: 'Inválido.' },
    ]);
    expect(error.detail).toBe(messages.errors.validation);
    expect(error.traceId).toBe(trace);
  });

  it.each([
    [401, 'common.unauthorized', messages.errors.unauthorized],
    [403, 'common.forbidden', messages.errors.forbidden],
    [404, 'common.notFound', messages.errors.notFound],
    [409, 'common.conflict', messages.errors.conflict],
    [422, 'common.businessRule', messages.errors.businessRule],
    [429, 'common.tooManyRequests', messages.errors.tooManyRequests],
    [500, 'common.unexpected', messages.errors.server],
    [503, 'common.unexpected', messages.errors.server],
    [418, 'http.unknown', messages.errors.unknown],
  ])('uses a front fallback for status %i without body', (status, code, detail) => {
    const error = toApiError(response(status, 'plain text'), trace);

    expect(error).toMatchObject({ status, code, detail, title: detail, fieldErrors: [] });
  });

  it('maps network failure, timeout and unknown errors', () => {
    expect(toApiError(response(0, new ProgressEvent('error')), trace)).toMatchObject({
      code: NETWORK_ERROR_CODE,
      title: messages.errors.networkTitle,
      traceId: trace,
    });
    expect(toApiError(new TimeoutError(), null)).toMatchObject({
      code: TIMEOUT_ERROR_CODE,
      traceId: null,
    });
    expect(toApiError(new Error('boom'), null)).toMatchObject({ code: 'http.unknown', status: 0 });
  });

  it('reads the waiting time from the body or the Retry-After header', () => {
    expect(toApiError(response(429, { retryAfterSeconds: 42.2 }), trace).retryAfterSeconds).toBe(
      43,
    );
    expect(
      toApiError(
        new HttpErrorResponse({ status: 429, headers: new HttpHeaders({ 'Retry-After': '7' }) }),
        trace,
      ).retryAfterSeconds,
    ).toBe(7);
    expect(toApiError(response(429, null), trace).retryAfterSeconds).toBeNull();
  });

  it('keeps errors that are already mapped', () => {
    const error = toApiError(response(404, null), trace);

    expect(toApiError(error, 'other')).toBe(error);
    expect(isApiError(null)).toBe(false);
  });
});
