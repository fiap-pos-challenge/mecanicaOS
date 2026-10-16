import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { provideInterceptorTest } from '../http';
import { fakeApiInterceptor } from './fake-api.interceptor';
import { FakeBackend } from './fake-backend.service';

describe('fakeApiInterceptor', () => {
  function setup(): HttpClient {
    TestBed.configureTestingModule({ providers: [...provideInterceptorTest(fakeApiInterceptor)] });
    return TestBed.inject(HttpClient);
  }

  it('passes through requests outside the API and answers 404 for unknown API routes', async () => {
    const client = setup();
    client.get('http://localhost/assets/logo.svg').subscribe();

    TestBed.inject(HttpTestingController).expectOne('http://localhost/assets/logo.svg').flush({});
    await expect(firstValueFrom(client.get('http://localhost/api/unknown'))).rejects.toMatchObject({
      status: 404,
    });
  });

  it('answers known routes with the fake response and errors', async () => {
    const client = setup();

    const login = await firstValueFrom(
      client.post<{ accessToken: string }>('http://localhost/api/auth/login', {
        email: 'admin@oficina.local',
        password: 'Admin@123',
      }),
    );
    expect(login.accessToken).toContain('fake-token.');
    await expect(firstValueFrom(client.get('http://localhost/api/auth/me'))).rejects.toMatchObject({
      status: 401,
    });
  });

  it('never answers the timeout scenario', () => {
    const client = setup();
    TestBed.inject(FakeBackend).setScenario('auth', 'timeout');
    let answered = false;

    client.get('http://localhost/api/auth/me').subscribe({
      next: () => (answered = true),
      error: () => (answered = true),
    });

    expect(answered).toBe(false);
  });
});
