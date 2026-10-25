import { Routes } from '@angular/router';
import { provideDashboardData } from './data/dashboard.gateway';

export const DASHBOARD_CHILD_ROUTES: Routes = [
  {
    path: '',
    providers: [provideDashboardData()],
    loadComponent: () => import('./pages/dashboard-page').then((module) => module.DashboardPage),
  },
];
