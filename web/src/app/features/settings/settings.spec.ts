import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent, { UserEvent } from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FAKE_TWO_FACTOR_CODE } from '../../../testing/fake-api/fake-api/fake-auth-state';
import { FAKE_ADMIN_PASSWORD } from '../../../testing/fake-api/fake-api/fake-database';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { BackendRole } from '../../core/auth/permissions';
import { messages } from '../../core/i18n/messages.pt-BR';
import { InactivityService } from '../../core/session/inactivity.service';
import { ToastService } from '../../shared/ui/toast/toast';
import { cents } from '../../shared/util/money';
import { provideAuthData } from '../auth/data/auth.providers';
import { AuthStore } from '../auth/state/auth.store';
import { provideQuoteData, QuoteGateway } from '../quotes/data/quote.gateway';
import { ruleNumber, toValue } from './components/discounts-tab';
import { feeOf } from './components/payments-tab';
import { readAsDataUrl, validLogo } from './components/workshop-tab';
import {
  fromDiscountSettings,
  fromPaymentConfiguration,
  provideSettingsData,
  toDiscountSettings,
  toMessageSettings,
  toPaymentConfiguration,
  toSecurity,
  toWorkshop,
} from './data/settings.gateway';
import {
  renderTemplate,
  unknownVariables,
  validateDiscounts,
  validateMessages,
  validatePayments,
  validateSecurity,
  validateWorkshop,
  Workshop,
} from './data/settings.models';
import { SettingsPage } from './pages/settings-page';
import { SettingsStore } from './state/settings.store';

const text = messages.settings;

const workshop: Workshop = toWorkshop({
  name: 'Oficina',
  document: '11222333000181',
  address: {
    zipCode: '01310100',
    street: 'Rua',
    number: '1',
    city: 'São Paulo',
    state: 'SP',
  },
  hours: [{ weekday: 'Monday', closed: false, opens: '08:00', closes: '18:00' }],
});

describe('settings models', () => {
  it('validates the workshop data', () => {
    expect(validateWorkshop(workshop)).toEqual([]);
    expect(
      validateWorkshop({
        ...workshop,
        name: 'ab',
        document: '11111111111111',
        address: { ...workshop.address, state: 'sp' },
        hours: [{ weekday: 'Monday', closed: false, opens: '18:00', closes: '08:00' }],
      }),
    ).toEqual(['name', 'document', 'address', 'hours']);
    expect(workshop.hours.filter((day) => day.closed)).toHaveLength(6);
    expect(toWorkshop({ name: 'x', document: 'y' }).plan).toEqual({
      name: '',
      renewsAt: '',
      users: 0,
      maxUsers: 0,
    });
  });

  it('validates and maps discount rules with the quote contract', () => {
    const settings = toDiscountSettings({
      rules: [{ id: 'r', name: 'Fiel', type: 'Percent', value: 5, condition: 'LoyalCustomer' }],
      limits: [{ role: 'Attendant', percent: 10 }],
    });
    expect(settings.limits).toEqual({ Admin: 0, Manager: 0, Attendant: 10, Mechanic: 0 });
    expect(validateDiscounts(settings)).toEqual([]);
    expect(fromDiscountSettings(settings).rules).toEqual([
      {
        id: 'r',
        name: 'Fiel',
        type: 'Percent',
        value: 5,
        condition: 'LoyalCustomer',
        isActive: true,
      },
    ]);
    const rule = settings.rules[0];
    if (!rule) {
      throw new Error('Missing rule');
    }

    expect(
      validateDiscounts({
        rules: [
          { ...rule, name: 'x', value: { kind: 'Amount', amount: cents(0) } },
          { ...rule, value: { kind: 'Percent', basisPoints: 20_000 } },
        ],
        limits: { ...settings.limits, Manager: 120 },
      }),
    ).toEqual(['ruleName', 'ruleValue', 'limit']);
    expect(ruleNumber(toValue('Amount', 12.5))).toBe(12.5);
    expect(ruleNumber(toValue('Percent', 7.5))).toBe(7.5);
  });

  it('validates and maps payments', () => {
    const settings = toPaymentConfiguration({
      methods: ['Pix'],
      maxInstallments: 6,
      interestFreeInstallments: 3,
      monthlyInterestPercent: 1.99,
      fees: [{ method: 'Pix', feePercent: 0.99, settlementDays: 0 }],
    });
    expect(validatePayments(settings)).toEqual([]);
    expect(fromPaymentConfiguration(settings)).toMatchObject({
      monthlyInterestPercent: 1.99,
      fees: [{ method: 'Pix', feePercent: 0.99, settlementDays: 0 }],
    });
    expect(feeOf(settings, 'Boleto')).toEqual({
      method: 'Boleto',
      feeBasisPoints: 0,
      settlementDays: 0,
    });
    expect(
      validatePayments({
        methods: [],
        credit: {
          maxInstallments: 2,
          interestFreeInstallments: 3,
          monthlyInterestBasisPoints: 2_000,
        },
        fees: [{ method: 'Pix', feeBasisPoints: 5_000, settlementDays: 90 }],
      }),
    ).toEqual(['methods', 'installments', 'interest', 'fees']);
  });

  it('validates message templates and renders the preview', () => {
    expect(unknownVariables('Olá {cliente}, {nome} e {}')).toEqual(['nome', '']);
    expect(renderTemplate('Olá {cliente}, {nome}', text.messages.sample)).toBe('Olá Joana, {nome}');
    const settings = toMessageSettings({
      templates: [{ kind: 'QuoteSent', channel: 'WhatsApp', body: 'Olá {cliente}' }],
      window: { from: '08:00', to: '18:00', weekdays: ['Monday'] },
    });
    expect(validateMessages(settings)).toEqual([]);
    expect(
      validateMessages({
        templates: [
          { kind: 'QuoteSent', channel: 'WhatsApp', body: ' ' },
          { kind: 'Receipt', channel: 'Email', body: 'x'.repeat(1001) },
          { kind: 'Receipt', channel: 'Email', body: '{senha}' },
        ],
        window: { from: '18:00', to: '08:00', weekdays: [] },
      }),
    ).toEqual(['empty', 'tooLong', 'unknownVariable', 'window']);
    expect(toMessageSettings({}).window).toEqual({ from: '08:00', to: '18:00', weekdays: [] });
  });

  it('validates the security policy', () => {
    expect(validateSecurity(toSecurity({}))).toEqual([]);
    expect(
      validateSecurity({
        requireTwoFactorForAll: true,
        idleMinutes: 2,
        lockoutAttempts: 1,
        lockoutMinutes: 500,
      }),
    ).toEqual(['idle', 'attempts', 'lockout']);
  });

  it('accepts only small PNG and JPEG logos', async () => {
    expect(validLogo({ type: 'image/png', size: 1000 })).toBe(true);
    expect(validLogo({ type: 'image/svg+xml', size: 1000 })).toBe(false);
    expect(validLogo({ type: 'image/jpeg', size: 600 * 1024 })).toBe(false);
    expect(await readAsDataUrl(new Blob(['x'], { type: 'image/png' }))).toContain('data:image/png');
  });
});

async function show(role: BackendRole = 'Admin', mode: TestApiMode = 'mock', manage = true) {
  const view = await render(SettingsPage, {
    providers: [
      ...provideTestApi(mode, [{ path: '**', children: [] }]),
      provideAuthData(),
      provideSettingsData(),
      provideQuoteData(),
      SettingsStore,
    ],
    configureTestBed: () => {
      signInWithFakeApi(role);
      if (!manage) {
        const auth = TestBed.inject(AuthStore);
        const can = auth.can.bind(auth);
        vi.spyOn(auth, 'can').mockImplementation((permission) =>
          permission === 'settings.manage' ? false : can(permission),
        );
      }
    },
  });
  return { view, user: userEvent.setup() };
}

function db() {
  return TestBed.inject(FakeBackend).state.database;
}

async function toast(message: string): Promise<void> {
  await vi.waitFor(() =>
    expect(
      TestBed.inject(ToastService)
        .toasts()
        .map((item) => item.message),
    ).toContain(message),
  );
}

async function confirm(user: UserEvent, secret = FAKE_ADMIN_PASSWORD): Promise<void> {
  const dialog = await screen.findByRole('dialog', { name: text.reauth.title });
  await user.type(within(dialog).getByLabelText(new RegExp(text.reauth.password)), secret);
  await user.click(within(dialog).getByRole('button', { name: text.reauth.confirm }));
}

async function openTab(user: UserEvent, tab: keyof typeof text.tabs): Promise<void> {
  await user.click(screen.getByRole('tab', { name: text.tabs[tab] }));
}

function field(label: string): HTMLInputElement {
  return screen.getByLabelText(
    new RegExp(`^\\s*${label.replace(/[()]/g, '\\$&')}`),
  ) as HTMLInputElement;
}

describe('SettingsPage as Admin', () => {
  it('edits the workshop data after confirming the identity', async () => {
    const { view, user } = await show();
    expect(await screen.findByTestId('plan')).toHaveTextContent('Essencial');
    await user.clear(field(text.workshop.name));
    await user.type(field(text.workshop.name), 'ab');
    await user.click(screen.getByRole('button', { name: text.save }));
    expect(screen.getByText(text.workshop.problems.name)).toBeTruthy();
    await user.type(field(text.workshop.name), 'c Motores');
    await user.type(field(text.workshop.district), ' Sul');
    await user.clear(field(text.workshop.state));
    await user.type(field(text.workshop.state), 'rj');
    await user.click(screen.getByRole('checkbox', { name: text.workshop.closedOn('Domingo') }));
    await user.clear(screen.getByLabelText(text.workshop.opens('Domingo')));
    await user.type(screen.getByLabelText(text.workshop.opens('Domingo')), '09:00');
    await user.clear(screen.getByLabelText(text.workshop.closes('Domingo')));
    await user.type(screen.getByLabelText(text.workshop.closes('Domingo')), '12:00');
    await user.click(screen.getByRole('button', { name: text.save }));

    const dialog = await screen.findByRole('dialog', { name: text.reauth.title });
    await user.click(within(dialog).getByRole('button', { name: text.reauth.confirm }));
    expect(within(dialog).getByText(text.reauth.empty)).toBeTruthy();
    await user.type(within(dialog).getByLabelText(new RegExp(text.reauth.password)), 'errada');
    await user.click(within(dialog).getByRole('button', { name: text.reauth.confirm }));
    expect(await within(dialog).findByText(text.reauth.failed)).toBeTruthy();
    await user.click(within(dialog).getByRole('radio', { name: text.reauth.useCode }));
    await user.type(within(dialog).getByLabelText(/Código de verificação/), FAKE_TWO_FACTOR_CODE);
    await user.click(within(dialog).getByRole('button', { name: text.reauth.confirm }));
    await toast(text.saved);
    expect(db().settings.workshop).toMatchObject({
      name: 'abc Motores',
      address: expect.objectContaining({ state: 'RJ', district: 'Bela Vista Sul' }),
    });
    expect(db().settings.audit[0]?.action).toBe('Dados da oficina alterados');
    await expectNoAxeViolations(view.container);
  });

  it('changes the discount rules and limits that the quote builder uses', async () => {
    const { user } = await show();
    await screen.findByTestId('plan');
    await openTab(user, 'discounts');
    const rules = await screen.findByRole('list', { name: text.discounts.rulesLabel });
    expect(within(rules).getAllByRole('listitem')).toHaveLength(4);
    await user.click(screen.getByRole('button', { name: text.discounts.add }));
    const items = within(rules).getAllByRole('listitem');
    const added = items.at(-1);
    if (!added) {
      throw new Error('Missing rule');
    }

    await user.clear(within(added).getByLabelText(new RegExp(text.discounts.name)));
    await user.type(
      within(added).getByLabelText(new RegExp(text.discounts.name)),
      'Semana do freio',
    );
    await user.selectOptions(
      within(added).getByLabelText(new RegExp(text.discounts.type)),
      'Amount',
    );
    await user.clear(within(added).getByLabelText(new RegExp(text.discounts.value)));
    await user.type(within(added).getByLabelText(new RegExp(text.discounts.value)), '40');
    await user.selectOptions(
      within(added).getByLabelText(new RegExp(text.discounts.condition)),
      'Fleet',
    );
    await user.click(within(added).getByRole('checkbox'));
    await user.click(
      screen.getByRole('button', { name: text.discounts.remove('Aniversário da oficina') }),
    );
    const attendant = field(text.discounts.limitOf(messages.roles.Attendant));
    await user.clear(attendant);
    await user.type(attendant, '12');
    await user.click(screen.getByRole('button', { name: text.save }));
    await confirm(user);
    await toast(text.saved);

    expect(db().quotes.discountLimits.Attendant).toBe(12);
    expect(db().quotes.rules.map((rule) => rule.name)).toEqual([
      'Cliente fiel',
      'Primeiro serviço',
      'Frota',
      'Semana do freio',
    ]);
    const quoteId = db().quotes.quotes[0]?.id ?? '';
    const options = await new Promise<{ rules: readonly { name: string }[] }>((resolve) =>
      TestBed.inject(QuoteGateway).options(quoteId).subscribe(resolve),
    );
    expect(options.rules.map((rule) => rule.name)).toContain('Semana do freio');

    await user.clear(attendant);
    await user.type(attendant, '150');
    await user.click(screen.getByRole('button', { name: text.save }));
    expect(screen.getByText(text.discounts.problems.limit)).toBeTruthy();
  });

  it('changes payment methods, fees and installments', async () => {
    const { user } = await show();
    await screen.findByTestId('plan');
    await openTab(user, 'payments');
    await user.click(
      await screen.findByRole('checkbox', { name: text.payments.methodNames.Boleto }),
    );
    const fee = field(text.payments.fee(text.payments.methodNames.Credit));
    await user.clear(fee);
    await user.type(fee, '3.79');
    const days = field(text.payments.settlement(text.payments.methodNames.Credit));
    await user.clear(days);
    await user.type(days, '15');
    await user.clear(field(text.payments.maxInstallments));
    await user.type(field(text.payments.maxInstallments), '12');
    await user.clear(field(text.payments.interestFree));
    await user.type(field(text.payments.interestFree), '4');
    await user.clear(field(text.payments.interest));
    await user.type(field(text.payments.interest), '2.49');
    await user.click(screen.getByRole('checkbox', { name: text.payments.methodNames.Boleto }));
    await user.click(screen.getByRole('button', { name: text.save }));
    await confirm(user);
    await toast(text.saved);
    expect(db().quotes.payment).toMatchObject({
      maxInstallments: 12,
      interestFreeInstallments: 4,
      monthlyInterestPercent: 2.49,
    });
    expect(db().quotes.payment.fees.find((item) => item.method === 'Credit')).toEqual({
      method: 'Credit',
      feePercent: 3.79,
      settlementDays: 15,
    });

    for (const method of Object.values(text.payments.methodNames)) {
      await user.click(screen.getByRole('checkbox', { name: method }));
    }

    await user.click(screen.getByRole('button', { name: text.save }));
    expect(screen.getByText(text.payments.problems.methods)).toBeTruthy();
  });

  it('edits message templates with preview and rejects unknown variables', async () => {
    const { user } = await show();
    await screen.findByTestId('plan');
    await openTab(user, 'messages');
    const label = text.messages.body(
      text.messages.kinds.QuoteSent,
      text.messages.channels.WhatsApp,
    );
    const body = (await screen.findByLabelText(new RegExp(label))) as HTMLTextAreaElement;
    expect(screen.getAllByTestId('message-preview')[0]).toHaveTextContent('Olá, Joana!');
    await user.clear(body);
    await user.type(body, 'Oi {{cliente}, prazo {{prazo} e {{senha}');
    expect(screen.getByText(text.messages.unknown('{senha}'))).toBeTruthy();
    expect(screen.getAllByTestId('message-preview')[0]).toHaveTextContent(
      'Oi Joana, prazo sexta-feira, 18h e {senha}',
    );
    await user.click(screen.getByRole('button', { name: text.save }));
    expect(screen.getByText(text.messages.problems.unknownVariable)).toBeTruthy();
    await user.clear(body);
    await user.type(body, 'Oi {{cliente}, veja {{link}');
    await user.click(screen.getByRole('checkbox', { name: text.weekdays.Sunday }));
    await user.click(screen.getByRole('checkbox', { name: text.weekdays.Monday }));
    await user.clear(field(text.messages.to));
    await user.type(field(text.messages.to), '20:00');
    await user.click(screen.getByRole('button', { name: text.save }));
    await confirm(user);
    await toast(text.saved);
    expect(db().settings.messages.templates[0]?.body).toBe('Oi {cliente}, veja {link}');
    expect(db().settings.messages.window).toMatchObject({
      to: '20:00',
      weekdays: ['Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
    });
  });

  it('applies the security policy, ends sessions and filters the audit log', async () => {
    const { user } = await show();
    await screen.findByTestId('plan');
    const inactivity = TestBed.inject(InactivityService);
    const idle = vi.spyOn(inactivity, 'setIdleMinutes');
    await openTab(user, 'security');
    expect(await screen.findByRole('table', { name: text.security.auditCaption })).toBeTruthy();
    await vi.waitFor(() => expect(idle).toHaveBeenCalledWith(30));
    await user.click(screen.getByRole('checkbox', { name: text.security.twoFactor }));
    await user.clear(field(text.security.idle));
    await user.type(field(text.security.idle), '2');
    await user.click(screen.getByRole('button', { name: text.save }));
    expect(screen.getByText(text.security.problems.idle)).toBeTruthy();
    await user.clear(field(text.security.idle));
    await user.type(field(text.security.idle), '15');
    await user.clear(field(text.security.attempts));
    await user.type(field(text.security.attempts), '4');
    await user.clear(field(text.security.lockout));
    await user.type(field(text.security.lockout), '30');
    await user.click(screen.getByRole('button', { name: text.save }));
    await confirm(user);
    await toast(text.saved);
    expect(idle).toHaveBeenLastCalledWith(15);
    expect(db().settings.security).toEqual({
      requireTwoFactorForAll: true,
      idleMinutes: 15,
      lockoutAttempts: 4,
      lockoutMinutes: 30,
    });

    const sessions = screen.getByRole('list', { name: text.security.sessionsLabel });
    await user.click(
      within(sessions).getByRole('button', {
        name: text.security.end('Chrome no Android', 'Marcos Mecânico'),
      }),
    );
    await confirm(user);
    await toast(text.security.ended);
    expect(within(sessions).getAllByRole('listitem')).toHaveLength(2);

    await user.selectOptions(field(text.security.area), 'stock');
    await user.click(screen.getByRole('button', { name: text.security.filter }));
    const table = screen.getByRole('table', { name: text.security.auditCaption });
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(2));
    expect(within(table).getByText('Ajuste de estoque')).toBeTruthy();
    await user.selectOptions(field(text.security.area), 'all');
    await user.type(field(text.security.actor), 'ninguém');
    await user.type(field(text.security.from), '2026-10-01');
    await user.type(field(text.security.to), '2026-10-31');
    await user.click(screen.getByRole('button', { name: text.security.filter }));
    expect(await within(table).findByText(text.security.empty)).toBeTruthy();
  });

  it('shows the fiscal integrations as information only', async () => {
    const { user } = await show();
    await screen.findByTestId('plan');
    await openTab(user, 'fiscal');
    expect(await screen.findByText(text.fiscal.kinds.NFSe)).toBeTruthy();
    expect(screen.getByText(text.fiscal.statuses.Planned)).toBeTruthy();
  });

  it('cancels the confirmation without saving and reports failures', async () => {
    const { user } = await show();
    await screen.findByTestId('plan');
    await user.click(screen.getByRole('button', { name: text.save }));
    const dialog = await screen.findByRole('dialog', { name: text.reauth.title });
    await user.click(within(dialog).getByRole('button', { name: text.reauth.cancel }));
    expect(db().settings.audit).toHaveLength(5);

    TestBed.inject(FakeBackend).setScenario('settings', 'serverError');
    await openTab(user, 'fiscal');
    expect(await screen.findByText(text.loadError)).toBeTruthy();
    TestBed.inject(FakeBackend).setScenario('settings', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByText(text.fiscal.kinds.NFe)).toBeTruthy();
  });
});

describe('SettingsPage for other profiles', () => {
  it('shows the settings read-only without the manage permission, without security', async () => {
    const { view, user } = await show('Admin', 'mock', false);
    expect(await screen.findByText(text.readOnly)).toBeTruthy();
    expect(screen.queryByRole('tab', { name: text.tabs.security })).toBeNull();
    expect(screen.queryByRole('button', { name: text.save })).toBeNull();
    expect(
      (await screen.findByRole('textbox', { name: new RegExp(text.workshop.name) })).closest(
        'fieldset',
      )?.disabled,
    ).toBe(true);
    await openTab(user, 'discounts');
    expect(await screen.findByRole('list', { name: text.discounts.rulesLabel })).toBeTruthy();
    expect(screen.queryByRole('button', { name: text.discounts.add })).toBeNull();
    await openTab(user, 'payments');
    expect(await screen.findByText(text.payments.title)).toBeTruthy();
    await openTab(user, 'messages');
    expect(await screen.findByText(text.messages.title)).toBeTruthy();
    await expectNoAxeViolations(view.container);
  });

  it('shows the pending notice while the API does not exist', async () => {
    await show('Admin', 'pending');
    expect(await screen.findByText(messages.pending.settings)).toBeTruthy();
  });
});
