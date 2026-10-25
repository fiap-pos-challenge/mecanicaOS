import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideCustomersData } from '../data/customers.providers';
import { CustomersPage } from './customers-page';

const text = messages.customers.list;

describe('CustomersPage', () => {
  async function setup(role: BackendRole = 'Admin') {
    const view = await render(CustomersPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideCustomersData()],
      configureTestBed: () => {
        signInWithFakeApi(role);
      },
    });
    const table = await screen.findByRole('table', { name: text.caption });
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(4));
    return { view, table, user: userEvent.setup() };
  }

  const rowNames = (table: HTMLElement): string[] =>
    within(table)
      .queryAllByRole('link')
      .map((link) => link.textContent?.trim() ?? '');

  it('lists customers with masked document, vehicles, spending and situation', async () => {
    const { view, table } = await setup();

    expect(
      screen.getByRole('heading', { level: 1, name: messages.screens.customers }),
    ).toBeInTheDocument();
    expect(await screen.findByText(text.subtitle(3))).toBeInTheDocument();
    expect(rowNames(table)).toEqual(['Carlos Mendes', 'Joana Prado', 'Transportes Rota Sul Ltda']);
    expect(within(table).getByText('•••.982.247-••')).toBeInTheDocument();
    expect(within(table).getByText(text.vehicleCount(3))).toBeInTheDocument();
    expect(within(table).getAllByText(/R\$/).length).toBeGreaterThan(0);
    expect(within(table).getAllByText(text.fleet)).toHaveLength(1);
    expect(screen.getByRole('link', { name: new RegExp(text.newCustomer) })).toHaveAttribute(
      'href',
      '/customers/new',
    );
    await expectNoAxeViolations(view.container);
  });

  it('searches with a short delay and filters by type', async () => {
    const { table, user } = await setup();

    await user.type(screen.getByRole('searchbox'), 'joana');
    await vi.waitFor(() => expect(rowNames(table)).toEqual(['Joana Prado']));

    await user.clear(screen.getByRole('searchbox'));
    await vi.waitFor(() => expect(rowNames(table)).toHaveLength(3));
    await user.click(screen.getByRole('radio', { name: text.typeCompany }));
    await vi.waitFor(() => expect(rowNames(table)).toEqual(['Transportes Rota Sul Ltda']));
    await user.click(screen.getByRole('radio', { name: text.typeAll }));
    await vi.waitFor(() => expect(rowNames(table)).toHaveLength(3));
  });

  it('filters by fleet, situation and return, sorts and clears everything', async () => {
    const { table, user } = await setup();

    await user.click(screen.getByRole('button', { name: text.fleet }));
    await vi.waitFor(() => expect(rowNames(table)).toEqual(['Transportes Rota Sul Ltda']));
    await user.click(screen.getByRole('button', { name: text.clear }));
    await vi.waitFor(() => expect(rowNames(table)).toHaveLength(3));

    await user.selectOptions(screen.getByRole('combobox', { name: text.sort }), 'nameDesc');
    await vi.waitFor(() => expect(rowNames(table)[0]).toBe('Transportes Rota Sul Ltda'));
    await user.selectOptions(screen.getByRole('combobox', { name: text.situation }), 'Active');
    await vi.waitFor(() =>
      expect(
        within(table).queryAllByText(messages.customers.situations.OpenServiceOrder),
      ).toHaveLength(0),
    );
    await user.selectOptions(screen.getByRole('combobox', { name: text.situation }), '');
    await user.click(screen.getByRole('button', { name: text.noReturn }));
    await vi.waitFor(() =>
      expect(within(table).queryAllByText(messages.customers.situations.Active)).toHaveLength(0),
    );
  });

  it('opens the import dialog', async () => {
    const { user } = await setup('Attendant');

    await user.click(screen.getByRole('button', { name: new RegExp(text.import) }));

    expect(
      await screen.findByRole('dialog', { name: messages.customers.import.title }),
    ).toBeInTheDocument();
  });

  it('shows the failure and tries again', async () => {
    const { table, user } = await setup();
    TestBed.inject(FakeBackend).setScenario('customers', 'serverError');

    await user.type(screen.getByRole('searchbox'), 'x');

    expect(await screen.findByText(text.loadError)).toBeInTheDocument();
    TestBed.inject(FakeBackend).setScenario('customers', 'none');
    await user.clear(screen.getByRole('searchbox'));
    await user.click(screen.getByRole('button', { name: text.retry }));
    await vi.waitFor(() => expect(rowNames(table)).toHaveLength(3));
  });
});
