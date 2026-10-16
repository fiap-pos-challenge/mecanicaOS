import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { useGatewayHarness } from '../../../../testing/gateway';
import { CustomerInsightsGateway } from '../data/customer-insights.gateway';
import { Customer } from '../data/customer.models';
import { CustomersGateway } from '../data/customers.gateway';
import { CustomerSummary } from '../data/insights.models';
import {
  CustomerListFilters,
  CustomersListStore,
  DEFAULT_FILTERS,
  matchesSummary,
  needsSummaryFilter,
  toQuery,
} from './customers-list.store';

const summary = (changes: Partial<CustomerSummary>): CustomerSummary => ({
  customerId: 'c',
  vehicleCount: 0,
  lastVisitAt: null,
  totalSpent: 0 as CustomerSummary['totalSpent'],
  averageTicket: 0 as CustomerSummary['averageTicket'],
  serviceOrderCount: 0,
  openServiceOrders: 0,
  situation: 'Active',
  fleet: false,
  ...changes,
});

describe('customer list filters', () => {
  const filters = (changes: Partial<CustomerListFilters>): CustomerListFilters => ({
    ...DEFAULT_FILTERS,
    ...changes,
  });

  it('knows when the summary is needed and how to match it', () => {
    expect(needsSummaryFilter(DEFAULT_FILTERS)).toBe(false);
    expect(needsSummaryFilter(filters({ fleet: true }))).toBe(true);
    expect(matchesSummary(filters({ fleet: true }), summary({ fleet: true }))).toBe(true);
    expect(matchesSummary(filters({ fleet: true }), summary({}))).toBe(false);
    expect(matchesSummary(filters({ noReturn: true }), summary({ situation: 'NoReturn' }))).toBe(
      true,
    );
    expect(matchesSummary(filters({ situation: 'OpenServiceOrder' }), summary({}))).toBe(false);
    expect(matchesSummary(DEFAULT_FILTERS, null)).toBe(false);
  });

  it('turns the filters into the API query', () => {
    expect(toQuery(filters({ term: 'ana', documentType: 'Cpf', sort: 'newest' }), 2, 20)).toEqual({
      term: 'ana',
      documentType: 'Cpf',
      page: 2,
      pageSize: 20,
      sort: 'CreatedAt',
      direction: 'Desc',
    });
    expect(toQuery(filters({ sort: 'oldest' }), 1, 5)).toMatchObject({
      sort: 'CreatedAt',
      direction: 'Asc',
    });
    expect(toQuery(filters({ sort: 'nameDesc' }), 1, 5)).toMatchObject({
      sort: 'Name',
      direction: 'Desc',
    });
  });
});

describe('CustomersListStore', () => {
  const harness = useGatewayHarness();

  async function setup(): Promise<InstanceType<typeof CustomersListStore>> {
    TestBed.configureTestingModule({
      providers: [...harness.providers('http'), CustomersListStore],
    });
    harness.spyNavigation();
    await harness.login();
    return TestBed.inject(CustomersListStore);
  }

  const names = (store: InstanceType<typeof CustomersListStore>): string[] =>
    store.rows().map((row) => row.customer.name);

  it('loads the first page with the summary of each customer', async () => {
    const store = await setup();

    await store.load();

    expect(names(store)).toEqual(['Carlos Mendes', 'Joana Prado', 'Transportes Rota Sul Ltda']);
    expect(store.totalItems()).toBe(3);
    expect(store.rows()[2]?.summary).toMatchObject({ vehicleCount: 3, fleet: true });
    expect(store.loading()).toBe(false);
    expect(store.hasFilters()).toBe(false);
  });

  it('searches, filters by type, sorts and clears', async () => {
    const store = await setup();

    await store.update({ term: 'prado' });
    expect(names(store)).toEqual(['Joana Prado']);
    await store.update({ term: '', documentType: 'Cnpj' });
    expect(names(store)).toEqual(['Transportes Rota Sul Ltda']);
    await store.update({ documentType: null, sort: 'nameDesc' });
    expect(names(store)[0]).toBe('Transportes Rota Sul Ltda');
    expect(store.hasFilters()).toBe(true);
    await store.clear();
    expect(store.filters()).toEqual(DEFAULT_FILTERS);
    expect(store.page()).toBe(1);
  });

  it('filters by fleet and by the summary situation', async () => {
    const store = await setup();

    await store.update({ fleet: true });
    expect(names(store)).toEqual(['Transportes Rota Sul Ltda']);
    expect(store.truncated()).toBe(false);

    await store.update({ fleet: false, situation: 'Active' });
    expect(store.rows().every((row) => row.summary?.situation === 'Active')).toBe(true);
    await store.update({ situation: null, noReturn: true });
    expect(store.rows().every((row) => row.summary?.situation === 'NoReturn')).toBe(true);
  });

  it('pages through the results', async () => {
    const store = await setup();

    await store.goTo(2);

    expect(store.page()).toBe(2);
    expect(store.rows()).toEqual([]);
  });

  it('warns when the summary filter looked only at the first customers', async () => {
    const store = await setup();
    const customer = { id: 'c1', name: 'Ana' } as Customer;
    vi.spyOn(TestBed.inject(CustomersGateway), 'query').mockImplementation((query) =>
      of({
        items: Array.from({ length: 100 }, (_, index) => ({
          ...customer,
          id: `c${query.page}-${index}`,
        })),
        page: query.page,
        pageSize: 100,
        totalItems: 900,
      }),
    );
    vi.spyOn(TestBed.inject(CustomerInsightsGateway), 'summaries').mockReturnValue(
      throwError(() => new Error('down')),
    );

    await store.update({ fleet: true });

    expect(store.truncated()).toBe(true);
    expect(store.rows()).toEqual([]);
  });

  it('keeps the error to show and ignores stale answers', async () => {
    const store = await setup();
    vi.spyOn(TestBed.inject(CustomersGateway), 'query').mockReturnValueOnce(
      throwError(() => new Error('offline')),
    );

    await store.load();

    expect(store.error()).toMatchObject({ code: 'http.unknown' });
    const first = store.update({ term: 'Joana' });
    const second = store.update({ term: 'Carlos' });
    await Promise.all([first, second]);
    expect(names(store)).toEqual(['Carlos Mendes']);
    expect(store.error()).toBeNull();
  });
});
