import { Permission } from '../core/auth/permissions';
import { messages } from '../core/i18n/messages.pt-BR';

export type NavGroup = keyof typeof messages.navGroups;

export type ScreenId = keyof typeof messages.screens;

export interface Screen {
  readonly id: Exclude<ScreenId, 'uiCatalog' | 'fakeApi'>;
  readonly path: string;
  readonly label: string;
  readonly group: NavGroup | null;
  readonly insideShell: boolean;
  readonly permission: Permission;
}

const label = messages.screens;

export const SCREENS: readonly Screen[] = [
  {
    id: 'dashboard',
    path: 'dashboard',
    label: label.dashboard,
    group: 'operation',
    insideShell: true,
    permission: 'dashboard.view',
  },
  {
    id: 'serviceOrders',
    path: 'service-orders',
    label: label.serviceOrders,
    group: 'operation',
    insideShell: true,
    permission: 'serviceOrders.view',
  },
  {
    id: 'quotes',
    path: 'quotes',
    label: label.quotes,
    group: 'operation',
    insideShell: true,
    permission: 'quotes.view',
  },
  {
    id: 'mechanic',
    path: 'mechanic',
    label: label.mechanic,
    group: 'operation',
    insideShell: true,
    permission: 'mechanic.view',
  },
  {
    id: 'customers',
    path: 'customers',
    label: label.customers,
    group: 'registry',
    insideShell: true,
    permission: 'customers.view',
  },
  {
    id: 'catalog',
    path: 'catalog',
    label: label.catalog,
    group: 'registry',
    insideShell: true,
    permission: 'catalog.view',
  },
  {
    id: 'team',
    path: 'team',
    label: label.team,
    group: 'registry',
    insideShell: true,
    permission: 'team.view',
  },
  {
    id: 'inventory',
    path: 'inventory',
    label: label.inventory,
    group: 'warehouse',
    insideShell: true,
    permission: 'inventory.view',
  },
  {
    id: 'reports',
    path: 'reports',
    label: label.reports,
    group: 'management',
    insideShell: true,
    permission: 'reports.view',
  },
  {
    id: 'finance',
    path: 'finance',
    label: label.finance,
    group: 'management',
    insideShell: true,
    permission: 'finance.view',
  },
  {
    id: 'documents',
    path: 'documents',
    label: label.documents,
    group: 'management',
    insideShell: true,
    permission: 'documents.view',
  },
  {
    id: 'automations',
    path: 'automations',
    label: label.automations,
    group: 'management',
    insideShell: true,
    permission: 'automations.view',
  },
  {
    id: 'portal',
    path: 'portal',
    label: label.portal,
    group: 'management',
    insideShell: false,
    permission: 'portal.view',
  },
  {
    id: 'settings',
    path: 'settings',
    label: label.settings,
    group: 'management',
    insideShell: true,
    permission: 'settings.view',
  },
  {
    id: 'platform',
    path: 'platform',
    label: label.platform,
    group: null,
    insideShell: true,
    permission: 'platform.view',
  },
];
