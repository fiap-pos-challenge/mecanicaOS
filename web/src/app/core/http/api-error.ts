import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';
import { messages } from '../i18n/messages.pt-BR';

export interface FieldError {
  readonly field: string;
  readonly code: string;
  readonly message: string;
}

export interface ApiError {
  readonly kind: 'api-error';
  readonly status: number;
  readonly code: string;
  readonly title: string;
  readonly detail: string;
  readonly traceId: string | null;
  readonly fieldErrors: readonly FieldError[];
  readonly retryAfterSeconds: number | null;
}

export const NETWORK_ERROR_CODE = 'network.unavailable';
export const TIMEOUT_ERROR_CODE = 'network.timeout';

const text = messages.errors;

const fallbackByStatus: Readonly<
  Record<number, { readonly code: string; readonly detail: string }>
> = {
  400: { code: 'common.validationFailed', detail: text.validation },
  401: { code: 'common.unauthorized', detail: text.unauthorized },
  403: { code: 'common.forbidden', detail: text.forbidden },
  404: { code: 'common.notFound', detail: text.notFound },
  409: { code: 'common.conflict', detail: text.conflict },
  422: { code: 'common.businessRule', detail: text.businessRule },
  429: { code: 'common.tooManyRequests', detail: text.tooManyRequests },
};

export function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === 'object' &&
    value !== null &&
    (value as { kind?: unknown }).kind === 'api-error'
  );
}

function fallbackFor(status: number): { readonly code: string; readonly detail: string } {
  return (
    fallbackByStatus[status] ??
    (status >= 500
      ? { code: 'common.unexpected', detail: text.server }
      : { code: 'http.unknown', detail: text.unknown })
  );
}

function asRecord(value: unknown): Readonly<Record<string, unknown>> | null {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function readString(record: Readonly<Record<string, unknown>> | null, key: string): string | null {
  const value = record?.[key];
  return typeof value === 'string' && value.length > 0 ? value : null;
}

function readFieldErrors(errors: unknown, defaultCode: string): readonly FieldError[] {
  if (Array.isArray(errors)) {
    return errors.flatMap((entry) => {
      const record = asRecord(entry);
      const field = readString(record, 'field');
      const message = readString(record, 'message');
      return field && message
        ? [{ field, code: readString(record, 'code') ?? defaultCode, message }]
        : [];
    });
  }

  return Object.entries(asRecord(errors) ?? {}).flatMap(([field, value]) =>
    (Array.isArray(value) ? value : [value])
      .filter((message): message is string => typeof message === 'string')
      .map((message) => ({
        field: field.charAt(0).toLowerCase() + field.slice(1),
        code: defaultCode,
        message,
      })),
  );
}

function readRetryAfter(
  error: HttpErrorResponse,
  body: Readonly<Record<string, unknown>> | null,
): number | null {
  const fromBody = body?.['retryAfterSeconds'];
  const value = typeof fromBody === 'number' ? fromBody : Number(error.headers.get('Retry-After'));
  return Number.isFinite(value) && value > 0 ? Math.ceil(value) : null;
}

function build(
  status: number,
  code: string,
  title: string,
  detail: string,
  traceId: string | null,
  fieldErrors: readonly FieldError[] = [],
  retryAfterSeconds: number | null = null,
): ApiError {
  return {
    kind: 'api-error',
    status,
    code,
    title,
    detail,
    traceId,
    fieldErrors,
    retryAfterSeconds,
  };
}

export const FEATURE_PENDING_CODE = 'feature.pending';

export function featurePendingError(): ApiError {
  return {
    kind: 'api-error',
    status: 501,
    code: FEATURE_PENDING_CODE,
    title: text.featurePendingTitle,
    detail: text.featurePending,
    traceId: null,
    fieldErrors: [],
    retryAfterSeconds: null,
  };
}

export function isFeaturePending(error: unknown): boolean {
  return isApiError(error) && error.code === FEATURE_PENDING_CODE;
}

export function toApiError(error: unknown, requestTraceId: string | null): ApiError {
  if (isApiError(error)) {
    return error;
  }

  if (error instanceof TimeoutError) {
    return build(0, TIMEOUT_ERROR_CODE, text.timeoutTitle, text.timeout, requestTraceId);
  }

  if (!(error instanceof HttpErrorResponse)) {
    return build(0, 'http.unknown', text.unknownTitle, text.unknown, requestTraceId);
  }

  if (error.status === 0) {
    return build(0, NETWORK_ERROR_CODE, text.networkTitle, text.network, requestTraceId);
  }

  const body = asRecord(error.error);
  const fallback = fallbackFor(error.status);
  const code = readString(body, 'code') ?? fallback.code;
  const detail = readString(body, 'detail') ?? fallback.detail;
  return build(
    error.status,
    code,
    readString(body, 'title') ?? detail,
    detail,
    readString(body, 'traceId') ?? requestTraceId,
    readFieldErrors(body?.['errors'], code),
    readRetryAfter(error, body),
  );
}
