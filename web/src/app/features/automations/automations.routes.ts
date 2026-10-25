import { Routes } from '@angular/router';
import { provideAutomationData } from './data/automation.gateway';
import { AutomationsStore } from './state/automations.store';

export const AUTOMATION_CHILD_ROUTES: Routes = [
  {
    path: '',
    providers: [provideAutomationData(), AutomationsStore],
    loadComponent: () =>
      import('./pages/automations-page').then((module) => module.AutomationsPage),
  },
];
