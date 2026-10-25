import type { HttpHandler } from 'msw';
import { FakeFeatureApi } from '../fake-api/fake-backend';
import { MswOptions, toMswHandler } from '../msw-handlers';

export function serviceOrdersHandlers(
  api: FakeFeatureApi,
  options: MswOptions,
): readonly HttpHandler[] {
  return [toMswHandler(api, options)];
}
