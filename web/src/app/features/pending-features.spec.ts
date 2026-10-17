import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, testEnvironment } from '../../testing/api';
import { APP_ENVIRONMENT } from '../core/config/app-environment';
import { signInWithFakeApi } from '../../testing/auth';
import { FakeBackend } from '../../testing/fake-api/fake-backend.service';
import { messages } from '../core/i18n/messages.pt-BR';
import { provideAuthData } from './auth/data/auth.providers';
import { provideCatalogData } from './catalog/data/catalog.gateway';
import { CatalogListPage } from './catalog/pages/catalog-list-page';
import { PackageFormPage } from './catalog/pages/package-form-page';
import { PartFormPage } from './catalog/pages/part-form-page';
import { provideCustomersData } from './customers/data/customers.providers';
import { CustomerDetailPage } from './customers/pages/customer-detail-page';
import { CustomerFormPage } from './customers/pages/customer-form-page';
import { CustomersPage } from './customers/pages/customers-page';
import { provideTeamData } from './team/data/team.providers';
import { PermissionsPage } from './team/pages/permissions-page';
import { TeamMemberPage } from './team/pages/team-member-page';
import { TeamPage } from './team/pages/team-page';

const pending = messages.pending;
const providers = [
  ...provideTestApi('pending'),
  provideAuthData(),
  provideCustomersData(),
  provideTeamData(),
  provideCatalogData(),
];

const catalogPending = [
  ...providers,
  {
    provide: APP_ENVIRONMENT,
    useValue: {
      ...testEnvironment('pending'),
      features: { ...testEnvironment('pending').features, catalog: { api: 'pending' } },
    },
  },
];

async function notice(message: string): Promise<HTMLElement> {
  const element = await screen.findByText(message);
  expect(element.closest('[data-testid="pending-notice"]')).not.toBeNull();
  return element;
}

describe('screens whose back-end API is not published yet', () => {
  it('lists real customers and explains the missing summary', async () => {
    await render(CustomersPage, {
      providers,
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });

    expect(await screen.findByText('Joana Prado')).toBeInTheDocument();
    await notice(pending.customerSummary);
  });

  it('shows the real customer and vehicles with the history under construction', async () => {
    const view = await render(CustomerDetailPage, {
      providers,
      inputs: { id: '' },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });
    const id =
      TestBed.inject(FakeBackend).state.database.customers.find(
        (customer) => customer.name === 'Joana Prado',
      )?.id ?? '';
    view.fixture.componentRef.setInput('id', id);

    expect(await screen.findByRole('heading', { level: 2, name: 'Joana Prado' })).toBeVisible();
    await notice(pending.history);
    expect(screen.queryByText(pending.vehicles)).toBeNull();
    await notice(pending.customerSummary);
    await userEvent.click(
      screen.getByRole('button', { name: new RegExp(messages.customers.detail.addVehicle) }),
    );
    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByRole('heading', { level: 2 })).toBeVisible();
  });

  it('answers the zip code lookup with the construction message', async () => {
    const user = userEvent.setup();
    await render(CustomerFormPage, {
      providers,
      inputs: { id: null },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });

    await user.type(
      screen.getByLabelText(new RegExp(`^\\s*${messages.customers.form.zipCode}`), {
        selector: 'input',
      }),
      '01310100',
    );
    expect(await screen.findByText(messages.errors.featurePending)).toBeInTheDocument();
  });

  it('keeps the real team and explains the missing production', async () => {
    await render(TeamPage, {
      providers,
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });

    expect(await screen.findByText('Marcos Mecânico')).toBeInTheDocument();
    await notice(pending.production);
  });

  it('shows the member access and the professional profile under construction', async () => {
    const view = await render(TeamMemberPage, {
      providers,
      inputs: { id: 'loading' },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });
    const id =
      TestBed.inject(FakeBackend).state.database.users.find(
        (user) => user.email === 'marcos@oficina.local',
      )?.id ?? '';
    view.fixture.componentRef.setInput('id', id);

    expect(await screen.findByRole('heading', { level: 1, name: 'Marcos Mecânico' })).toBeVisible();
    expect((await screen.findAllByText(pending.production)).length).toBeGreaterThan(0);
    await userEvent.click(screen.getByRole('button', { name: messages.team.member.saveProfile }));
  });

  it('explains that the permissions come from a future API', async () => {
    await render(PermissionsPage, {
      providers,
      configureTestBed: () => {
        signInWithFakeApi('Manager');
      },
    });

    await notice(pending.roles);
  });

  it('shows an empty catalog list and keeps the forms with the construction message', async () => {
    const list = await render(CatalogListPage, {
      providers: catalogPending,
      inputs: { kind: 'parts' },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });
    await notice(pending.catalog);
    expect(screen.getByText(messages.catalog.list.empty)).toBeInTheDocument();
    list.fixture.destroy();
  });

  it('keeps the new part form and answers the save with the construction message', async () => {
    const user = userEvent.setup();
    await render(PartFormPage, {
      providers: catalogPending,
      inputs: { id: null },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });
    const field = (label: string): HTMLElement =>
      screen.getByLabelText(new RegExp(`^\\s*${label.replace(/[()$]/g, '\\$&')}(\\s*\\*)?\\s*$`), {
        selector: 'input',
      });

    await user.type(field(messages.catalog.form.parts.code), 'PEC-1');
    await user.type(field(messages.catalog.form.name), 'Correia');
    await user.type(field(messages.catalog.form.parts.cost), '10');
    await user.type(field(messages.catalog.form.parts.unitPrice), '20');
    await user.click(screen.getByRole('button', { name: messages.catalog.form.save }));

    expect(await screen.findByTestId('catalog-form-error')).toHaveTextContent(
      messages.errors.featurePending,
    );
  });

  it('opens the package form with the construction notice and an existing item as pending', async () => {
    const view = await render(PackageFormPage, {
      providers: catalogPending,
      inputs: { id: null },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });
    await notice(pending.catalog);
    expect(
      screen.getByRole('heading', { level: 1, name: messages.catalog.form.packages.newTitle }),
    ).toBeVisible();

    view.fixture.componentRef.setInput('id', 'any');
    await notice(pending.catalog);
  });
});
