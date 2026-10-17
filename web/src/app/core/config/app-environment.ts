import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

export type ApiMode = 'http' | 'pending';

export type FeatureName =
  | 'auth'
  | 'customers'
  | 'users'
  | 'realtime'
  | 'session'
  | 'twoFactor'
  | 'vehicles'
  | 'customerInsights'
  | 'addresses'
  | 'jobs'
  | 'teamProfiles'
  | 'roles'
  | 'catalog'
  | 'inventory'
  | 'quotes'
  | 'serviceOrders'
  | 'portal'
  | 'documents'
  | 'dashboard'
  | 'reports'
  | 'automations'
  | 'notifications'
  | 'assistant'
  | 'settings'
  | 'finance'
  | 'platform';

export interface FeatureSettings {
  readonly api: ApiMode;
}

export interface SessionSettings {
  readonly idleWarningMs: number;
  readonly idleLogoutMs: number;
  readonly versionCheckIntervalMs: number;
}

export interface AppEnvironment {
  readonly apiBaseUrl: string;
  readonly realtimeHubPath: string;
  readonly paymentProviderOrigins: readonly string[];
  readonly session: SessionSettings;
  readonly features: Readonly<Record<FeatureName, FeatureSettings>>;
}

export const APP_ENVIRONMENT = new InjectionToken<AppEnvironment>('APP_ENVIRONMENT', {
  providedIn: 'root',
  factory: () => environment,
});

export const API_ORIGIN = new InjectionToken<string>('API_ORIGIN', {
  providedIn: 'root',
  factory: () => '',
});
