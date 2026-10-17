import { environment } from '../../../environments/environment';
import { environment as demoEnvironment } from '../../../environments/environment.demo';
import { environment as e2eEnvironment } from '../../../environments/environment.e2e';

describe('environments', () => {
  it('keeps the E2E build equal to production except for the APIs the E2E intercepts', () => {
    expect(environment.features.quotes.api).toBe('pending');
    expect(e2eEnvironment.features.quotes.api).toBe('http');
    expect({
      ...e2eEnvironment,
      paymentProviderOrigins: [],
      features: {
        ...e2eEnvironment.features,
        quotes: { api: 'pending' },
        serviceOrders: { api: 'pending' },
        portal: { api: 'pending' },
        documents: { api: 'pending' },
        dashboard: { api: 'pending' },
        reports: { api: 'pending' },
        automations: { api: 'pending' },
        notifications: { api: 'pending' },
        assistant: { api: 'pending' },
        settings: { api: 'pending' },
      },
    }).toEqual(environment);
  });

  it('keeps the real API in production only for login, customers, users, vehicles and catalog', () => {
    const real = Object.entries(environment.features)
      .filter(([, settings]) => settings.api === 'http')
      .map(([name]) => name);
    expect(real).toEqual(['auth', 'customers', 'users', 'vehicles', 'catalog']);
    for (const [name, settings] of Object.entries(demoEnvironment.features)) {
      expect(settings.api).toBe(
        ['realtime', 'session', 'twoFactor'].includes(name) ? 'pending' : 'http',
      );
    }
  });
});
