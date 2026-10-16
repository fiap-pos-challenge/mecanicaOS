import { Injectable } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { testEnvironment } from '../../../testing/api';
import { FEATURE_PENDING_CODE } from '../http/api-error';
import { APP_ENVIRONMENT, AppEnvironment } from './app-environment';
import { pendingGateway, provideGateway } from './provide-gateway';

abstract class SampleGateway {
  abstract load(): Observable<string>;
}

@Injectable({ providedIn: 'root' })
class HttpSampleGateway extends SampleGateway {
  load(): Observable<string> {
    return new Observable((subscriber) => subscriber.next('http'));
  }
}

@Injectable({ providedIn: 'root' })
class PendingSampleGateway extends SampleGateway {
  load(): Observable<string> {
    return new Observable((subscriber) => subscriber.next('custom pending'));
  }
}

function resolve(environment: AppEnvironment, custom = false): SampleGateway {
  TestBed.configureTestingModule({
    providers: [
      { provide: APP_ENVIRONMENT, useValue: environment },
      provideGateway(SampleGateway, 'catalog', {
        http: HttpSampleGateway,
        ...(custom ? { pending: PendingSampleGateway } : {}),
      }),
    ],
  });
  return TestBed.inject(SampleGateway);
}

describe('provideGateway', () => {
  it('uses the http adapter when the feature has an API', async () => {
    expect(await firstValueFrom(resolve(testEnvironment('http')).load())).toBe('http');
  });

  it('answers every call with the pending error while the API does not exist', async () => {
    await expect(firstValueFrom(resolve(testEnvironment('pending')).load())).rejects.toMatchObject({
      status: 501,
      code: FEATURE_PENDING_CODE,
    });
  });

  it('uses a custom pending adapter when given', async () => {
    expect(await firstValueFrom(resolve(testEnvironment('pending'), true).load())).toBe(
      'custom pending',
    );
  });

  it('is not mistaken for a promise', () => {
    expect((pendingGateway<object>() as { then?: unknown }).then).toBeUndefined();
  });

  it('reads the default environment', () => {
    expect(TestBed.inject(APP_ENVIRONMENT).apiBaseUrl).toBe('/api');
  });
});
