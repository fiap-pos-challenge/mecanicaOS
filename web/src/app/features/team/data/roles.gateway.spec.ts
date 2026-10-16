import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ROLE_PERMISSIONS } from '../../../core/auth/permissions';
import { HttpRolesGateway, RolesGateway, toRoleDefinition } from './roles.gateway';

describe('role mappers', () => {
  it('drops unknown permissions', () => {
    const role = toRoleDefinition({ role: 'Mechanic', permissions: ['mechanic.view', 'x.y'] });

    expect([...role.permissions]).toEqual(['mechanic.view']);
    expect(role.editable).toBe(false);
  });
});

describe.each(API_MODES)('RolesGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string): Promise<RolesGateway> {
    harness.configure(mode);
    await harness.login(email);
    return TestBed.inject(RolesGateway);
  }

  it('lists the fixed permissions of every role', async () => {
    const gateway = await setup();

    const catalog = await firstValueFrom(gateway.list());

    expect(gateway).toBeInstanceOf(HttpRolesGateway);
    expect(catalog.customizable).toBe(false);
    expect(catalog.roles.map((role) => role.role)).toEqual([
      'Admin',
      'Manager',
      'Attendant',
      'Mechanic',
      'Customer',
    ]);
    expect(catalog.roles[3]?.permissions).toEqual(new Set(ROLE_PERMISSIONS.Mechanic));
  });

  it('answers 501 while custom roles are not supported', async () => {
    const gateway = await setup();

    await expect(
      firstValueFrom(gateway.updatePermissions('Mechanic', ['mechanic.view'])),
    ).rejects.toMatchObject({ status: 501, code: 'roles.customizationUnavailable' });
  });

  it('forbids attendants', async () => {
    const gateway = await setup('ana@oficina.local');

    await expect(firstValueFrom(gateway.list())).rejects.toMatchObject({ status: 403 });
  });
});
