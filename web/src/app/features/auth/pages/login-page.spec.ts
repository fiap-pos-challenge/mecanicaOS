import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { useGatewayHarness } from '../../../../testing/gateway';
import { SessionService } from '../../../core/auth/session.service';
import { messages } from '../../../core/i18n/messages.pt-BR';
import {
  FAKE_ADMIN_EMAIL,
  FAKE_ADMIN_PASSWORD,
} from '../../../../testing/fake-api/fake-api/fake-database';
import { LoginPage } from './login-page';

const text = messages.auth;

const emailInput = (): HTMLElement => screen.getByRole('textbox', { name: text.login.email });
const passwordInput = (): HTMLElement =>
  screen.getByLabelText(new RegExp(text.login.password), { selector: 'input' });

describe('LoginPage', () => {
  const harness = useGatewayHarness();

  async function setup() {
    const view = await render(LoginPage, { providers: harness.providers('http') });
    const navigate = harness.spyNavigation();
    return { view, navigate, user: userEvent.setup() };
  }

  async function signIn(
    user: ReturnType<typeof userEvent.setup>,
    email: string,
    password: string,
  ): Promise<void> {
    await user.clear(emailInput());
    await user.type(emailInput(), email);
    await user.type(passwordInput(), password);
    await user.click(screen.getByRole('button', { name: text.login.submit }));
  }

  it('signs in and opens the home of the profile', async () => {
    const { view, navigate, user } = await setup();

    expect(screen.getByRole('heading', { level: 1, name: text.login.title })).toBeInTheDocument();
    await expectNoAxeViolations(view.container);
    await signIn(user, FAKE_ADMIN_EMAIL, FAKE_ADMIN_PASSWORD);

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/dashboard'));
  });

  it('asks for both fields before calling the API', async () => {
    const { user } = await setup();

    await user.click(screen.getByRole('button', { name: text.login.submit }));

    expect(await screen.findByTestId('login-error')).toHaveTextContent(text.login.required);
    expect(harness.requests).toEqual([]);
  });

  it('shows a generic message for a wrong password and clears the password', async () => {
    const { user } = await setup();

    await signIn(user, FAKE_ADMIN_EMAIL, 'errada');

    expect(await screen.findByTestId('login-error')).toHaveTextContent(
      text.errors.invalidCredentials,
    );
    expect(passwordInput()).toHaveValue('');
    expect(passwordInput()).toHaveFocus();
  });

  it('tells a disabled account apart only after the right password', async () => {
    const { user } = await setup();

    await signIn(user, 'rita@oficina.local', FAKE_ADMIN_PASSWORD);

    expect(await screen.findByTestId('login-error')).toHaveTextContent(text.errors.accountDisabled);
  });

  it('blocks the form during the rate limit and frees it when the time is over', async () => {
    const { user } = await setup();
    harness.scenario('http', 'auth', 'tooManyRequests');

    await signIn(user, FAKE_ADMIN_EMAIL, FAKE_ADMIN_PASSWORD);

    expect(await screen.findByTestId('login-wait')).toHaveTextContent(
      text.errors.tooManyRequests('0:01'),
    );
    expect(screen.getByRole('button', { name: text.login.submit })).toBeDisabled();
    expect(
      await screen.findByText(text.errors.waitOver, {}, { timeout: 3000 }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: text.login.submit })).toBeEnabled();
  });

  it('shows the remaining lockout time after five wrong passwords', async () => {
    const { user } = await setup();

    for (let attempt = 0; attempt < 5; attempt++) {
      await signIn(user, 'ana@oficina.local', 'errada');
      await screen.findByText(/E-mail ou senha|bloqueada/);
    }

    expect(await screen.findByTestId('login-wait')).toHaveTextContent(
      text.errors.accountLocked('15:00'),
    );
  });

  it('explains when the server cannot be reached', async () => {
    const { user } = await setup();
    harness.server.use(http.post('*/api/auth/login', () => HttpResponse.error()));

    await signIn(user, FAKE_ADMIN_EMAIL, FAKE_ADMIN_PASSWORD);

    expect(await screen.findByTestId('login-error')).toHaveTextContent(text.errors.network);
  });

  it('shows the support code when the server fails', async () => {
    const { user } = await setup();
    harness.scenario('http', 'auth', 'serverError');

    await signIn(user, FAKE_ADMIN_EMAIL, FAKE_ADMIN_PASSWORD);

    expect(await screen.findByTestId('login-error')).toHaveTextContent(text.errors.server);
    expect(screen.getByTestId('login-error')).toHaveTextContent(/Código para o suporte: 00-/);
  });

  it('shows why the previous session ended and toggles the password visibility', async () => {
    const { view, user } = await setup();
    TestBed.inject(SessionService).endReason.set('inactivity');
    view.fixture.detectChanges();

    expect(screen.getByRole('status')).toHaveTextContent(text.notices.inactivity);
    const toggle = screen.getByRole('button', { name: text.login.showPassword });
    await user.click(toggle);
    expect(passwordInput()).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: text.login.hidePassword })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });
});
