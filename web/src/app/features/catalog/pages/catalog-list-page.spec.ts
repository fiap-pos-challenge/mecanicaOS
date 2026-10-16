import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { cents, formatBrl } from '../../../shared/util/money';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideCatalogData } from '../data/catalog.gateway';
import { CatalogKind } from '../data/catalog.models';
import { CatalogListPage, packageRow, partRow, serviceRow } from './catalog-list-page';

const text = messages.catalog.list;

describe('catalog rows', () => {
  const base = { id: 'x', isActive: true, createdAt: 'x', updatedAt: null };

  it('formats each kind', () => {
    expect(
      serviceRow({
        ...base,
        name: 'Retífica',
        description: null,
        basePrice: cents(180_000),
        estimatedMinutes: 480,
        commissionPercent: 8.5,
        warrantyDays: 90,
        usualParts: [],
      }).values,
    ).toEqual({ minutes: '8 h', basePrice: formatBrl(cents(180_000)), commission: '8,5%' });
    const pack = packageRow({
      ...base,
      name: 'Pacote',
      description: null,
      items: [],
      price: cents(1100),
      itemsTotal: cents(1000),
      savings: cents(-100),
      suggestion: null,
    });
    expect(pack.negativeSavings).toBe(true);
    expect(pack.values['savings']).toBe(`${formatBrl(cents(-100))} (-10%)`);
    const part = partRow({
      ...base,
      name: 'Vela',
      code: 'PEC-1',
      type: 'Part',
      unitOfMeasure: 'un',
      unitPrice: cents(4400),
      cost: null,
      stockQuantity: 1.5,
      minimumStock: 2,
      supplier: null,
      barcode: null,
    });
    expect(part.values).toMatchObject({ margin: '—', stock: '1,5 un / mín 2' });
    expect(part.belowMinimum).toBe(true);
    expect(messages.catalog.duration(45)).toBe('45 min');
    expect(messages.catalog.duration(90)).toBe('1 h 30 min');
  });
});

describe('CatalogListPage', () => {
  async function setup(kind: CatalogKind, role: BackendRole = 'Admin') {
    const view = await render(CatalogListPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideCatalogData()],
      inputs: { kind },
      configureTestBed: () => {
        signInWithFakeApi(role);
      },
    });
    const table = await screen.findByRole('table');
    await vi.waitFor(() => expect(within(table).getAllByRole('row').length).toBeGreaterThan(2));
    return { view, table, user: userEvent.setup() };
  }

  const names = (table: HTMLElement): string[] =>
    within(table)
      .queryAllByRole('link')
      .map((link) => link.textContent?.trim() ?? '');

  it('lists services and filters by search and status', async () => {
    const { view, table, user } = await setup('services');

    expect(names(table)).toContain('Troca de óleo');
    expect(screen.getByText(text.total(8))).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Novo serviço/ })).toHaveAttribute(
      'href',
      '/catalog/services/new',
    );
    await expectNoAxeViolations(view.container);

    await user.type(screen.getByRole('searchbox'), 'alinh');
    await vi.waitFor(() => expect(names(table)).toEqual(['Alinhamento 3D']));
    await user.clear(screen.getByRole('searchbox'));
    await user.selectOptions(screen.getByRole('combobox', { name: text.status }), 'Inactive');
    await vi.waitFor(() => expect(names(table)).toEqual(['Retífica de cabeçote']));
  });

  it('shows parts with margin and stock alerts', async () => {
    const { table } = await setup('parts');

    expect(within(table).getByRole('columnheader', { name: text.columns.margin })).toBeVisible();
    expect(within(table).getAllByText(text.belowMinimum)).toHaveLength(2);
    expect(within(table).getByText('40,7%')).toBeInTheDocument();
  });

  it('hides costs and actions from the mechanic', async () => {
    const { table } = await setup('parts', 'Mechanic');

    expect(within(table).queryByRole('columnheader', { name: text.columns.margin })).toBeNull();
    expect(screen.queryByRole('link', { name: /Nova peça/ })).toBeNull();
    expect(within(table).queryByRole('button', { name: /Desativar/ })).toBeNull();
  });

  it('shows packages with savings', async () => {
    const { table } = await setup('packages');

    expect(within(table).getByText('58,00 (7,3%)', { exact: false })).toBeInTheDocument();
  });

  it('explains why an item used by an active package cannot be deactivated', async () => {
    const { table, user } = await setup('parts');

    await user.click(
      within(table).getByRole('button', { name: text.deactivateOf('Pastilha de freio dianteira') }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: text.deactivate }),
    );
    const blocked = await screen.findByRole('dialog', { name: text.blockedTitle });
    expect(within(blocked).getByText(/Kit freio dianteiro/)).toBeInTheDocument();
    expect(within(blocked).queryByRole('button', { name: messages.ui.dialog.cancel })).toBeNull();
    await user.click(within(blocked).getByRole('button', { name: text.close }));
  });

  it('deactivates and reactivates a free item', async () => {
    const { table, user } = await setup('services');
    const row = (): HTMLElement =>
      within(table)
        .getByRole('link', { name: text.open('Alinhamento 3D') })
        .closest('tr') ?? table;

    await user.click(
      within(row()).getByRole('button', { name: text.deactivateOf('Alinhamento 3D') }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: text.deactivate }),
    );
    await vi.waitFor(() =>
      expect(within(row()).getByText(messages.catalog.statuses.Inactive)).toBeInTheDocument(),
    );
    await user.click(
      within(row()).getByRole('button', { name: text.reactivateOf('Alinhamento 3D') }),
    );
    await vi.waitFor(() =>
      expect(within(row()).getByText(messages.catalog.statuses.Active)).toBeInTheDocument(),
    );
  });

  it('keeps the item when the deactivation is cancelled or fails', async () => {
    const { table, user } = await setup('services');
    const backend = TestBed.inject(FakeBackend);

    await user.click(
      within(table).getByRole('button', { name: text.deactivateOf('Alinhamento 3D') }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', {
        name: messages.ui.dialog.cancel,
      }),
    );
    backend.setScenario('catalog', 'serverError');
    await user.click(
      within(table).getByRole('button', { name: text.deactivateOf('Alinhamento 3D') }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: text.deactivate }),
    );
    await vi.waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
    expect(
      backend.state.database.catalog.services.find((item) => item.name === 'Alinhamento 3D')
        ?.isActive,
    ).toBe(true);
  });

  it('shows the load error and retries', async () => {
    const view = await render(CatalogListPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideCatalogData()],
      inputs: { kind: 'services' },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
        TestBed.inject(FakeBackend).setScenario('catalog', 'serverError');
      },
    });

    expect(await screen.findByText(text.loadError)).toBeInTheDocument();
    TestBed.inject(FakeBackend).setScenario('catalog', 'none');
    await userEvent.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByText('Troca de óleo')).toBeInTheDocument();
    expect(view.container).toBeTruthy();
  });
});
