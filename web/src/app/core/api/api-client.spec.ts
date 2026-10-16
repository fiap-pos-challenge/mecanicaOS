import { HttpContext, HttpContextToken } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideInterceptorTest } from '../../../testing/http';
import { ApiClient, OperationShape, resolvePath, toHttpParams } from './api-client';
import { isApiUrl } from './api-url';

interface ItemOperation extends OperationShape {
  readonly url: '/api/items/{id}';
  readonly path: { readonly id: string };
  readonly query?: { readonly page?: number; readonly active?: boolean; readonly term?: string };
  readonly body?: { readonly name: string };
}

const MARK = new HttpContextToken(() => false);

describe('ApiClient', () => {
  it('fills path parameters safely', () => {
    expect(resolvePath('/api/items/{id}', { id: 'a/b c' })).toBe('/api/items/a%2Fb%20c');
    expect(resolvePath('/api/items', undefined)).toBe('/api/items');
    expect(resolvePath('/api/items/{id}', {})).toBe('/api/items/');
  });

  it('skips undefined query values', () => {
    expect(toHttpParams({ page: 2, active: false, term: undefined }).toString()).toBe(
      'page=2&active=false',
    );
    expect(toHttpParams(undefined).keys()).toEqual([]);
  });

  it('detects API urls', () => {
    expect(isApiUrl('/api/items', '', '/api')).toBe(true);
    expect(isApiUrl('/api', '', '/api')).toBe(true);
    expect(isApiUrl('/apis', '', '/api')).toBe(false);
    expect(isApiUrl('https://cdn.example.com/api/x', '', '/api')).toBe(false);
  });

  it('sends typed requests with origin, body, query and context', () => {
    TestBed.configureTestingModule({ providers: provideInterceptorTest() });
    const client = TestBed.inject(ApiClient);
    const http = TestBed.inject(HttpTestingController);
    let result: unknown;

    client
      .send<ItemOperation, { id: string }>('PUT', '/api/items/{id}', {
        path: { id: '7' },
        query: { page: 1 },
        body: { name: 'Filtro' },
        context: new HttpContext().set(MARK, true),
      })
      .subscribe((value) => (result = value));
    const request = http.expectOne('http://localhost/api/items/7?page=1');
    request.flush({ id: '7' });

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Filtro' });
    expect(request.request.context.get(MARK)).toBe(true);
    expect(result).toEqual({ id: '7' });

    client.send<ItemOperation, null>('GET', '/api/items/{id}', { path: { id: '8' } }).subscribe();
    const plain = http.expectOne('http://localhost/api/items/8').request;
    expect(plain.context.get(MARK)).toBe(false);
    expect(plain.headers.has('X-Portal-Session')).toBe(false);
    client
      .send<ItemOperation, null>('GET', '/api/items/{id}', {
        path: { id: '9' },
        headers: { 'X-Portal-Session': 's' },
      })
      .subscribe();
    expect(
      http.expectOne('http://localhost/api/items/9').request.headers.get('X-Portal-Session'),
    ).toBe('s');
    http.verify();
  });
});
