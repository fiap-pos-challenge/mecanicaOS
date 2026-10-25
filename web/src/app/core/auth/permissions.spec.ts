import { AppRole, hasPermission, homeFor, PERMISSIONS, ROLE_PERMISSIONS } from './permissions';

const roles: readonly AppRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic', 'Customer'];

describe('permissions', () => {
  it('gives the administrator every staff permission but none of the customer portal', () => {
    expect(ROLE_PERMISSIONS.Admin).toContain('platform.view');
    expect(ROLE_PERMISSIONS.Admin).toContain('customers.delete');
    expect(ROLE_PERMISSIONS.Admin).not.toContain('portal.view');
    expect(ROLE_PERMISSIONS.Admin).not.toContain('quotes.viewOwn');
  });

  it('keeps the manager out of platform, settings and user management', () => {
    for (const permission of [
      'platform.view',
      'settings.view',
      'team.manage',
      'customers.delete',
    ] as const) {
      expect(hasPermission('Manager', permission)).toBe(false);
    }
    expect(hasPermission('Manager', 'finance.view')).toBe(true);
    expect(hasPermission('Manager', 'catalog.viewCosts')).toBe(true);
  });

  it('limits the attendant to the front desk', () => {
    expect(hasPermission('Attendant', 'customers.manage')).toBe(true);
    expect(hasPermission('Attendant', 'customers.delete')).toBe(false);
    expect(hasPermission('Attendant', 'catalog.viewCosts')).toBe(false);
    expect(hasPermission('Attendant', 'finance.view')).toBe(false);
    expect(hasPermission('Attendant', 'team.view')).toBe(false);
  });

  it('shows the mechanic only what the bay needs, without costs or customer data', () => {
    expect(ROLE_PERMISSIONS.Mechanic).toEqual([
      'mechanic.view',
      'serviceOrders.view',
      'catalog.view',
      'inventory.view',
      'inventory.count',
    ]);
  });

  it('shows the customer only their own quotes, orders and the service catalog', () => {
    expect(ROLE_PERMISSIONS.Customer).toEqual([
      'portal.view',
      'quotes.viewOwn',
      'quotes.respondOwn',
      'serviceOrders.viewOwn',
      'catalog.viewServices',
    ]);
    expect(hasPermission('Customer', 'inventory.view')).toBe(false);
    expect(hasPermission('Customer', 'catalog.view')).toBe(false);
  });

  it('only uses declared permissions and denies everything without a role', () => {
    for (const role of roles) {
      for (const permission of ROLE_PERMISSIONS[role]) {
        expect(PERMISSIONS).toContain(permission);
      }
    }
    expect(hasPermission(null, 'dashboard.view')).toBe(false);
  });

  it('sends each profile to its own home', () => {
    expect(roles.map((role) => homeFor(role))).toEqual([
      '/dashboard',
      '/dashboard',
      '/dashboard',
      '/mechanic',
      '/portal',
    ]);
    expect(homeFor(null)).toBe('/login');
  });
});
