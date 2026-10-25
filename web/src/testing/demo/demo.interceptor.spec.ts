import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { provideSessionData } from '../../app/core/auth/session.gateway';
import { API_ORIGIN, APP_ENVIRONMENT } from '../../app/core/config/app-environment';
import { provideApi } from '../../app/core/http/provide-api';
import { provideAuthData } from '../../app/features/auth/data/auth.providers';
import { TEST_ORIGIN, testEnvironment } from '../api';
import { signInWithFakeApi } from '../auth';
import { FAKE_FEATURES } from '../fake-api/fake-api/fake-scenarios';
import { demoApiInterceptor, demoFeatures, REAL_API_FEATURES } from './demo.interceptor';
import { DEMO_INTERCEPTORS, DEMO_MODE } from './demo-mode';

describe('demo mode', () => {
  it('keeps login, customers, users and vehicles on the real API', () => {
    expect(DEMO_MODE).toBe(true);
    expect(DEMO_INTERCEPTORS).toEqual([demoApiInterceptor]);
    expect(demoFeatures(FAKE_FEATURES)).not.toContain('customers');
    expect(demoFeatures(FAKE_FEATURES)).not.toContain('vehicles');
    expect(REAL_API_FEATURES).toEqual(['auth', 'customers', 'users', 'vehicles']);
  });

  it('answers the pending features in the browser and lets the real ones through', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideApi([demoApiInterceptor]),
        provideSessionData(),
        provideAuthData(),
        { provide: API_ORIGIN, useValue: TEST_ORIGIN },
        { provide: APP_ENVIRONMENT, useValue: testEnvironment('http') },
      ],
    });
    signInWithFakeApi('Admin');
    const http = TestBed.inject(HttpClient);
    const quotes = await firstValueFrom(
      http.get<{ items: unknown[] }>(`${TEST_ORIGIN}/api/admin/quotes?page=1&pageSize=5`),
    );
    expect(quotes.items.length).toBeGreaterThan(0);
    await expect(
      firstValueFrom(http.get(`${TEST_ORIGIN}/api/admin/customers?page=1&pageSize=5`)),
    ).rejects.toBeTruthy();
  });
});
