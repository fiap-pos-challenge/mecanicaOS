export type BackendRole = 'Admin' | 'Manager' | 'Attendant' | 'Mechanic';

export const BACKEND_ROLES: readonly BackendRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic'];

export type AppRole = BackendRole | 'Customer';

export const APP_ROLES: readonly AppRole[] = [...BACKEND_ROLES, 'Customer'];

export const PERMISSIONS = [
  'dashboard.view',
  'serviceOrders.view',
  'serviceOrders.manage',
  'serviceOrders.viewOwn',
  'quotes.view',
  'quotes.manage',
  'quotes.viewOwn',
  'quotes.respondOwn',
  'quotes.approveDiscount',
  'mechanic.view',
  'customers.view',
  'customers.manage',
  'customers.delete',
  'customers.reveal',
  'customers.import',
  'customers.export',
  'vehicles.manage',
  'catalog.view',
  'catalog.viewServices',
  'catalog.viewCosts',
  'catalog.manage',
  'team.view',
  'team.manage',
  'inventory.view',
  'inventory.manage',
  'inventory.count',
  'inventory.approve',
  'reports.view',
  'finance.view',
  'documents.view',
  'automations.view',
  'automations.reprocess',
  'portal.view',
  'settings.view',
  'settings.manage',
  'platform.view',
] as const;

export type Permission = (typeof PERMISSIONS)[number];

const staffOperation: readonly Permission[] = [
  'dashboard.view',
  'serviceOrders.view',
  'serviceOrders.manage',
  'quotes.view',
  'quotes.manage',
  'customers.view',
  'customers.manage',
  'customers.reveal',
  'customers.import',
  'vehicles.manage',
  'catalog.view',
  'inventory.view',
  'inventory.count',
  'documents.view',
];

export const ROLE_PERMISSIONS: Readonly<Record<AppRole, readonly Permission[]>> = {
  Admin: PERMISSIONS.filter(
    (permission) =>
      !['serviceOrders.viewOwn', 'quotes.viewOwn', 'quotes.respondOwn', 'portal.view'].includes(
        permission,
      ),
  ),
  Manager: [
    ...staffOperation,
    'mechanic.view',
    'customers.export',
    'catalog.viewCosts',
    'catalog.manage',
    'team.view',
    'inventory.manage',
    'inventory.approve',
    'quotes.approveDiscount',
    'reports.view',
    'finance.view',
    'automations.view',
  ],
  Attendant: staffOperation,
  Mechanic: [
    'mechanic.view',
    'serviceOrders.view',
    'catalog.view',
    'inventory.view',
    'inventory.count',
  ],
  Customer: [
    'portal.view',
    'quotes.viewOwn',
    'quotes.respondOwn',
    'serviceOrders.viewOwn',
    'catalog.viewServices',
  ],
};

export type PermissionModule = Permission extends `${infer Module}.${string}` ? Module : never;

export function moduleOf(permission: Permission): PermissionModule {
  const [module] = permission.split('.');
  return module as PermissionModule;
}

export function hasPermission(role: AppRole | null, permission: Permission): boolean {
  return role !== null && ROLE_PERMISSIONS[role].includes(permission);
}

export const HOME_BY_ROLE: Readonly<Record<AppRole, string>> = {
  Admin: '/dashboard',
  Manager: '/dashboard',
  Attendant: '/dashboard',
  Mechanic: '/mechanic',
  Customer: '/portal',
};

export function homeFor(role: AppRole | null): string {
  return role === null ? '/login' : HOME_BY_ROLE[role];
}
