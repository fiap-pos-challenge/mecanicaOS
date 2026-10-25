import { AbstractType, inject, Provider, Type } from '@angular/core';
import { throwError } from 'rxjs';
import { featurePendingError } from '../http/api-error';
import { APP_ENVIRONMENT, FeatureName } from './app-environment';

export interface GatewayAdapters<T> {
  readonly http: Type<T>;
  readonly pending?: Type<T>;
}

export function pendingGateway<T extends object>(): T {
  return new Proxy({} as T, {
    get: (_target, key) =>
      key === 'then' ? undefined : () => throwError(() => featurePendingError()),
  });
}

export function provideGateway<T extends object>(
  port: AbstractType<T>,
  feature: FeatureName,
  adapters: GatewayAdapters<T>,
): Provider {
  return {
    provide: port,
    useFactory: () => {
      if (inject(APP_ENVIRONMENT).features[feature].api === 'http') {
        return inject(adapters.http);
      }

      return adapters.pending ? inject(adapters.pending) : pendingGateway<T>();
    },
  };
}
