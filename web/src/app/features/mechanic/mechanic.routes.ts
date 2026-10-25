import { Routes } from '@angular/router';
import { provideMechanicData } from './data/mechanic.gateway';
import { MechanicStore } from './state/mechanic.store';

export const MECHANIC_CHILD_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMechanicData(), MechanicStore],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./pages/mechanic-page').then((module) => module.MechanicPage),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./pages/mechanic-order-page').then((module) => module.MechanicOrderPage),
      },
    ],
  },
];
