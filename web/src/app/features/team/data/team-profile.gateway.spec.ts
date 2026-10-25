import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { productivityOf } from '../../../../testing/fake-api/team/team.fake-api';
import { cents } from '../../../shared/util/money';
import {
  HttpTeamProfileGateway,
  TeamProfileGateway,
  toProductivity,
  toTeamProfile,
} from './team-profile.gateway';
import { UsersGateway } from './users.gateway';

describe('team profile mappers', () => {
  it('fills defaults and converts revenue to cents', () => {
    expect(toTeamProfile({ userId: 'u' })).toEqual({
      userId: 'u',
      specialties: [],
      commissionPercent: 0,
      discountLimitPercent: 0,
      schedule: [],
      updatedAt: null,
    });
    expect(toProductivity({ userId: 'u', revenue: 12.34 })).toMatchObject({
      revenue: cents(1234),
      weekly: [],
    });
    expect(() => toTeamProfile({ schedule: [] })).toThrow(ContractError);
  });

  it('builds deterministic productivity per period', () => {
    expect(productivityOf('abc', 30).weekly).toHaveLength(4);
    expect(productivityOf('abc', 90).weekly).toHaveLength(13);
    expect(productivityOf('abc', 30)).toEqual(productivityOf('abc', 30));
  });
});

describe.each(API_MODES)('TeamProfileGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string) {
    harness.configure(mode);
    await harness.login(email);
    const users = await firstValueFrom(
      TestBed.inject(UsersGateway).list({ page: 1, pageSize: 20 }),
    );
    const byEmail = (email: string): string =>
      users.items.find((user) => user.email === email)?.id ?? '';
    return { gateway: TestBed.inject(TeamProfileGateway), byEmail };
  }

  async function setupAsManager() {
    harness.configure(mode);
    await harness.login();
    const users = await firstValueFrom(
      TestBed.inject(UsersGateway).list({ page: 1, pageSize: 20 }),
    );
    const mechanic = users.items.find((user) => user.role === 'Mechanic')?.id ?? '';
    await harness.login('gestor@oficina.local');
    return { gateway: TestBed.inject(TeamProfileGateway), mechanic };
  }

  it('resolves the adapter from the environment', async () => {
    const { gateway } = await setup();

    expect(gateway).toBeInstanceOf(HttpTeamProfileGateway);
  });

  it('reads profiles in batch and one by one', async () => {
    const { gateway, byEmail } = await setup();
    const marcos = byEmail('marcos@oficina.local');

    const profiles = await firstValueFrom(gateway.profiles([marcos]));
    expect(profiles.get(marcos)?.specialties).toEqual(['Motor', 'Transmissão']);
    expect((await firstValueFrom(gateway.get(marcos))).schedule).toHaveLength(6);
    expect((await firstValueFrom(gateway.profiles([]))).size).toBe(0);
    expect((await firstValueFrom(gateway.get('real-api-user'))).specialties).toEqual([]);
  });

  it('saves the profile and enforces the rules', async () => {
    const { gateway, byEmail } = await setup();
    const marcos = byEmail('marcos@oficina.local');
    const gabi = byEmail('gestor@oficina.local');
    const input = {
      specialties: ['Motor'],
      commissionPercent: 12,
      discountLimitPercent: 0,
      schedule: [{ day: 'Monday' as const, start: '07:00', end: '16:00' }],
    };

    const saved = await firstValueFrom(gateway.update(marcos, input));
    expect(saved).toMatchObject({ commissionPercent: 12, specialties: ['Motor'] });
    expect(saved.updatedAt).not.toBeNull();

    await expect(
      firstValueFrom(gateway.update(marcos, { ...input, discountLimitPercent: 5 })),
    ).rejects.toMatchObject({ status: 422, code: 'team.mechanicDiscount' });
    expect(
      (await firstValueFrom(gateway.update(gabi, { ...input, discountLimitPercent: 20 })))
        .discountLimitPercent,
    ).toBe(20);
    await expect(
      firstValueFrom(
        gateway.update(marcos, {
          specialties: ['x'],
          commissionPercent: 80,
          discountLimitPercent: 40,
          schedule: [{ day: 'Monday', start: '18:00', end: '08:00' }],
        }),
      ),
    ).rejects.toMatchObject({
      status: 400,
      fieldErrors: [
        expect.objectContaining({ field: 'specialties' }),
        expect.objectContaining({ field: 'commissionPercent' }),
        expect.objectContaining({ field: 'discountLimitPercent' }),
        expect.objectContaining({ field: 'schedule' }),
      ],
    });
    expect((await firstValueFrom(gateway.update('real-api-user', input))).commissionPercent).toBe(
      12,
    );
  });

  it('reports productivity for the chosen period', async () => {
    const { gateway, byEmail } = await setup();
    const marcos = byEmail('marcos@oficina.local');

    const report = await firstValueFrom(gateway.productivity([marcos], 90));

    expect(report.periodDays).toBe(90);
    expect(report.items.get(marcos)?.weekly).toHaveLength(13);
    expect((await firstValueFrom(gateway.productivity([], 30))).items.size).toBe(0);
  });

  it('lets the manager read but not change profiles', async () => {
    const { gateway, mechanic } = await setupAsManager();

    expect((await firstValueFrom(gateway.get(mechanic))).userId).toBe(mechanic);
    await expect(
      firstValueFrom(
        gateway.update(mechanic, {
          specialties: [],
          commissionPercent: 0,
          discountLimitPercent: 0,
          schedule: [],
        }),
      ),
    ).rejects.toMatchObject({ status: 403 });
  });
});
