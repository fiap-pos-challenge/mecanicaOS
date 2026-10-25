import type { HttpHandler } from 'msw';
import { FakeFeatureApi } from '../fake-api/fake-backend';
import { MswOptions, toMswHandler } from '../msw-handlers';

export function rolesHandlers(api: FakeFeatureApi, options: MswOptions): readonly HttpHandler[] {
  return [toMswHandler(api, options)];
}
