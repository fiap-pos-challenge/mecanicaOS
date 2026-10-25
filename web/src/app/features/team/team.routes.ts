import { Routes } from '@angular/router';
import { messages, pageTitle } from '../../core/i18n/messages.pt-BR';
import { permissionGuard } from '../auth/auth.guards';

const text = messages.team;

export const TEAM_ROUTES: Routes = [
  {
    path: 'team',
    title: pageTitle(messages.screens.team),
    canActivate: [permissionGuard('team.view')],
    loadComponent: () => import('./pages/team-page').then((module) => module.TeamPage),
  },
  {
    path: 'team/permissions',
    title: pageTitle(text.permissions.title),
    canActivate: [permissionGuard('team.view')],
    loadComponent: () =>
      import('./pages/permissions-page').then((module) => module.PermissionsPage),
  },
  {
    path: 'team/:id',
    title: pageTitle(text.member.screen),
    canActivate: [permissionGuard('team.view')],
    loadComponent: () => import('./pages/team-member-page').then((module) => module.TeamMemberPage),
  },
];
