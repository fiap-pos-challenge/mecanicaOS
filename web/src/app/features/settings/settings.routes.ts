import { Routes } from '@angular/router';
import { provideSettingsData } from './data/settings.gateway';
import { SettingsStore } from './state/settings.store';

export const SETTINGS_CHILD_ROUTES: Routes = [
  {
    path: '',
    providers: [provideSettingsData(), SettingsStore],
    loadComponent: () => import('./pages/settings-page').then((module) => module.SettingsPage),
  },
];
