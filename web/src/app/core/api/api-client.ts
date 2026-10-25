import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_ORIGIN } from '../config/app-environment';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE';

type QueryValue = string | number | boolean | undefined;

export interface OperationShape {
  readonly url: string;
  readonly path?: object;
  readonly query?: object;
  readonly body?: unknown;
}

export interface ApiCallOptions<TOperation extends OperationShape> {
  readonly path?: TOperation['path'];
  readonly query?: TOperation['query'];
  readonly body?: TOperation['body'];
  readonly context?: HttpContext;
  readonly headers?: Readonly<Record<string, string>>;
}

export function resolvePath(url: string, path: object | undefined): string {
  const values = (path ?? {}) as Readonly<Record<string, string | number>>;
  return url.replace(/\{(\w+)\}/g, (_, name: string) =>
    encodeURIComponent(String(values[name] ?? '')),
  );
}

export function toHttpParams(query: object | undefined): HttpParams {
  return Object.entries((query ?? {}) as Readonly<Record<string, QueryValue>>).reduce(
    (params, [key, value]) => (value === undefined ? params : params.set(key, String(value))),
    new HttpParams(),
  );
}

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly origin = inject(API_ORIGIN);

  send<TOperation extends OperationShape, TResponse>(
    method: HttpMethod,
    url: TOperation['url'],
    options: ApiCallOptions<TOperation> = {},
  ): Observable<TResponse> {
    return this.http.request<TResponse>(method, `${this.origin}${resolvePath(url, options.path)}`, {
      body: options.body,
      params: toHttpParams(options.query),
      context: options.context ?? new HttpContext(),
      ...(options.headers ? { headers: { ...options.headers } } : {}),
      responseType: 'json',
    });
  }
}
