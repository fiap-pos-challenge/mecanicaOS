import { Routes } from '@angular/router';
import { provideAssistantData } from './data/assistant.gateway';
import { provideReportData } from './data/report.gateway';
import { ReportsStore } from './state/reports.store';

export const REPORT_CHILD_ROUTES: Routes = [
  {
    path: '',
    providers: [provideReportData(), provideAssistantData(), ReportsStore],
    loadComponent: () => import('./pages/reports-page').then((module) => module.ReportsPage),
  },
];
