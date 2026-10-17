import { inject } from '@angular/core';
import { FakeFeature } from '../fake-api/fake-api/fake-scenarios';
import { createFakeInterceptor } from '../fake-api/fake-api.interceptor';
import { FAKE_APIS } from '../fake-api/fake-backend.service';

export const REAL_API_FEATURES: readonly FakeFeature[] = ['auth', 'customers', 'users', 'vehicles'];

export function demoFeatures(all: readonly FakeFeature[]): readonly FakeFeature[] {
  return all.filter((feature) => !REAL_API_FEATURES.includes(feature));
}

export const demoApiInterceptor = createFakeInterceptor(() => {
  const apis = inject(FAKE_APIS);
  const features = demoFeatures(
    Object.keys(apis).filter((key): key is FakeFeature => key !== 'state' && key !== 'marker'),
  );
  return (path, method) =>
    features.map((feature) => apis[feature]).find((api) => api.handles(path, method)) ?? null;
}, true);
