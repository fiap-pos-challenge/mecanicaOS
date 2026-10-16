import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { cents } from '../../../shared/util/money';
import { HttpInventoryGateway, InventoryGateway } from './inventory.gateway';
import { toStockItem, toStockKpis } from './inventory.mappers';
import { StockItem, StockQuery } from './inventory.models';

const all: StockQuery = { page: 1, pageSize: 50, term: '', situation: null, location: null };

describe('inventory mappers', () => {
  it('fills defaults and requires the identifiers', () => {
    expect(toStockKpis(undefined)).toMatchObject({ belowMinimum: 0, turnoverGoalDays: 30 });
    expect(() => toStockItem({ name: 'x' })).toThrow(ContractError);
  });
});

describe.each(API_MODES)('InventoryGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string): Promise<InventoryGateway> {
    harness.configure(mode);
    await harness.login(email);
    return TestBed.inject(InventoryGateway);
  }

  async function item(gateway: InventoryGateway, code: string): Promise<StockItem> {
    const found = (await firstValueFrom(gateway.overview({ ...all, term: code }))).items[0];
    if (!found) {
      throw new Error(`Item ${code} missing.`);
    }

    return found;
  }

  it('returns the overview with KPIs, filters and locations', async () => {
    const gateway = await setup();

    const overview = await firstValueFrom(gateway.overview(all));
    expect(gateway).toBeInstanceOf(HttpInventoryGateway);
    expect(overview.totalItems).toBe(7);
    expect(overview.kpis.belowMinimum).toBe(2);
    expect(overview.kpis.idleItems).toBe(1);
    expect(overview.locations).toEqual(['A3', 'B1', 'B2', 'C1']);
    expect(
      (await firstValueFrom(gateway.overview({ ...all, situation: 'OutOfStock' }))).items.map(
        (stock) => stock.code,
      ),
    ).toEqual(['PEC-0107']);
    expect(
      (await firstValueFrom(gateway.overview({ ...all, location: 'A3' }))).items.map(
        (stock) => stock.code,
      ),
    ).toEqual(['PEC-0412', 'PEC-0415']);
  });

  it('registers entries and exits and lists the movements', async () => {
    const gateway = await setup();
    const pads = await item(gateway, 'PEC-0412');

    const entry = await firstValueFrom(
      gateway.registerEntry({
        partId: pads.partId,
        quantity: 6,
        unitCost: cents(11_000),
        reason: '',
        supplierId: null,
        invoiceNumber: null,
      }),
    );
    expect(entry).toMatchObject({ type: 'In', quantity: 6, balanceAfter: 8 });
    await firstValueFrom(
      gateway.registerExit({
        partId: pads.partId,
        quantity: 3,
        reason: 'OS',
        serviceOrderNumber: 3100,
      }),
    );
    const movements = await firstValueFrom(gateway.movements(pads.partId));
    expect(movements.item.stockQuantity).toBe(5);
    expect(movements.movements[0]).toMatchObject({ type: 'Out', serviceOrderNumber: 3100 });
    expect(movements.movements).toHaveLength(4);
  });

  it('blocks a negative balance and fractional parts', async () => {
    const gateway = await setup();
    const pads = await item(gateway, 'PEC-0412');
    const coolant = await item(gateway, 'INS-0201');

    await expect(
      firstValueFrom(
        gateway.registerExit({
          partId: pads.partId,
          quantity: 3,
          reason: '',
          serviceOrderNumber: null,
        }),
      ),
    ).rejects.toMatchObject({ status: 422, code: 'inventory.insufficientStock' });
    await expect(
      firstValueFrom(
        gateway.registerEntry({
          partId: pads.partId,
          quantity: 1.5,
          unitCost: cents(100),
          reason: '',
          supplierId: null,
          invoiceNumber: null,
        }),
      ),
    ).rejects.toMatchObject({ status: 400 });
    expect(
      (
        await firstValueFrom(
          gateway.registerEntry({
            partId: coolant.partId,
            quantity: 1.5,
            unitCost: cents(1800),
            reason: '',
            supplierId: null,
            invoiceNumber: null,
          }),
        )
      ).balanceAfter,
    ).toBe(21.5);
  });

  it('finds items by barcode and explains unknown codes', async () => {
    const gateway = await setup();

    expect((await firstValueFrom(gateway.findByBarcode('7891234567895'))).code).toBe('PEC-0101');
    await expect(firstValueFrom(gateway.findByBarcode('0000000000000'))).rejects.toMatchObject({
      status: 404,
      code: 'inventory.unknownBarcode',
    });
  });

  it('reads an invoice draft and confirms it', async () => {
    const gateway = await setup();

    await expect(
      firstValueFrom(
        gateway.readInvoice({ fileName: 'nota.txt', contentType: 'text/plain', content: 'eA==' }),
      ),
    ).rejects.toMatchObject({ status: 400 });
    const draft = await firstValueFrom(
      gateway.readInvoice({ fileName: 'nota.xml', contentType: 'text/xml', content: 'eA==' }),
    );
    expect(draft.lines).toHaveLength(3);
    expect(draft.lines[2]?.partId).toBeNull();
    const coolant = await item(gateway, 'INS-0201');
    await firstValueFrom(
      gateway.confirmInvoice({
        supplierId: draft.supplierId,
        invoiceNumber: draft.invoiceNumber,
        lines: draft.lines.map((line) => ({
          partId: line.partId ?? coolant.partId,
          quantity: line.quantity,
          unitCost: line.unitCost,
        })),
      }),
    );
    expect((await item(gateway, 'PEC-0412')).stockQuantity).toBe(10);
    await expect(
      firstValueFrom(gateway.confirmInvoice({ supplierId: null, invoiceNumber: '1', lines: [] })),
    ).rejects.toMatchObject({ status: 400 });
  });

  it('runs a count through approval with one open count per location', async () => {
    const gateway = await setup();

    const count = await firstValueFrom(gateway.openCount('A3'));
    expect(count.lines.map((line) => line.code)).toEqual(['PEC-0412', 'PEC-0415']);
    await expect(firstValueFrom(gateway.openCount('A3'))).rejects.toMatchObject({
      status: 409,
      code: 'inventory.countAlreadyOpen',
    });
    await expect(firstValueFrom(gateway.openCount('ZZ'))).rejects.toMatchObject({ status: 400 });
    await expect(firstValueFrom(gateway.submitCount(count.id))).rejects.toMatchObject({
      status: 422,
      code: 'inventory.countIncomplete',
    });
    const [first, second] = count.lines;
    await expect(
      firstValueFrom(
        gateway.saveCount(count.id, [{ partId: first?.partId ?? '', countedQuantity: 1.5 }]),
      ),
    ).rejects.toMatchObject({ status: 400 });
    await firstValueFrom(
      gateway.saveCount(count.id, [
        { partId: first?.partId ?? '', countedQuantity: 1 },
        { partId: second?.partId ?? '', countedQuantity: 4 },
      ]),
    );
    expect((await firstValueFrom(gateway.submitCount(count.id))).status).toBe('PendingApproval');
    await expect(firstValueFrom(gateway.submitCount(count.id))).rejects.toMatchObject({
      status: 409,
    });
    await expect(
      firstValueFrom(
        gateway.saveCount(count.id, [{ partId: first?.partId ?? '', countedQuantity: 1 }]),
      ),
    ).rejects.toMatchObject({ status: 409 });
    await expect(firstValueFrom(gateway.rejectCount(count.id, ' '))).rejects.toMatchObject({
      status: 400,
    });
    expect((await firstValueFrom(gateway.approveCount(count.id))).status).toBe('Approved');
    await expect(firstValueFrom(gateway.approveCount(count.id))).rejects.toMatchObject({
      status: 409,
    });
    expect((await item(gateway, 'PEC-0412')).stockQuantity).toBe(1);
    expect((await firstValueFrom(gateway.counts()))[0]?.id).toBe(count.id);
    expect((await firstValueFrom(gateway.count(count.id))).decidedBy).toBe('Administrador');

    const again = await firstValueFrom(gateway.openCount('A3'));
    await firstValueFrom(
      gateway.saveCount(
        again.id,
        again.lines.map((line) => ({ partId: line.partId, countedQuantity: line.systemQuantity })),
      ),
    );
    await firstValueFrom(gateway.submitCount(again.id));
    expect((await firstValueFrom(gateway.rejectCount(again.id, 'Recontar'))).rejectionReason).toBe(
      'Recontar',
    );
    await expect(firstValueFrom(gateway.count('missing'))).rejects.toMatchObject({ status: 404 });
  });

  it('suggests purchases, creates drafts and queues them', async () => {
    const gateway = await setup();

    const suggestions = await firstValueFrom(gateway.suggestions());
    expect(suggestions.map((suggestion) => suggestion.code)).toEqual(['PEC-0412', 'PEC-0107']);
    const pads = suggestions[0];
    const order = await firstValueFrom(
      gateway.createPurchaseOrder({
        supplierId: pads?.supplierId ?? '',
        lines: [{ partId: pads?.partId ?? '', quantity: 20, unitCost: cents(11_200) }],
      }),
    );
    expect(order).toMatchObject({ number: 1001, status: 'Draft', total: 224_000 });
    expect((await firstValueFrom(gateway.sendPurchaseOrder(order.id))).status).toBe('Queued');
    await expect(firstValueFrom(gateway.sendPurchaseOrder(order.id))).rejects.toMatchObject({
      status: 409,
    });
    expect((await firstValueFrom(gateway.purchaseOrders()))[0]?.status).toBe('Queued');
    await expect(
      firstValueFrom(gateway.createPurchaseOrder({ supplierId: 'x', lines: [] })),
    ).rejects.toMatchObject({ status: 400 });
  });

  it('creates and updates suppliers with CNPJ validation', async () => {
    const gateway = await setup();
    const input = {
      name: 'Peças Norte',
      document: '11444777000161',
      email: 'compras@norte.example.com',
      phone: '1133334444',
      leadTimeDays: 4,
    };

    const created = await firstValueFrom(gateway.createSupplier(input));
    expect(created).toMatchObject({ name: 'Peças Norte', isActive: true });
    await expect(firstValueFrom(gateway.createSupplier(input))).rejects.toMatchObject({
      status: 409,
    });
    await expect(
      firstValueFrom(gateway.createSupplier({ ...input, document: '123', leadTimeDays: 900 })),
    ).rejects.toMatchObject({ status: 400 });
    expect(
      (await firstValueFrom(gateway.updateSupplier(created.id, { ...input, leadTimeDays: 7 })))
        .leadTimeDays,
    ).toBe(7);
    expect((await firstValueFrom(gateway.suppliers())).length).toBe(4);
    await expect(firstValueFrom(gateway.updateSupplier('missing', input))).rejects.toMatchObject({
      status: 404,
    });
  });

  it('lets the mechanic count but not move stock or buy', async () => {
    const gateway = await setup('marcos@oficina.local');
    const pads = await item(gateway, 'PEC-0412');

    expect((await firstValueFrom(gateway.openCount('A3'))).status).toBe('Open');
    const calls: Observable<unknown>[] = [
      gateway.registerEntry({
        partId: pads.partId,
        quantity: 1,
        unitCost: cents(1),
        reason: '',
        supplierId: null,
        invoiceNumber: null,
      }),
      gateway.suggestions(),
      gateway.purchaseOrders(),
    ];
    for (const call of calls) {
      await expect(firstValueFrom(call)).rejects.toMatchObject({ status: 403 });
    }
  });
});
