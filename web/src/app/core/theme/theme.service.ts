import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';
import { PreferenceValues, UiPreferences } from '../preferences/ui-preferences';

export type ThemeMode = PreferenceValues['theme'];

const nextMode: Readonly<Record<ThemeMode, ThemeMode>> = {
  light: 'dark',
  dark: 'system',
  system: 'light',
};

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly preferences = inject(UiPreferences);
  private readonly root = inject(DOCUMENT).documentElement;

  readonly mode = signal<ThemeMode>(this.preferences.read('theme') ?? 'system');

  constructor() {
    effect(() => {
      const mode = this.mode();
      if (mode === 'system') {
        this.root.removeAttribute('data-theme');
      } else {
        this.root.setAttribute('data-theme', mode);
      }
    });
  }

  next(): ThemeMode {
    return nextMode[this.mode()];
  }

  set(mode: ThemeMode): void {
    this.mode.set(mode);
    this.preferences.write('theme', mode);
  }

  cycle(): void {
    this.set(this.next());
  }
}
