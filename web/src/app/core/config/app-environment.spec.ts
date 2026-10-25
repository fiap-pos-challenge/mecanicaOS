import { environment } from '../../../environments/environment';
import { environment as demoEnvironment } from '../../../environments/environment.demo';
import { environment as e2eEnvironment } from '../../../environments/environment.e2e';

describe('environments', () => {
  it('keeps the E2E build equal to production except for the payment provider origin', () => {
    expect({ ...e2eEnvironment, paymentProviderOrigins: [] }).toEqual(environment);
  });

  it('uses the real API for every feature in production and keeps the demo offline for live channels', () => {
    for (const settings of Object.values(environment.features)) {
      expect(settings.api).toBe('http');
    }

    for (const [name, settings] of Object.entries(demoEnvironment.features)) {
      expect(settings.api).toBe(
        ['realtime', 'session', 'twoFactor'].includes(name) ? 'pending' : 'http',
      );
    }
  });
});
