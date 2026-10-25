import { HttpClient, HttpContext } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL, provideInterceptorTest } from '../../../../testing/http';
import { AuthTokenStore } from '../../auth/auth-token.store';
import { SKIP_AUTH } from '../http-context';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  function setup(): { client: HttpClient; http: HttpTestingController; tokens: AuthTokenStore } {
    TestBed.configureTestingModule({ providers: provideInterceptorTest(authInterceptor) });
    return {
      client: TestBed.inject(HttpClient),
      http: TestBed.inject(HttpTestingController),
      tokens: TestBed.inject(AuthTokenStore),
    };
  }

  it('sends the in-memory token as Bearer to the API', () => {
    const { client, http, tokens } = setup();
    tokens.set({ accessToken: 'abc', expiresAt: '2099-01-01T00:00:00Z' });

    client.get(API_URL).subscribe();

    expect(http.expectOne(API_URL).request.headers.get('Authorization')).toBe('Bearer abc');
  });

  it('never sends the token to other hosts, without session or when skipped', () => {
    const { client, http, tokens } = setup();
    client.get(API_URL).subscribe();
    expect(http.expectOne(API_URL).request.headers.has('Authorization')).toBe(false);

    tokens.set({ accessToken: 'abc', expiresAt: '2099-01-01T00:00:00Z' });
    client.get('https://evil.example.com/api/x').subscribe();
    client.get(API_URL, { context: new HttpContext().set(SKIP_AUTH, true) }).subscribe();

    expect(
      http.expectOne('https://evil.example.com/api/x').request.headers.has('Authorization'),
    ).toBe(false);
    expect(http.expectOne(API_URL).request.headers.has('Authorization')).toBe(false);
  });
});
