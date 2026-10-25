import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { toUser } from './user.mappers';
import { HttpUsersGateway, UsersGateway } from './users.gateway';

describe('user mappers', () => {
  it('rejects responses without required fields', () => {
    expect(() => toUser({ id: '1', name: 'A', email: 'a@b.c', role: 'Admin' })).toThrow(
      ContractError,
    );
  });

  it('derives the status of old responses from isActive', () => {
    const base = { id: '1', name: 'A', email: 'a@b.c', role: 'Admin' as const, createdAt: 'x' };

    expect(toUser({ ...base, isActive: false }).status).toBe('Inactive');
    expect(toUser({ ...base, isActive: true }).status).toBe('Active');
    expect(toUser({ ...base, isActive: true, status: 'InvitationPending' })).toMatchObject({
      status: 'InvitationPending',
      invitationExpiresAt: null,
    });
  });
});

describe.each(API_MODES)('UsersGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string): Promise<UsersGateway> {
    harness.configure(mode);
    await harness.login(email);
    return TestBed.inject(UsersGateway);
  }

  async function idOf(gateway: UsersGateway, email: string): Promise<string> {
    return (await firstValueFrom(gateway.findByEmail({ email }))).id;
  }

  it('resolves the adapter from the environment', async () => {
    const gateway = await setup();

    expect(gateway).toBeInstanceOf(HttpUsersGateway);
  });

  it('lists users by name with status and without passwords', async () => {
    const gateway = await setup();

    const page = await firstValueFrom(gateway.list({ page: 1, pageSize: 20 }));

    expect(page.totalItems).toBe(7);
    expect(page.items[0]?.name).toBe('Administrador');
    expect(page.items[0]).not.toHaveProperty('password');
    expect(page.items.find((user) => user.email === 'rita@oficina.local')?.status).toBe('Inactive');
    const pending = page.items.find((user) => user.email === 'pedro@oficina.local');
    expect(pending?.status).toBe('InvitationPending');
    expect(pending?.invitationExpiresAt).not.toBeNull();
  });

  it('filters by role and status in the query string', async () => {
    const gateway = await setup();

    const mechanics = await firstValueFrom(
      gateway.list({ page: 1, pageSize: 20, role: 'Mechanic', status: 'Active' }),
    );

    expect(mechanics.items.map((user) => user.email)).toEqual(['marcos@oficina.local']);
    if (mode === 'http') {
      expect(harness.requests.at(-1)?.url).toContain('role=Mechanic&status=Active');
    }
  });

  it('finds users by e-mail through POST', async () => {
    const gateway = await setup();

    expect((await firstValueFrom(gateway.findByEmail({ email: 'ANA@oficina.local' }))).role).toBe(
      'Attendant',
    );
    await expect(
      firstValueFrom(gateway.findByEmail({ email: 'none@oficina.local' })),
    ).rejects.toMatchObject({ status: 404 });
    await expect(firstValueFrom(gateway.findByEmail({ email: '' }))).rejects.toMatchObject({
      status: 400,
    });
    if (mode === 'http') {
      expect(harness.requests.filter((request) => request.url.includes('oficina.local'))).toEqual(
        [],
      );
    }
  });

  it('invites, resends, updates, deactivates, reactivates and removes a user', async () => {
    const gateway = await setup();

    const user = await firstValueFrom(
      gateway.invite({ name: 'Bruno', email: 'Bruno@Oficina.local', role: 'Mechanic' }),
    );
    expect(user).toMatchObject({ email: 'bruno@oficina.local', status: 'InvitationPending' });
    expect((await firstValueFrom(gateway.resendInvitation(user.id))).status).toBe(
      'InvitationPending',
    );

    expect(
      (await firstValueFrom(gateway.update(user.id, { name: 'Bruno S.', role: 'Manager' }))).role,
    ).toBe('Manager');
    await firstValueFrom(gateway.deactivate(user.id));
    expect((await firstValueFrom(gateway.get(user.id))).status).toBe('Inactive');
    await expect(firstValueFrom(gateway.deactivate(user.id))).rejects.toMatchObject({
      status: 409,
      code: 'identity.alreadyInactive',
    });
    await firstValueFrom(gateway.reactivate(user.id));
    await expect(firstValueFrom(gateway.reactivate(user.id))).rejects.toMatchObject({
      status: 409,
      code: 'identity.alreadyActive',
    });
    await firstValueFrom(gateway.remove(user.id));
    await expect(firstValueFrom(gateway.get(user.id))).rejects.toMatchObject({ status: 404 });
  });

  it('validates the invitation and rejects a duplicated e-mail', async () => {
    const gateway = await setup();

    await expect(
      firstValueFrom(gateway.invite({ name: '', email: 'x', role: 'Mechanic' })),
    ).rejects.toMatchObject({
      status: 400,
      fieldErrors: [
        expect.objectContaining({ field: 'name' }),
        expect.objectContaining({ field: 'email' }),
      ],
    });
    await expect(
      firstValueFrom(
        gateway.invite({ name: 'Ana', email: 'ana@oficina.local', role: 'Attendant' }),
      ),
    ).rejects.toMatchObject({ status: 409, code: 'identity.duplicateEmail' });
    const id = await idOf(gateway, 'ana@oficina.local');
    await expect(
      firstValueFrom(gateway.update(id, { name: '', role: 'Attendant' })),
    ).rejects.toMatchObject({ status: 400 });
    await expect(firstValueFrom(gateway.resendInvitation(id))).rejects.toMatchObject({
      status: 409,
      code: 'identity.invitationNotPending',
    });
  });

  it('blocks the admin from removing their own access', async () => {
    const gateway = await setup();
    const adminId = await idOf(gateway, 'admin@oficina.local');

    const calls: Observable<unknown>[] = [
      gateway.update(adminId, { name: 'Adm', role: 'Mechanic' }),
      gateway.remove(adminId),
      gateway.deactivate(adminId),
    ];
    for (const call of calls) {
      await expect(firstValueFrom(call)).rejects.toMatchObject({
        status: 422,
        code: 'identity.cannotManageOwnAccount',
      });
    }

    expect(
      (await firstValueFrom(gateway.update(adminId, { name: 'Adm', role: 'Admin' }))).name,
    ).toBe('Adm');
  });

  it('answers 404 for unknown ids', async () => {
    const gateway = await setup();
    const missing = '00000000-0000-4000-8000-ffffffffffff';

    const calls: Observable<unknown>[] = [
      gateway.get(missing),
      gateway.update(missing, { name: 'X', role: 'Admin' }),
      gateway.remove(missing),
      gateway.deactivate(missing),
      gateway.reactivate(missing),
      gateway.resendInvitation(missing),
    ];
    for (const call of calls) {
      await expect(firstValueFrom(call)).rejects.toMatchObject({ status: 404 });
    }
  });

  it('lets the manager read but not change the team', async () => {
    const gateway = await setup('gestor@oficina.local');
    const id = await firstValueFrom(gateway.list({ page: 1, pageSize: 1 })).then(
      (page) => page.items[0]?.id ?? '',
    );

    expect((await firstValueFrom(gateway.get(id))).id).toBe(id);
    const calls: Observable<unknown>[] = [
      gateway.findByEmail({ email: 'ana@oficina.local' }),
      gateway.invite({ name: 'Xavier', email: 'x@x.io', role: 'Admin' }),
      gateway.update(id, { name: 'X', role: 'Admin' }),
      gateway.remove(id),
      gateway.deactivate(id),
      gateway.reactivate(id),
      gateway.resendInvitation(id),
    ];
    for (const call of calls) {
      await expect(firstValueFrom(call)).rejects.toMatchObject({ status: 403 });
    }
  });

  it('forbids the team to attendants', async () => {
    const gateway = await setup('ana@oficina.local');

    await expect(firstValueFrom(gateway.list({ page: 1, pageSize: 20 }))).rejects.toMatchObject({
      status: 403,
    });
  });

  it('rejects unknown filters', async () => {
    const gateway = await setup();

    await expect(
      firstValueFrom(
        gateway.list({ page: 1, pageSize: 20, status: 'Lost' as unknown as 'Active' }),
      ),
    ).rejects.toMatchObject({ status: 400 });
  });

  it('simulates the unprocessable scenario for every route', async () => {
    const gateway = await setup();
    harness.scenario(mode, 'users', 'unprocessable');

    await expect(firstValueFrom(gateway.deactivate('any'))).rejects.toMatchObject({
      status: 422,
      code: 'common.businessRule',
    });
  });
});
