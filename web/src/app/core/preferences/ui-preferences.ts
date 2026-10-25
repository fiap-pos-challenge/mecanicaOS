import { inject, Injectable, InjectionToken } from '@angular/core';

export interface PreferenceValues {
  sidebar: 'expanded' | 'collapsed';
  theme: 'light' | 'dark' | 'system';
}

export type PreferenceKey = keyof PreferenceValues;

export const PREFERENCE_STORAGE_KEYS: Readonly<Record<PreferenceKey, string>> = {
  sidebar: 'ui.sidebar',
  theme: 'ui.theme',
};

const allowedValues: { readonly [K in PreferenceKey]: readonly PreferenceValues[K][] } = {
  sidebar: ['expanded', 'collapsed'],
  theme: ['light', 'dark', 'system'],
};

export const PREFERENCE_STORAGE = new InjectionToken<Storage | null>('PREFERENCE_STORAGE', {
  providedIn: 'root',
  factory: () => {
    try {
      return globalThis.localStorage;
    } catch {
      return null;
    }
  },
});

@Injectable({ providedIn: 'root' })
export class UiPreferences {
  private readonly storage = inject(PREFERENCE_STORAGE);

  read<K extends PreferenceKey>(key: K): PreferenceValues[K] | null {
    const stored = this.safely(() => this.storage?.getItem(PREFERENCE_STORAGE_KEYS[key]) ?? null);
    const allowed: readonly string[] = allowedValues[key];
    return stored !== null && allowed.includes(stored) ? (stored as PreferenceValues[K]) : null;
  }

  write<K extends PreferenceKey>(key: K, value: PreferenceValues[K]): void {
    this.safely(() => this.storage?.setItem(PREFERENCE_STORAGE_KEYS[key], value));
  }

  private safely<T>(operation: () => T): T | null {
    try {
      return operation();
    } catch {
      return null;
    }
  }
}
