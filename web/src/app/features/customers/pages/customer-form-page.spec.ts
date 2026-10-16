import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { DraftStore } from '../../../core/drafts/draft.store';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideCustomersData } from '../data/customers.providers';
import { completeAddress, CustomerFormPage, formatZipCode } from './customer-form-page';
import { FormControl, FormGroup } from '@angular/forms';

const text = messages.customers.form;

describe('customer form helpers', () => {
  it('masks the zip code and requires the whole address once started', () => {
    expect(formatZipCode('01310100')).toBe('01310-100');
    expect(formatZipCode('0131')).toBe('0131');
    const group = (street: string, city: string) =>
      new FormGroup({
        zipCode: new FormControl('01310100'),
        street: new FormControl(street),
        number: new FormControl('1'),
        district: new FormControl('Centro'),
        city: new FormControl(city),
        state: new FormControl('SP'),
      });
    expect(completeAddress(group('Rua', 'São Paulo'))).toBeNull();
    expect(completeAddress(group('Rua', ''))).toEqual({ incompleteAddress: true });
    expect(
      completeAddress(
        new FormGroup({ zipCode: new FormControl(''), street: new FormControl(null) }),
      ),
    ).toBeNull();
  });
});

describe('CustomerFormPage', () => {
  async function setup(customerName: string | null = null, draft?: Record<string, unknown>) {
    let id: string | null = null;
    const view = await render(CustomerFormPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideCustomersData()],
      inputs: { id: null },
      configureTestBed: () => {
        const user = signInWithFakeApi('Attendant');
        id =
          customerName === null
            ? null
            : (TestBed.inject(FakeBackend).state.database.customers.find(
                (customer) => customer.name === customerName,
              )?.id ?? null);
        TestBed.inject(DraftStore).claim(user.id);
        if (draft) {
          TestBed.inject(DraftStore).save(`customer-form:${id ?? 'new'}`, draft);
        }
      },
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    view.fixture.componentRef.setInput('id', id);
    view.fixture.detectChanges();
    return { view, id, navigate, user: userEvent.setup() };
  }

  const field = (label: string): HTMLInputElement =>
    screen.getByLabelText(new RegExp(`^\\s*${label.replace(/[()]/g, '\\$&')}`), {
      selector: 'input',
    });

  it('creates a customer with masks, zip code lookup and consent', async () => {
    const { view, user, navigate } = await setup();
    await expectNoAxeViolations(view.container);

    await user.type(field(text.name), 'Paula Reis');
    await user.type(field(text.document), '39053344705');
    await user.type(field(text.phone), '11912345678');
    expect(field(text.document)).toHaveValue('390.533.447-05');
    expect(field(text.phone)).toHaveValue('(11) 91234-5678');
    expect(screen.getByRole('checkbox', { name: text.consentEmail })).toBeDisabled();
    expect(screen.getByText(text.consentEmailNeedsEmail)).toBeInTheDocument();
    await user.type(field(text.email), 'paula@example.com');
    await user.type(field(text.zipCode), '01310100');
    await vi.waitFor(() => expect(field(text.street)).toHaveValue('Avenida Paulista'));
    await user.type(field(text.number), '200');
    await user.click(screen.getByRole('checkbox', { name: text.consentWhatsApp }));
    await user.click(screen.getByRole('checkbox', { name: text.consentEmail }));
    await user.click(screen.getByRole('button', { name: text.save }));

    await vi.waitFor(() => expect(navigate).toHaveBeenCalled());
    const created = TestBed.inject(FakeBackend).state.database.customers.find(
      (customer) => customer.name === 'Paula Reis',
    );
    expect(created).toMatchObject({
      document: '39053344705',
      consentWhatsApp: true,
      consentEmail: true,
      address: expect.objectContaining({ zipCode: '01310100', state: 'SP' }),
    });
    expect(navigate).toHaveBeenCalledWith(['/customers', created?.id]);
    expect(TestBed.inject(DraftStore).restore('customer-form:new')).toBeNull();
  });

  it('shows the field errors before calling the API', async () => {
    const { user } = await setup();

    await user.type(field(text.document), '123');
    await user.type(field(text.email), 'x');
    await user.type(field(text.state), 'S');
    await user.type(field(text.street), 'Rua');
    await user.click(screen.getByRole('button', { name: text.save }));

    expect(await screen.findByTestId('customer-form-error')).toHaveTextContent(text.fixErrors);
    expect(screen.getAllByText(text.required).length).toBeGreaterThan(0);
    expect(screen.getByText(text.invalidDocument)).toBeInTheDocument();
    expect(screen.getByText(text.invalidEmail)).toBeInTheDocument();
    expect(screen.getByText(text.invalidState)).toBeInTheDocument();
    expect(screen.getByText(text.incompleteAddress)).toBeInTheDocument();
  });

  it('marks the document when it is already registered', async () => {
    const { user } = await setup();

    await user.type(field(text.name), 'Outra Joana');
    await user.type(field(text.document), '52998224725');
    await user.type(field(text.phone), '11912345678');
    await user.click(screen.getByRole('button', { name: text.save }));

    expect(await screen.findByTestId('customer-form-error')).toHaveTextContent(
      messages.fake.documentInUse,
    );
    expect(field(text.document)).toHaveAttribute('aria-invalid', 'true');
  });

  it('explains unknown zip codes and lookup failures', async () => {
    const { user } = await setup();

    await user.type(field(text.zipCode), '99999999');
    expect(await screen.findByText(text.zipNotFound)).toBeInTheDocument();

    TestBed.inject(FakeBackend).setScenario('addresses', 'serverError');
    await user.click(screen.getByRole('button', { name: text.lookup }));
    expect(await screen.findByText(text.zipFailed)).toBeInTheDocument();
  });

  it('loads the customer for editing with the revealed contact and saves', async () => {
    const { user, navigate, id } = await setup('Joana Prado');

    await vi.waitFor(() => expect(field(text.phone)).toHaveValue('(11) 98765-4321'));
    expect(field(text.email)).toHaveValue('joana@example.com');
    expect(field(text.document)).toBeDisabled();
    expect(screen.getByText(/Consentimento registrado em/)).toBeInTheDocument();
    expect(
      TestBed.inject(FakeBackend).state.database.sensitiveAccess.map((access) => access.reason),
    ).toEqual(['Edit', 'Edit']);

    await user.clear(field(text.name));
    await user.type(field(text.name), 'Joana P. Lima');
    await user.click(screen.getByRole('button', { name: text.save }));

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/customers', id]));
    expect(
      TestBed.inject(FakeBackend).state.database.customers.find((customer) => customer.id === id)
        ?.name,
    ).toBe('Joana P. Lima');
  });

  it('restores the draft kept when the session expired', async () => {
    await setup(null, { name: 'Rascunho guardado' });

    expect(await screen.findByText(text.draftRestored)).toBeInTheDocument();
    expect(field(text.name)).toHaveValue('Rascunho guardado');
  });

  it('shows the message of an unexpected API answer', async () => {
    const { user } = await setup();
    TestBed.inject(FakeBackend).setScenario('customers', 'unprocessable');

    await user.type(field(text.name), 'Paula Reis');
    await user.type(field(text.document), '39053344705');
    await user.type(field(text.phone), '11912345678');
    await user.click(screen.getByRole('button', { name: text.save }));

    expect(await screen.findByTestId('customer-form-error')).toHaveTextContent(
      messages.errors.businessRule,
    );
  });
});
