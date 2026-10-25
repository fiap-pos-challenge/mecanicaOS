import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FAKE_INVITATION_TOKEN } from '../../../../testing/fake-api/fake-api/fake-database';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { provideAuthData } from '../data/auth.providers';
import { InvitePage, passwordRules, tokenFromHash } from './invite-page';

const text = messages.auth.invitation;

describe('invite helpers', () => {
  it('reads the token from the fragment only', () => {
    expect(tokenFromHash('#token=abc')).toBe('abc');
    expect(tokenFromHash('')).toBeNull();
    expect(tokenFromHash(`#token=${'a'.repeat(129)}`)).toBeNull();
  });

  it('checks each password rule', () => {
    expect(passwordRules('Oficina@2026')).toEqual({
      length: true,
      upper: true,
      lower: true,
      digit: true,
      symbol: true,
    });
    expect(passwordRules('abc')).toEqual({
      length: false,
      upper: false,
      lower: true,
      digit: false,
      symbol: false,
    });
  });
});

describe('InvitePage', () => {
  afterEach(() => window.history.replaceState(null, '', '/'));

  async function setup(hash: string, prepare: () => void = () => undefined) {
    window.history.replaceState(null, '', `/invite${hash}`);
    const view = await render(InvitePage, {
      providers: [...provideTestApi('mock'), provideAuthData()],
      configureTestBed: prepare,
    });
    return { view, user: userEvent.setup() };
  }

  const password = (label: string): HTMLInputElement =>
    screen.getByLabelText(new RegExp(`^\\s*${label}`), { selector: 'input' });

  it('creates the password and removes the token from the address', async () => {
    let replace: ReturnType<typeof vi.spyOn> | null = null;
    const { view, user } = await setup(`#token=${FAKE_INVITATION_TOKEN}`, () => {
      replace = vi.spyOn(TestBed.inject(Location), 'replaceState');
    });

    expect(await screen.findByTestId('invite-details')).toHaveTextContent('p•••@oficina.local');
    expect(screen.getByText(text.greeting('Pedro Convidado'), { exact: false })).toBeVisible();
    expect(replace).toHaveBeenCalledWith('/invite');
    await expectNoAxeViolations(view.container);

    await user.type(password(text.password), 'fraca');
    await user.click(screen.getByRole('button', { name: text.submit }));
    expect(screen.getByText(text.weak)).toBeInTheDocument();

    await user.clear(password(text.password));
    await user.type(password(text.password), 'Oficina@2026');
    await user.type(password(text.confirm), 'Oficina@2025');
    await user.click(screen.getByRole('button', { name: text.submit }));
    expect(screen.getByText(text.mismatch)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: text.showPassword }));
    expect(password(text.password)).toHaveAttribute('type', 'text');
    await user.clear(password(text.confirm));
    await user.type(password(text.confirm), 'Oficina@2026');
    await user.click(screen.getByRole('button', { name: text.submit }));

    expect(await screen.findByRole('heading', { name: text.doneTitle })).toBeVisible();
    expect(screen.getByRole('link', { name: text.goToLogin })).toHaveAttribute('href', '/login');
    expect(
      TestBed.inject(FakeBackend).state.database.users.find(
        (item) => item.email === 'pedro@oficina.local',
      )?.password,
    ).toBe('Oficina@2026');
  });

  it('explains a missing token', async () => {
    await setup('');
    expect(await screen.findByTestId('invite-error')).toHaveTextContent(text.missing);
  });

  it('explains an unknown invitation', async () => {
    await setup('#token=desconhecido');
    expect(await screen.findByTestId('invite-error')).toHaveTextContent(text.notFound);
  });

  it('explains an expired invitation', async () => {
    await setup(`#token=${FAKE_INVITATION_TOKEN}`, () => {
      const invitation = TestBed.inject(FakeBackend).state.database.invitations[0];
      if (invitation) {
        invitation.expiresAt = '2020-01-01T00:00:00Z';
      }
    });
    expect(await screen.findByTestId('invite-error')).toHaveTextContent(text.expired);
  });

  it('switches to used when the token is consumed meanwhile', async () => {
    const { user } = await setup(`#token=${FAKE_INVITATION_TOKEN}`);
    await screen.findByTestId('invite-details');
    const invitation = TestBed.inject(FakeBackend).state.database.invitations[0];
    if (invitation) {
      invitation.revokedAt = '2026-10-05T12:00:00Z';
    }

    await user.type(password(text.password), 'Oficina@2026');
    await user.type(password(text.confirm), 'Oficina@2026');
    await user.click(screen.getByRole('button', { name: text.submit }));

    expect(await screen.findByTestId('invite-error')).toHaveTextContent(text.used);
  });

  it('falls back to a generic failure', async () => {
    await setup(`#token=${FAKE_INVITATION_TOKEN}`, () => {
      TestBed.inject(FakeBackend).setScenario('auth', 'serverError');
    });
    expect(await screen.findByTestId('invite-error')).toHaveTextContent(text.failed);
  });
});
