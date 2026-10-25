import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { ToastService } from '../../../shared/ui/toast/toast';
import { toCustomer, toQueryCustomersRequest } from './customer.mappers';
import { CustomerInput, CustomerQuery } from './customer.models';
import { CustomersGateway, HttpCustomersGateway } from './customers.gateway';

const newCustomer: CustomerInput = {
  name: 'Paula Reis',
  document: '390.533.447-05',
  email: 'Paula@Example.com',
  phone: '(11) 91234-5678',
  address: null,
  messagingConsent: { whatsApp: true, email: false },
};

const query = (changes: Partial<CustomerQuery> = {}): CustomerQuery => ({
  term: '',
  documentType: null,
  sort: 'Name',
  direction: 'Asc',
  page: 1,
  pageSize: 20,
  ...changes,
});

describe('customer mappers', () => {
  it('rejects responses without required fields', () => {
    expect(() => toCustomer({ id: '1', name: 'A' })).toThrow(ContractError);
  });

  it('defaults a missing consent to no consent and trims the query term', () => {
    expect(
      toCustomer({
        id: '1',
        name: 'A',
        document: '•••',
        documentType: 'Cpf',
        createdAt: '2026-10-05T12:00:00Z',
      }).messagingConsent,
    ).toEqual({ whatsApp: false, email: false, updatedAt: null });
    expect(toQueryCustomersRequest(query({ term: '  ', documentType: 'Cnpj' }))).toEqual({
      term: null,
      documentType: 'Cnpj',
      sort: 'Name',
      direction: 'Asc',
      page: 1,
      pageSize: 20,
    });
  });
});

describe.each(API_MODES)('CustomersGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string): Promise<CustomersGateway> {
    harness.configure(mode);
    await harness.login(email);
    return TestBed.inject(CustomersGateway);
  }

  it('resolves the adapter from the environment', async () => {
    const gateway = await setup();

    expect(gateway).toBeInstanceOf(HttpCustomersGateway);
  });

  it('lists customers with masked personal data and consent', async () => {
    const gateway = await setup();

    const page = await firstValueFrom(gateway.list({ page: 1, pageSize: 2 }));

    expect(page).toMatchObject({ page: 1, pageSize: 2, totalItems: 3 });
    expect(page.items.map((customer) => customer.name)).toEqual(['Carlos Mendes', 'Joana Prado']);
    expect(page.items[1]).toMatchObject({
      maskedDocument: '•••.982.247-••',
      maskedEmail: 'j•••@example.com',
      maskedPhone: '•••••••4321',
      messagingConsent: { whatsApp: true, email: true },
    });
    await expect(firstValueFrom(gateway.list({ page: 1, pageSize: 500 }))).rejects.toMatchObject({
      status: 400,
    });
  });

  it('queries by name, full document, type and order', async () => {
    const gateway = await setup();
    const names = async (changes: Partial<CustomerQuery>): Promise<string[]> =>
      (await firstValueFrom(gateway.query(query(changes)))).items.map((customer) => customer.name);

    expect(await names({})).toEqual(['Carlos Mendes', 'Joana Prado', 'Transportes Rota Sul Ltda']);
    expect(await names({ direction: 'Desc' })).toEqual([
      'Transportes Rota Sul Ltda',
      'Joana Prado',
      'Carlos Mendes',
    ]);
    expect(await names({ term: 'prado' })).toEqual(['Joana Prado']);
    expect(await names({ term: '111.444.777-35' })).toEqual(['Carlos Mendes']);
    expect(await names({ documentType: 'Cnpj' })).toEqual(['Transportes Rota Sul Ltda']);
    expect(await names({ sort: 'CreatedAt', pageSize: 1, page: 2 })).toHaveLength(1);
    await expect(firstValueFrom(gateway.query(query({ pageSize: 500 })))).rejects.toMatchObject({
      status: 400,
    });
    if (mode === 'http') {
      const body = harness.requests.find((request) => request.url.endsWith('/query'))?.body ?? '';
      expect(body).toContain('"sort":"Name"');
    }
  });

  it('reveals one field on demand and records who saw it', async () => {
    const gateway = await setup();
    const joana = (await firstValueFrom(gateway.query(query({ term: 'Joana' })))).items[0];
    const id = joana?.id ?? '';

    expect(await firstValueFrom(gateway.reveal(id, 'Document', 'View'))).toBe('52998224725');
    expect(await firstValueFrom(gateway.reveal(id, 'Phone', 'Edit'))).toBe('11987654321');
    expect(await firstValueFrom(gateway.reveal(id, 'Email', 'View'))).toBe('joana@example.com');
    expect(harness.state(mode).database.sensitiveAccess.map((access) => access.field)).toEqual([
      'Document',
      'Phone',
      'Email',
    ]);
    await expect(
      firstValueFrom(gateway.reveal('00000000-0000-4000-8000-ffffffffffff', 'Phone', 'View')),
    ).rejects.toMatchObject({ status: 404 });
  });

  it('searches by document with POST and keeps it out of the URL', async () => {
    const gateway = await setup();

    const customer = await firstValueFrom(gateway.findByDocument({ document: '529.982.247-25' }));

    expect(customer.name).toBe('Joana Prado');
    if (mode === 'http') {
      const search = harness.requests.find((request) => request.url.endsWith('/search'));
      expect(search?.method).toBe('POST');
      expect(search?.url).not.toContain('529');
    }

    await expect(
      firstValueFrom(gateway.findByDocument({ document: '390.533.447-05' })),
    ).rejects.toMatchObject({ status: 404 });
    await expect(firstValueFrom(gateway.findByDocument({ document: '' }))).rejects.toMatchObject({
      status: 400,
    });
  });

  it('creates with consent, reads, updates and removes a customer', async () => {
    const gateway = await setup();

    const created = await firstValueFrom(
      gateway.create({
        ...newCustomer,
        address: {
          zipCode: '01001000',
          street: 'Praça da Sé',
          number: '1',
          complement: null,
          district: 'Sé',
          city: 'São Paulo',
          state: 'SP',
        },
      }),
    );
    expect(created).toMatchObject({
      maskedDocument: '•••.533.447-••',
      maskedEmail: 'p•••@example.com',
      messagingConsent: { whatsApp: true, email: false },
    });
    expect(created.messagingConsent.updatedAt).not.toBeNull();
    expect((await firstValueFrom(gateway.get(created.id))).name).toBe('Paula Reis');

    const updated = await firstValueFrom(
      gateway.update(created.id, {
        name: 'Paula R.',
        email: 'paula@example.com',
        phone: '11912345678',
        address: null,
        messagingConsent: { whatsApp: false, email: true },
      }),
    );
    expect(updated).toMatchObject({ name: 'Paula R.', address: null });
    expect(updated.messagingConsent).toMatchObject({ whatsApp: false, email: true });

    await firstValueFrom(gateway.remove(created.id), { defaultValue: undefined });
    await expect(firstValueFrom(gateway.get(created.id))).rejects.toMatchObject({ status: 404 });
  });

  it('refuses email consent without email and invalid data', async () => {
    const gateway = await setup();

    await expect(
      firstValueFrom(
        gateway.create({
          ...newCustomer,
          email: null,
          messagingConsent: { whatsApp: false, email: true },
        }),
      ),
    ).rejects.toMatchObject({ status: 400, code: 'customer.emailConsentWithoutEmail' });
    await expect(
      firstValueFrom(
        gateway.create({ ...newCustomer, name: '', document: '123', email: 'x', phone: '1' }),
      ),
    ).rejects.toMatchObject({
      status: 400,
      fieldErrors: [
        expect.objectContaining({ field: 'name' }),
        expect.objectContaining({ field: 'document' }),
        expect.objectContaining({ field: 'email' }),
        expect.objectContaining({ field: 'phone' }),
      ],
    });
    const joana = (await firstValueFrom(gateway.query(query({ term: 'Joana' })))).items[0];
    await expect(
      firstValueFrom(
        gateway.update(joana?.id ?? '', {
          name: 'Joana',
          email: null,
          phone: '11987654321',
          address: null,
          messagingConsent: { whatsApp: true, email: true },
        }),
      ),
    ).rejects.toMatchObject({ code: 'customer.emailConsentWithoutEmail' });
    await expect(
      firstValueFrom(
        gateway.update(joana?.id ?? '', {
          name: '',
          email: null,
          phone: '1',
          address: null,
          messagingConsent: { whatsApp: false, email: false },
        }),
      ),
    ).rejects.toMatchObject({ status: 400 });
  });

  it('rejects a duplicated document with 409 and keeps unknown ids at 404', async () => {
    const gateway = await setup();
    const missing = '00000000-0000-4000-8000-ffffffffffff';
    const update = {
      name: 'X',
      email: null,
      phone: '11987654321',
      address: null,
      messagingConsent: { whatsApp: false, email: false },
    };

    await expect(
      firstValueFrom(gateway.create({ ...newCustomer, document: '52998224725' })),
    ).rejects.toMatchObject({
      status: 409,
      code: 'customer.duplicateDocument',
      detail: messages.fake.documentInUse,
    });
    await expect(firstValueFrom(gateway.get(missing))).rejects.toMatchObject({ status: 404 });
    await expect(firstValueFrom(gateway.update(missing, update))).rejects.toMatchObject({
      status: 404,
    });
    await expect(firstValueFrom(gateway.remove(missing))).rejects.toMatchObject({ status: 404 });
  });

  it('blocks the removal of a customer with vehicles, as the domain policy does', async () => {
    const gateway = await setup();
    const fleet = (await firstValueFrom(gateway.query(query({ documentType: 'Cnpj' })))).items[0];

    await expect(firstValueFrom(gateway.remove(fleet?.id ?? ''))).rejects.toMatchObject({
      status: 409,
      code: 'customer.hasActiveVehicles',
    });
  });

  it('applies the profile rules of the API', async () => {
    const gateway = await setup('ana@oficina.local');
    const carlos = (await firstValueFrom(gateway.query(query({ term: 'Carlos' })))).items[0];

    await expect(firstValueFrom(gateway.remove(carlos?.id ?? ''))).rejects.toMatchObject({
      status: 403,
    });
    TestBed.resetTestingModule();
    const mechanic = await setup('marcos@oficina.local');
    await expect(firstValueFrom(mechanic.query(query()))).rejects.toMatchObject({ status: 403 });
  });

  it.each([
    ['forbidden', 403],
    ['conflict', 409],
    ['unprocessable', 422],
    ['tooManyRequests', 429],
    ['serverError', 500],
  ] as const)('simulates the %s scenario', async (scenario, status) => {
    const gateway = await setup();
    harness.scenario(mode, 'customers', scenario);

    await expect(firstValueFrom(gateway.create(newCustomer))).rejects.toMatchObject({ status });
  });

  it('sends the user to the access denied screen and toasts server errors', async () => {
    harness.configure(mode);
    await harness.login();
    const navigate = vi.mocked(TestBed.inject(Router).navigateByUrl);
    const gateway = TestBed.inject(CustomersGateway);

    harness.scenario(mode, 'customers', 'forbidden');
    await firstValueFrom(gateway.create(newCustomer)).catch(() => undefined);
    harness.scenario(mode, 'customers', 'serverError');
    await firstValueFrom(gateway.create(newCustomer)).catch(() => undefined);

    expect(navigate).toHaveBeenCalledWith('/forbidden');
    expect(TestBed.inject(ToastService).toasts()[0]).toMatchObject({ tone: 'error' });
  });
});
