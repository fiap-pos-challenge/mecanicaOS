import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { VEHICLES_WITH_OPEN_ORDER } from '../../../../testing/fake-api/vehicles/vehicles.fake-api';
import { CustomersGateway } from './customers.gateway';
import { toVehicle } from './vehicle.mappers';
import { VehicleInput } from './vehicle.models';
import { HttpVehiclesGateway, VehiclesGateway } from './vehicles.gateway';

describe('vehicle mappers', () => {
  it('rejects responses without required fields', () => {
    expect(() => toVehicle({ id: '1', licensePlate: 'ABC1234' })).toThrow(ContractError);
  });
});

describe.each(API_MODES)('VehiclesGateway (%s)', (mode) => {
  const harness = useGatewayHarness();
  afterEach(() => VEHICLES_WITH_OPEN_ORDER.clear());

  async function setup(email?: string): Promise<{ gateway: VehiclesGateway; owners: string[] }> {
    harness.configure(mode);
    await harness.login(email);
    const page = await firstValueFrom(
      TestBed.inject(CustomersGateway).query({
        term: '',
        documentType: null,
        sort: 'Name',
        direction: 'Asc',
        page: 1,
        pageSize: 20,
      }),
    ).catch(() => ({ items: [] }));
    return { gateway: TestBed.inject(VehiclesGateway), owners: page.items.map((item) => item.id) };
  }

  const input = (customerId: string, changes: Partial<VehicleInput> = {}): VehicleInput => ({
    customerId,
    licensePlate: 'bra-2e19',
    brand: 'Chevrolet',
    model: 'Onix',
    year: 2023,
    mileage: 15_000,
    ...changes,
  });

  it('resolves the adapter and lists the vehicles of a customer', async () => {
    const { gateway, owners } = await setup();

    expect(gateway).toBeInstanceOf(HttpVehiclesGateway);
    const fleet = await firstValueFrom(gateway.listByCustomer(owners[2] ?? ''));
    expect(fleet.map((vehicle) => vehicle.licensePlate)).toEqual([
      'ABC-1234',
      'RSL1A23',
      'RSL2B34',
    ]);
    expect(await firstValueFrom(gateway.listByCustomer(owners[0] ?? ''))).toEqual([]);
  });

  it('registers Mercosul and old plates, finds by plate in the body, edits and removes', async () => {
    const { gateway, owners } = await setup();
    const owner = owners[0] ?? '';

    const mercosul = await firstValueFrom(gateway.create(input(owner)));
    const old = await firstValueFrom(gateway.create(input(owner, { licensePlate: 'xyz9876' })));
    expect([mercosul.licensePlate, old.licensePlate]).toEqual(['BRA2E19', 'XYZ-9876']);
    expect((await firstValueFrom(gateway.findByPlate('BRA-2E19'))).id).toBe(mercosul.id);
    if (mode === 'http') {
      const search = harness.requests.find((request) => request.url.endsWith('/vehicles/search'));
      expect(search?.url).not.toContain('BRA');
    }

    const edited = await firstValueFrom(
      gateway.update(mercosul.id, {
        brand: 'Chevrolet',
        model: 'Onix Plus',
        year: 2023,
        mileage: 16_000,
      }),
    );
    expect(edited).toMatchObject({ model: 'Onix Plus', mileage: 16_000 });
    await firstValueFrom(gateway.remove(old.id), { defaultValue: undefined });
    expect(
      (await firstValueFrom(gateway.listByCustomer(owner))).map((vehicle) => vehicle.id),
    ).toEqual([mercosul.id]);
  });

  it('rejects invalid data, duplicated plates and a lower mileage', async () => {
    const { gateway, owners } = await setup();
    const owner = owners[0] ?? '';

    await expect(
      firstValueFrom(
        gateway.create(
          input('x', { licensePlate: 'AB1', brand: '', model: '', year: 1800, mileage: -1 }),
        ),
      ),
    ).rejects.toMatchObject({
      status: 400,
      fieldErrors: ['customerId', 'licensePlate', 'brand', 'model', 'year', 'mileage'].map(
        (field) => expect.objectContaining({ field }),
      ),
    });
    await expect(
      firstValueFrom(gateway.create(input(owner, { licensePlate: 'RSL1A23' }))),
    ).rejects.toMatchObject({
      status: 409,
      code: 'vehicle.duplicatedLicensePlate',
    });
    const fleet = await firstValueFrom(gateway.listByCustomer(owners[2] ?? ''));
    await expect(
      firstValueFrom(
        gateway.update(fleet[0]?.id ?? '', {
          brand: 'Renault',
          model: 'Master',
          year: 2019,
          mileage: 10,
        }),
      ),
    ).rejects.toMatchObject({ status: 400, code: 'vehicle.mileageDecrease' });
    await expect(
      firstValueFrom(
        gateway.update(fleet[0]?.id ?? '', { brand: '', model: 'Master', year: 2019, mileage: 10 }),
      ),
    ).rejects.toMatchObject({ status: 400 });
  });

  it('transfers the owner and refuses the same owner', async () => {
    const { gateway, owners } = await setup();
    const fleet = await firstValueFrom(gateway.listByCustomer(owners[2] ?? ''));
    const vehicle = fleet[0];

    const moved = await firstValueFrom(gateway.transfer(vehicle?.id ?? '', owners[0] ?? ''));
    expect(moved.customerId).toBe(owners[0]);
    await expect(
      firstValueFrom(gateway.transfer(vehicle?.id ?? '', owners[0] ?? '')),
    ).rejects.toMatchObject({
      status: 422,
      code: 'vehicle.sameOwner',
    });
    await expect(
      firstValueFrom(gateway.transfer(vehicle?.id ?? '', 'nobody')),
    ).rejects.toMatchObject({
      status: 400,
    });
  });

  it('blocks removal with an open service order and answers 404 for unknown vehicles', async () => {
    const { gateway, owners } = await setup();
    const vehicle = (await firstValueFrom(gateway.listByCustomer(owners[1] ?? '')))[0];
    VEHICLES_WITH_OPEN_ORDER.add('FJK4E21');
    const missing = '00000000-0000-4000-8000-ffffffffffff';

    await expect(firstValueFrom(gateway.remove(vehicle?.id ?? ''))).rejects.toMatchObject({
      status: 409,
      code: 'vehicle.hasOpenServiceOrder',
    });
    const calls: Observable<unknown>[] = [
      gateway.remove(missing),
      gateway.update(missing, { brand: 'A', model: 'B', year: 2020, mileage: 1 }),
      gateway.transfer(missing, owners[0] ?? ''),
      gateway.findByPlate('ZZZ9999'),
    ];
    for (const call of calls) {
      await expect(firstValueFrom(call)).rejects.toMatchObject({ status: 404 });
    }
  });

  it('lets the mechanic read but not change vehicles', async () => {
    const { gateway } = await setup('marcos@oficina.local');

    expect(await firstValueFrom(gateway.findByPlate('FJK4E21'))).toMatchObject({
      brand: 'Volkswagen',
    });
    await expect(
      firstValueFrom(gateway.create(input('00000000-0000-4000-8000-000000000001'))),
    ).rejects.toMatchObject({ status: 403 });
  });
});
