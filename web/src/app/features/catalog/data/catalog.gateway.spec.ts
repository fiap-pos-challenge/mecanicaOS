import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { fakeLineTotal, isValidEan13 } from '../../../../testing/fake-api/catalog/catalog.fake-api';
import { cents } from '../../../shared/util/money';
import { CatalogGateway, HttpCatalogGateway } from './catalog.gateway';
import { toCatalogPart, toCatalogService } from './catalog.mappers';
import { CatalogPart, CatalogQuery, PartInput } from './catalog.models';

const all: CatalogQuery = { page: 1, pageSize: 50, term: '', status: null };

const newPart: PartInput = {
  code: 'pec-9001',
  name: 'Correia dentada',
  type: 'Part',
  unitOfMeasure: 'un',
  unitPrice: cents(15_000),
  cost: cents(9_000),
  minimumStock: 2,
  supplier: 'Auto Peças Centro',
  barcode: '7891234567895',
};

describe('catalog mappers', () => {
  it('requires the identifiers and converts money to cents', () => {
    expect(() => toCatalogService({ name: 'X' })).toThrow(ContractError);
    expect(
      toCatalogPart({
        id: 'p',
        code: 'A',
        name: 'A',
        type: 'Part',
        unitOfMeasure: 'un',
        unitPrice: 1.5,
        isActive: true,
        createdAt: 'x',
      }),
    ).toMatchObject({ unitPrice: 150, cost: null, supplier: null, stockQuantity: 0 });
  });

  it('matches the fake line math and barcode rule', () => {
    expect(fakeLineTotal(19.99, 1.5)).toBe(2999);
    expect(fakeLineTotal(-1, 0.5)).toBe(-50);
    expect(isValidEan13('7891234567895')).toBe(true);
    expect(isValidEan13('123')).toBe(false);
  });
});

describe.each(API_MODES)('CatalogGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string): Promise<CatalogGateway> {
    harness.configure(mode);
    await harness.login(email);
    return TestBed.inject(CatalogGateway);
  }

  async function partNamed(gateway: CatalogGateway, name: string): Promise<CatalogPart> {
    const page = await firstValueFrom(gateway.list('parts', { ...all, term: name }));
    const part = page.items[0];
    if (!part) {
      throw new Error(`Part ${name} not found.`);
    }

    return part;
  }

  it('lists every tab with search and status filters', async () => {
    const gateway = await setup();

    expect(gateway).toBeInstanceOf(HttpCatalogGateway);
    const services = await firstValueFrom(gateway.list('services', all));
    expect(services.totalItems).toBe(8);
    expect(
      (await firstValueFrom(gateway.list('services', { ...all, status: 'Inactive' }))).items.map(
        (item) => item.name,
      ),
    ).toEqual(['Retífica de cabeçote']);
    expect(
      (await firstValueFrom(gateway.list('parts', { ...all, term: 'pec-04' }))).items.map(
        (item) => item.code,
      ),
    ).toEqual(['PEC-0415', 'PEC-0412']);
    const packages = await firstValueFrom(gateway.list('packages', all));
    const brakes = packages.items.find((item) => item.name === 'Kit freio dianteiro');
    expect(brakes).toMatchObject({ itemsTotal: 79_800, price: 74_000, savings: 5800 });
    expect(brakes?.items.map((item) => item.name)).toContain('Pastilha de freio dianteira');
    if (mode === 'http') {
      expect(harness.requests.some((request) => request.url.includes('status=Inactive'))).toBe(
        true,
      );
    }
  });

  it('creates and updates a part, recording the price history', async () => {
    const gateway = await setup();

    const part = await firstValueFrom(gateway.create('parts', newPart));
    expect(part).toMatchObject({ code: 'PEC-9001', cost: 9000, stockQuantity: 0 });

    const updated = await firstValueFrom(
      gateway.update('parts', part.id, { ...newPart, unitPrice: cents(16_500) }),
    );
    expect(updated.unitPrice).toBe(16_500);
    const history = await firstValueFrom(gateway.priceHistory('parts', part.id));
    expect(history[0]).toMatchObject({ field: 'UnitPrice', oldValue: 15_000, newValue: 16_500 });
    expect(history.map((entry) => entry.field)).toEqual(['UnitPrice', 'UnitPrice', 'Cost']);
    expect(history[0]?.changedBy).toBe('Administrador');
  });

  it('rejects a duplicated code and invalid fields', async () => {
    const gateway = await setup();

    await expect(
      firstValueFrom(gateway.create('parts', { ...newPart, code: 'PEC-0101' })),
    ).rejects.toMatchObject({ status: 409, code: 'inventory.duplicatePartCode' });
    await expect(
      firstValueFrom(
        gateway.create('parts', {
          ...newPart,
          code: 'PEC 1',
          name: 'X',
          unitOfMeasure: '',
          barcode: '123',
        }),
      ),
    ).rejects.toMatchObject({
      status: 400,
      fieldErrors: [
        expect.objectContaining({ field: 'name' }),
        expect.objectContaining({ field: 'code' }),
        expect.objectContaining({ field: 'unitOfMeasure' }),
        expect.objectContaining({ field: 'barcode' }),
      ],
    });
    const pads = await partNamed(gateway, 'Pastilha');
    await expect(
      firstValueFrom(gateway.update('parts', pads.id, { ...newPart, code: 'PEC-0101' })),
    ).rejects.toMatchObject({ status: 409 });
  });

  it('creates a service and a package with calculated savings', async () => {
    const gateway = await setup();
    const oil = await partNamed(gateway, 'Óleo 5W30');

    const service = await firstValueFrom(
      gateway.create('services', {
        name: 'Troca de fluido de freio',
        description: null,
        basePrice: cents(9000),
        estimatedMinutes: 40,
        commissionPercent: 10,
        warrantyDays: 90,
        usualParts: [{ partId: oil.id, quantity: 0.5 }],
      }),
    );
    expect(service.usualParts).toEqual([{ partId: oil.id, quantity: 0.5 }]);

    const pack = await firstValueFrom(
      gateway.create('packages', {
        name: 'Pacote freio',
        description: null,
        items: [
          { kind: 'Service', itemId: service.id, quantity: 1 },
          { kind: 'Part', itemId: oil.id, quantity: 1.5 },
        ],
        price: cents(15_000),
        suggestion: { minMileage: 10_000, maxMileage: 20_000 },
      }),
    );
    expect(pack).toMatchObject({ itemsTotal: 16_800, savings: 1800 });
    expect(pack.suggestion).toEqual({ minMileage: 10_000, maxMileage: 20_000 });
    const updated = await firstValueFrom(
      gateway.update('packages', pack.id, {
        name: 'Pacote freio',
        description: 'Fluido',
        items: [{ kind: 'Service', itemId: service.id, quantity: 1 }],
        price: cents(9_500),
        suggestion: null,
      }),
    );
    expect(updated).toMatchObject({ savings: -500, suggestion: null });
    expect(
      (
        await firstValueFrom(
          gateway.update('services', service.id, {
            name: 'Troca de fluido',
            description: null,
            basePrice: cents(9500),
            estimatedMinutes: 45,
            commissionPercent: 12,
            warrantyDays: 30,
            usualParts: [],
          }),
        )
      ).basePrice,
    ).toBe(9500);
    expect(
      (await firstValueFrom(gateway.priceHistory('packages', pack.id))).map(
        (entry) => entry.newValue,
      ),
    ).toEqual([9500, 15_000]);
  });

  it('validates services and packages', async () => {
    const gateway = await setup();

    await expect(
      firstValueFrom(
        gateway.create('services', {
          name: 'X',
          description: null,
          basePrice: cents(-1),
          estimatedMinutes: 0,
          commissionPercent: 60,
          warrantyDays: -1,
          usualParts: [{ partId: 'missing', quantity: 1 }],
        }),
      ),
    ).rejects.toMatchObject({ status: 400 });
    await expect(
      firstValueFrom(
        gateway.create('packages', {
          name: 'Vazio',
          description: null,
          items: [],
          price: cents(100),
          suggestion: { minMileage: 20, maxMileage: 10 },
        }),
      ),
    ).rejects.toMatchObject({
      status: 400,
      fieldErrors: [
        expect.objectContaining({ field: 'items' }),
        expect.objectContaining({ field: 'suggestion' }),
      ],
    });
  });

  it('blocks deactivating an item used by an active package', async () => {
    const gateway = await setup();
    const pads = await partNamed(gateway, 'Pastilha');

    await expect(firstValueFrom(gateway.setActive('parts', pads.id, false))).rejects.toMatchObject({
      status: 409,
      code: 'catalog.usedByActivePackage',
      detail: expect.stringContaining('Kit freio dianteiro'),
    });

    const brakes = (await firstValueFrom(gateway.list('packages', { ...all, term: 'freio' })))
      .items[0];
    await firstValueFrom(gateway.setActive('packages', brakes?.id ?? '', false));
    await firstValueFrom(gateway.setActive('parts', pads.id, false));
    await expect(firstValueFrom(gateway.setActive('parts', pads.id, false))).rejects.toMatchObject({
      status: 409,
      code: 'catalog.alreadyInactive',
    });
    await firstValueFrom(gateway.setActive('parts', pads.id, true));
    await expect(firstValueFrom(gateway.setActive('parts', pads.id, true))).rejects.toMatchObject({
      status: 409,
      code: 'catalog.alreadyActive',
    });
  });

  it('hides costs from the mechanic and forbids changes', async () => {
    const gateway = await setup('marcos@oficina.local');

    const pads = await partNamed(gateway, 'Pastilha');
    expect(pads.cost).toBeNull();
    const history = await firstValueFrom(gateway.priceHistory('parts', pads.id));
    expect(history.some((entry) => entry.field === 'Cost')).toBe(false);
    const calls: Observable<unknown>[] = [
      gateway.create('parts', newPart),
      gateway.update('parts', pads.id, newPart),
      gateway.setActive('parts', pads.id, false),
    ];
    for (const call of calls) {
      await expect(firstValueFrom(call)).rejects.toMatchObject({ status: 403 });
    }
  });

  it('answers 404 for unknown items', async () => {
    const gateway = await setup();

    const calls: Observable<unknown>[] = [
      gateway.get('services', 'missing'),
      gateway.update('parts', 'missing', newPart),
      gateway.setActive('packages', 'missing', false),
      gateway.setActive('packages', 'missing', true),
      gateway.priceHistory('services', 'missing'),
    ];
    for (const call of calls) {
      await expect(firstValueFrom(call)).rejects.toMatchObject({ status: 404 });
    }
  });

  it('reads one item of each kind', async () => {
    const gateway = await setup();
    const pads = await partNamed(gateway, 'Pastilha');

    expect((await firstValueFrom(gateway.get('parts', pads.id))).code).toBe('PEC-0412');
    const service = (await firstValueFrom(gateway.list('services', all))).items[0];
    expect((await firstValueFrom(gateway.get('services', service?.id ?? ''))).name).toBe(
      service?.name,
    );
  });

  it('sends each kind to its published API', async () => {
    const gateway = await setup();

    await firstValueFrom(gateway.list('services', all));
    await firstValueFrom(gateway.list('parts', all));
    await firstValueFrom(gateway.list('packages', all));

    if (mode === 'http') {
      for (const kind of ['services', 'parts', 'packages']) {
        expect(
          harness.requests.some((request) => request.url.includes(`/api/admin/${kind}?`)),
        ).toBe(true);
      }
      expect(harness.requests.some((request) => request.url.includes('includeInactive=true'))).toBe(
        true,
      );
    }
  });
});
