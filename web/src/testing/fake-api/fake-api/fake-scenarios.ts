import { FakeResult } from '../fake-request';
import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { problem } from './fake-http';

export const FAKE_SCENARIOS = [
  'none',
  'forbidden',
  'notFound',
  'conflict',
  'unprocessable',
  'tooManyRequests',
  'serverError',
  'timeout',
] as const;

export type FakeScenario = (typeof FAKE_SCENARIOS)[number];

export type FakeFeature =
  | 'auth'
  | 'customers'
  | 'users'
  | 'vehicles'
  | 'insights'
  | 'addresses'
  | 'jobs'
  | 'team'
  | 'roles'
  | 'catalog'
  | 'inventory'
  | 'quotes'
  | 'serviceOrders'
  | 'portal'
  | 'dashboard'
  | 'reports'
  | 'automations'
  | 'notifications'
  | 'assistant'
  | 'settings'
  | 'finance';

export const FAKE_FEATURES: readonly FakeFeature[] = [
  'auth',
  'customers',
  'users',
  'vehicles',
  'insights',
  'addresses',
  'jobs',
  'team',
  'roles',
  'catalog',
  'inventory',
  'quotes',
  'serviceOrders',
  'portal',
  'dashboard',
  'reports',
  'automations',
  'notifications',
  'assistant',
  'settings',
  'finance',
];

const text = messages.fake;

export function scenarioResult(scenario: FakeScenario): FakeResult | null {
  switch (scenario) {
    case 'none':
      return null;
    case 'forbidden':
      return problem(403, 'common.forbidden', text.forbidden);
    case 'notFound':
      return problem(404, 'common.notFound', messages.errors.notFound);
    case 'conflict':
      return problem(409, 'common.conflict', messages.errors.conflict);
    case 'unprocessable':
      return problem(422, 'common.businessRule', messages.errors.businessRule);
    case 'tooManyRequests':
      return problem(
        429,
        'common.tooManyRequests',
        text.tooManyRequests,
        [],
        { 'Retry-After': '1' },
        { retryAfterSeconds: 1 },
      );
    case 'serverError':
      return problem(500, 'common.unexpected', text.serverError);
    case 'timeout':
      return { timeout: true };
  }
}

export class FakeScenarios {
  private readonly active = new Map<FakeFeature, FakeScenario>();

  get(feature: FakeFeature): FakeScenario {
    return this.active.get(feature) ?? 'none';
  }

  set(feature: FakeFeature, scenario: FakeScenario): void {
    if (scenario === 'none') {
      this.active.delete(feature);
    } else {
      this.active.set(feature, scenario);
    }
  }

  reset(): void {
    this.active.clear();
  }

  resultFor(feature: FakeFeature): FakeResult | null {
    return scenarioResult(this.get(feature));
  }
}
