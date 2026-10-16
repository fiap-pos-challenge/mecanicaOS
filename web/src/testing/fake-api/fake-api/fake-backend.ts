import { FakeBackendHandler, FakeRequest, FakeResult } from '../fake-request';
import { createFakeDatabase, FakeDatabase } from './fake-database';
import { FakeRouter } from './fake-http';
import { FakeFeature, FakeScenarios } from './fake-scenarios';

export class FakeFeatureApi implements FakeBackendHandler {
  constructor(
    readonly feature: FakeFeature,
    private readonly router: FakeRouter,
    private readonly scenarios: FakeScenarios,
  ) {}

  handles(path: string, method?: string): boolean {
    return this.router.handles(path, method);
  }

  handle(request: FakeRequest): FakeResult | null {
    if (!this.router.handles(request.path, request.method)) {
      return null;
    }

    return this.scenarios.resultFor(this.feature) ?? this.router.dispatch(request);
  }
}

export interface FakeBackendState {
  readonly database: FakeDatabase;
  readonly scenarios: FakeScenarios;
}

export function createFakeBackendState(): FakeBackendState {
  return { database: createFakeDatabase(), scenarios: new FakeScenarios() };
}

export function resetFakeBackendState(state: FakeBackendState): void {
  Object.assign(state.database, createFakeDatabase());
  state.scenarios.reset();
}
