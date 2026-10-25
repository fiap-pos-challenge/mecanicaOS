import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { useGatewayHarness } from '../../../../testing/gateway';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FAKE_ADMIN_PASSWORD } from '../../../../testing/fake-api/fake-api/fake-database';
import {
  FAKE_TWO_FACTOR_CODE,
  FAKE_TWO_FACTOR_EMAIL,
} from '../../../../testing/fake-api/fake-api/fake-auth-state';
import { AuthStore } from '../state/auth.store';
import { TwoFactorPage } from './two-factor-page';

const text = messages.auth.twoFactor;

describe('TwoFactorPage', () => {
  const harness = useGatewayHarness();

  async function setup() {
    const view = await render(TwoFactorPage, { providers: harness.providers('http') });
    const navigate = harness.spyNavigation();
    await TestBed.inject(AuthStore).login({
      email: FAKE_TWO_FACTOR_EMAIL,
      password: FAKE_ADMIN_PASSWORD,
    });
    return { view, navigate, user: userEvent.setup() };
  }

  const digit = (position: number): HTMLInputElement => screen.getByLabelText(text.digit(position));

  it('accepts a pasted code and finishes the login', async () => {
    const { view, navigate } = await setup();
    await expectNoAxeViolations(view.container);

    fireEvent.paste(digit(1), { clipboardData: { getData: () => FAKE_TWO_FACTOR_CODE } });

    await vi.waitFor(() => expect(navigate).toHaveBeenLastCalledWith('/dashboard'));
    expect(TestBed.inject(AuthStore).isAuthenticated()).toBe(true);
  });

  it('moves between the digits and rejects a wrong code', async () => {
    const { user } = await setup();

    await user.type(digit(1), '0');
    expect(digit(2)).toHaveFocus();
    await user.keyboard('00');
    await user.keyboard('{Backspace}');
    expect(digit(3)).toHaveFocus();
    await user.keyboard('{Backspace}');
    expect(digit(2)).toHaveFocus();
    await user.keyboard('{ArrowRight}');
    expect(digit(3)).toHaveFocus();
    await user.keyboard('{ArrowLeft}');
    expect(digit(2)).toHaveFocus();
    await user.keyboard('00000');

    expect(await screen.findByTestId('two-factor-error')).toHaveTextContent(text.invalid);
    expect(digit(1)).toHaveValue('');
    expect(digit(1)).toHaveFocus();
  });

  it('asks for all the digits before sending', async () => {
    const { user } = await setup();
    await user.type(digit(1), '12');

    await user.click(screen.getByRole('button', { name: text.submit }));

    expect(await screen.findByTestId('two-factor-error')).toHaveTextContent(text.incomplete);
    expect(digit(3)).toHaveFocus();
  });

  it('distributes a code typed or autofilled in one field', async () => {
    const { navigate } = await setup();

    fireEvent.input(digit(1), { target: { value: FAKE_TWO_FACTOR_CODE } });

    await vi.waitFor(() => expect(navigate).toHaveBeenLastCalledWith('/dashboard'));
  });

  it('cancels the challenge and goes back to the login', async () => {
    const { navigate, user } = await setup();

    await user.click(screen.getByRole('link', { name: text.back }));

    expect(TestBed.inject(AuthStore).status()).toBe('anonymous');
    expect(navigate).toHaveBeenLastCalledWith('/login');
  });

  it('reports server failures without blaming the code', async () => {
    await setup();
    harness.scenario('http', 'auth', 'serverError');

    fireEvent.paste(digit(1), { clipboardData: { getData: () => FAKE_TWO_FACTOR_CODE } });

    expect(await screen.findByTestId('two-factor-error')).toHaveTextContent(
      messages.auth.errors.server,
    );
  });
});
