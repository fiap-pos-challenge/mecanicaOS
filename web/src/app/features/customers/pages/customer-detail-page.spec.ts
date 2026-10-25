import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { ToastService } from '../../../shared/ui/toast/toast';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideCustomersData } from '../data/customers.providers';
import { CustomerDetailPage } from './customer-detail-page';

const text = messages.customers.detail;
const vehicleText = messages.customers.vehicle;

describe('CustomerDetailPage', () => {
  afterEach(() => vi.useRealTimers());

  async function setup(customerName: string, role: BackendRole = 'Admin') {
    let id = '';
    const view = await render(CustomerDetailPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideCustomersData()],
      inputs: { id: '' },
      configureTestBed: () => {
        signInWithFakeApi(role);
        id =
          TestBed.inject(FakeBackend).state.database.customers.find(
            (customer) => customer.name === customerName,
          )?.id ?? 'missing';
      },
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    view.fixture.componentRef.setInput('id', id);
    view.fixture.detectChanges();
    await screen.findByRole('heading', {
      level: 2,
      name: customerName === 'none' ? text.notFound : customerName,
    });
    return { view, id, navigate, navigateByUrl, user: userEvent.setup() };
  }

  it('shows the masked data, the summary, the vehicles and the history', async () => {
    const { view } = await setup('Transportes Rota Sul Ltda');

    expect(screen.getByTestId('customer-document')).toHaveTextContent('11.222.•••/••••-81');
    expect(screen.getByTestId('customer-phone')).toHaveTextContent('•••••••7766');
    expect(screen.getByText(text.totalSpent)).toBeInTheDocument();
    const vehicles = screen.getByRole('region', { name: text.vehicles });
    expect(within(vehicles).getAllByRole('listitem')).toHaveLength(3);
    expect(screen.getByRole('button', { name: new RegExp(text.delete) })).toBeDisabled();
    expect(screen.getByText(text.deleteBlockedVehicles)).toBeInTheDocument();
    await expectNoAxeViolations(view.container);
  });

  it('reveals a field only on request and records the access', async () => {
    const { user, id } = await setup('Joana Prado');

    await user.click(screen.getByRole('button', { name: text.reveal(text.document) }));

    expect(await screen.findByText('52998224725')).toBeInTheDocument();
    expect(screen.getByText(text.revealed)).toBeInTheDocument();
    expect(TestBed.inject(FakeBackend).state.database.sensitiveAccess).toEqual([
      expect.objectContaining({ customerId: id, field: 'Document', reason: 'View' }),
    ]);
  });

  it('disables messages without consent and explains why', async () => {
    const { user } = await setup('Transportes Rota Sul Ltda');

    const email = screen.getByRole('button', { name: new RegExp(text.sendEmail) });
    expect(email).toBeDisabled();
    expect(email).toHaveAccessibleDescription(text.noEmailConsent);
    await user.click(screen.getByRole('button', { name: new RegExp(text.sendWhatsApp) }));
    expect(TestBed.inject(ToastService).toasts()[0]?.message).toBe(text.messagingSoon);
  });

  it('registers, edits, transfers and removes vehicles', async () => {
    const { user } = await setup('Joana Prado');
    const vehicles = (): HTMLElement => screen.getByRole('region', { name: text.vehicles });

    await user.click(screen.getByRole('button', { name: new RegExp(text.addVehicle) }));
    const dialog = await screen.findByRole('dialog', { name: vehicleText.newTitle });
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(vehicleText.plate) }),
      'bra2e19',
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(vehicleText.brand) }),
      'Chevrolet',
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(vehicleText.model) }),
      'Onix',
    );
    await user.type(
      within(dialog).getByRole('spinbutton', { name: new RegExp(vehicleText.year) }),
      '2023',
    );
    await user.type(
      within(dialog).getByRole('spinbutton', { name: new RegExp(vehicleText.mileage) }),
      '1000',
    );
    await user.click(within(dialog).getByRole('button', { name: vehicleText.save }));
    await vi.waitFor(() => expect(within(vehicles()).getAllByRole('listitem')).toHaveLength(2));

    await user.click(screen.getByRole('button', { name: vehicleText.edit('FJK4E21') }));
    const edit = await screen.findByRole('dialog', { name: vehicleText.editTitle });
    const mileage = within(edit).getByRole('spinbutton', { name: new RegExp(vehicleText.mileage) });
    await user.clear(mileage);
    await user.type(mileage, '10');
    await user.click(within(edit).getByRole('button', { name: vehicleText.save }));
    expect(await within(edit).findByText(messages.fake.mileageDecrease)).toBeInTheDocument();
    await user.click(within(edit).getByRole('button', { name: vehicleText.cancel }));

    await user.click(screen.getByRole('button', { name: vehicleText.transfer('BRA2E19') }));
    const transfer = await screen.findByRole('dialog', { name: vehicleText.transferTitle });
    await user.click(within(transfer).getByRole('button', { name: vehicleText.transferConfirm }));
    expect(within(transfer).getByText(vehicleText.transferRequired)).toBeInTheDocument();
    await user.type(within(transfer).getByRole('searchbox'), 'Carlos');
    await user.click(await within(transfer).findByRole('radio', { name: /Carlos Mendes/ }));
    await user.click(within(transfer).getByRole('button', { name: vehicleText.transferConfirm }));
    await vi.waitFor(() => expect(within(vehicles()).getAllByRole('listitem')).toHaveLength(1));

    await user.click(screen.getByRole('button', { name: vehicleText.remove('FJK4E21') }));
    const confirm = await screen.findByRole('dialog', { name: vehicleText.removeTitle });
    await user.click(within(confirm).getByRole('button', { name: vehicleText.removeTitle }));
    expect(await screen.findByText(text.noVehicles)).toBeInTheDocument();
  });

  it('deletes a customer without vehicles and goes back to the list', async () => {
    const { user, navigateByUrl } = await setup('Carlos Mendes');

    await user.click(screen.getByRole('button', { name: new RegExp(text.delete) }));
    const confirm = await screen.findByRole('dialog', { name: text.deleteTitle });
    await user.click(within(confirm).getByRole('button', { name: text.deleteConfirm }));

    await vi.waitFor(() => expect(navigateByUrl).toHaveBeenCalledWith('/customers'));
    expect(TestBed.inject(FakeBackend).state.database.customers).toHaveLength(2);
  });

  it('shows the domain message when the API blocks the deletion', async () => {
    const { user } = await setup('Carlos Mendes');
    TestBed.inject(FakeBackend).setScenario('customers', 'conflict');

    await user.click(screen.getByRole('button', { name: new RegExp(text.delete) }));
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: text.deleteConfirm }),
    );

    expect(await screen.findByText(messages.errors.conflict)).toBeInTheDocument();
  });

  it('exports the data of the holder through the queue and offers the download', async () => {
    const { user } = await setup('Joana Prado');
    const createObjectURL = vi.fn(() => 'blob:export');
    Object.assign(URL, { createObjectURL, revokeObjectURL: vi.fn() });

    await user.click(screen.getByRole('button', { name: text.export }));
    const download = await screen.findByRole(
      'button',
      { name: text.exportDownload },
      { timeout: 6000 },
    );
    await user.click(download);

    expect(createObjectURL).toHaveBeenCalled();
  });

  it('starts a quote for the customer without putting data in the URL', async () => {
    const { user, navigate, id } = await setup('Joana Prado');

    await user.click(screen.getByRole('button', { name: text.newQuote }));

    expect(navigate).toHaveBeenCalledWith(['/quotes'], { state: { customerId: id } });
  });

  it('hides export and deletion from the attendant', async () => {
    await setup('Joana Prado', 'Attendant');

    expect(screen.queryByRole('button', { name: text.export })).toBeNull();
    expect(screen.queryByRole('button', { name: new RegExp(text.delete) })).toBeNull();
    expect(screen.getByRole('button', { name: text.reveal(text.phone) })).toBeInTheDocument();
  });

  it('tells when the customer does not exist', async () => {
    await setup('none');

    expect(screen.getByRole('heading', { level: 2, name: text.notFound })).toBeInTheDocument();
  });
});
