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
import { provideTeamData } from '../data/team.providers';
import { User } from '../data/user.models';
import { accessBlockOf, invitationExpired } from './team-display';
import { TeamPage } from './team-page';

const text = messages.team.list;

function user(overrides: Partial<User>): User {
  return {
    id: 'u1',
    name: 'Ana',
    email: 'a@b.c',
    role: 'Admin',
    isActive: true,
    status: 'Active',
    invitationExpiresAt: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    ...overrides,
  };
}

describe('team display helpers', () => {
  it('blocks the own account and the last active admin', () => {
    expect(accessBlockOf(user({}), 'u1', 3)).toBe('self');
    expect(accessBlockOf(user({}), 'other', 1)).toBe('lastAdmin');
    expect(accessBlockOf(user({}), 'other', 2)).toBeNull();
    expect(accessBlockOf(user({ role: 'Mechanic' }), 'other', 1)).toBeNull();
  });

  it('detects expired invitations', () => {
    const pending = user({ status: 'InvitationPending' });

    expect(invitationExpired({ ...pending, invitationExpiresAt: '2020-01-01T00:00:00Z' })).toBe(
      true,
    );
    expect(invitationExpired({ ...pending, invitationExpiresAt: '2999-01-01T00:00:00Z' })).toBe(
      false,
    );
    expect(invitationExpired(pending)).toBe(true);
    expect(invitationExpired(user({}))).toBe(false);
  });
});

describe('TeamPage', () => {
  async function setup(role: BackendRole = 'Admin') {
    const view = await render(TeamPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideTeamData()],
      configureTestBed: () => {
        signInWithFakeApi(role);
      },
    });
    const list = await screen.findByRole('list', { name: text.caption });
    await vi.waitFor(() => expect(within(list).getAllByRole('listitem')).toHaveLength(7));
    return { view, list, user: userEvent.setup() };
  }

  const card = (list: HTMLElement, name: string): HTMLElement => {
    const link = within(list).getByRole('link', { name: text.open(name) });
    const item = link.closest('li');
    if (!item) {
      throw new Error(`Card of ${name} not found.`);
    }

    return item;
  };

  it('shows the team as cards with roles, production and invitations', async () => {
    const { view, list } = await setup();

    expect(screen.getByRole('heading', { level: 1, name: messages.screens.team })).toBeVisible();
    expect(screen.getByText(text.subtitle(7))).toBeInTheDocument();
    const marcos = card(list, 'Marcos Mecânico');
    expect(within(marcos).getByText('Motor · Transmissão')).toBeInTheDocument();
    expect(within(marcos).getByRole('img', { name: /Ordens de serviço por semana/ })).toBeVisible();
    expect(within(card(list, 'Ana Atendente')).getByText(text.noProduction)).toBeInTheDocument();
    const pedro = card(list, 'Pedro Convidado');
    expect(within(pedro).getByText(messages.team.statuses.InvitationPending)).toBeInTheDocument();
    expect(within(pedro).getByText(/Convite válido até/)).toBeInTheDocument();
    const admin = card(list, 'Administrador');
    expect(within(admin).getByText(text.you)).toBeInTheDocument();
    expect(
      within(admin).getByRole('button', { name: text.deactivateOf('Administrador') }),
    ).toBeDisabled();
    expect(within(admin).getByText(text.selfReason)).toBeInTheDocument();
    await expectNoAxeViolations(view.container);
  });

  it('filters by role and status and switches to the list view', async () => {
    const { list, user } = await setup();

    await user.click(screen.getByRole('radio', { name: messages.roles.Mechanic }));
    await vi.waitFor(() => expect(within(list).getAllByRole('listitem')).toHaveLength(3));
    await user.selectOptions(screen.getByRole('combobox', { name: text.status }), 'Inactive');
    await vi.waitFor(() => expect(within(list).getAllByRole('listitem')).toHaveLength(1));
    await user.click(screen.getByRole('button', { name: text.clear }));
    await vi.waitFor(() => expect(within(list).getAllByRole('listitem')).toHaveLength(7));

    await user.click(screen.getByRole('radio', { name: text.viewList }));
    const table = screen.getByRole('table', { name: text.caption });
    expect(within(table).getByText('marcos@oficina.local')).toBeInTheDocument();
    expect(within(table).getAllByText(messages.team.statuses.Active).length).toBeGreaterThan(0);
  });

  it('switches the production period', async () => {
    const { list, user } = await setup();
    const trend = (): string | null =>
      within(card(list, 'Marcos Mecânico'))
        .getByRole('img', { name: /Ordens de serviço por semana/ })
        .getAttribute('aria-label');
    const before = trend();

    await user.click(screen.getByRole('radio', { name: text.period90 }));

    await vi.waitFor(() => expect(trend()).not.toBe(before));
  });

  it('invites a member and lists the pending invitation', async () => {
    const { list, user } = await setup();
    const invite = messages.team.invite;

    await user.click(screen.getByRole('button', { name: text.invite }));
    const dialog = await screen.findByRole('dialog', { name: invite.title });
    await user.type(
      within(dialog).getByLabelText(new RegExp(`^\\s*${invite.name}`)),
      'Bruna Oliveira',
    );
    await user.type(within(dialog).getByLabelText(new RegExp(invite.email)), 'ana@oficina.local');
    await user.selectOptions(within(dialog).getByRole('combobox'), messages.roles.Manager);
    await user.click(within(dialog).getByRole('button', { name: invite.submit }));
    expect(await within(dialog).findByText(messages.fake.emailInUse)).toBeInTheDocument();

    const email = within(dialog).getByLabelText(new RegExp(invite.email));
    await user.clear(email);
    await user.type(email, 'bruna@oficina.local');
    await user.click(within(dialog).getByRole('button', { name: invite.submit }));

    await vi.waitFor(() => expect(within(list).getAllByRole('listitem')).toHaveLength(8));
    expect(
      within(card(list, 'Bruna Oliveira')).getByText(messages.team.statuses.InvitationPending),
    ).toBeInTheDocument();
  });

  it('deactivates, reactivates and resends after confirmation', async () => {
    const { list, user } = await setup();

    await user.click(
      within(card(list, 'Ana Atendente')).getByRole('button', {
        name: text.deactivateOf('Ana Atendente'),
      }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: text.deactivate }),
    );
    await vi.waitFor(() =>
      expect(
        within(card(list, 'Ana Atendente')).getByText(messages.team.statuses.Inactive),
      ).toBeInTheDocument(),
    );

    await user.click(
      within(card(list, 'Ana Atendente')).getByRole('button', {
        name: text.reactivateOf('Ana Atendente'),
      }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: text.reactivate }),
    );
    await vi.waitFor(() =>
      expect(
        within(card(list, 'Ana Atendente')).queryByText(messages.team.statuses.Inactive),
      ).toBeNull(),
    );

    const tokens = TestBed.inject(FakeBackend).state.database.invitations.length;
    await user.click(
      within(card(list, 'Pedro Convidado')).getByRole('button', {
        name: text.resendOf('Pedro Convidado'),
      }),
    );
    await vi.waitFor(() =>
      expect(TestBed.inject(FakeBackend).state.database.invitations).toHaveLength(tokens + 1),
    );
  });

  it('keeps the team when the deactivation is cancelled or fails', async () => {
    const { list, user } = await setup();
    const backend = TestBed.inject(FakeBackend);
    const ana = (): HTMLElement => card(list, 'Ana Atendente');

    await user.click(
      within(ana()).getByRole('button', { name: text.deactivateOf('Ana Atendente') }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', {
        name: messages.ui.dialog.cancel,
      }),
    );
    expect(within(ana()).queryByText(messages.team.statuses.Inactive)).toBeNull();

    backend.setScenario('users', 'unprocessable');
    await user.click(
      within(ana()).getByRole('button', { name: text.deactivateOf('Ana Atendente') }),
    );
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: text.deactivate }),
    );
    await vi.waitFor(() =>
      expect(
        backend.state.database.users.find((item) => item.name === 'Ana Atendente')?.isActive,
      ).toBe(true),
    );
  });

  it('is read-only for the manager', async () => {
    await setup('Manager');

    expect(screen.getByText(text.readOnly)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: text.invite })).toBeNull();
    expect(screen.queryByRole('button', { name: text.deactivateOf('Ana Atendente') })).toBeNull();
  });

  it('shows the load error and retries', async () => {
    const view = await render(TeamPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideTeamData()],
      configureTestBed: () => {
        signInWithFakeApi('Admin');
        TestBed.inject(FakeBackend).setScenario('users', 'serverError');
      },
    });

    expect(await screen.findByText(text.loadError)).toBeInTheDocument();
    TestBed.inject(FakeBackend).setScenario('users', 'none');
    TestBed.inject(FakeBackend).setScenario('team', 'serverError');
    await userEvent.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByText(text.productionUnavailable)).toBeInTheDocument();
    expect(view.container.querySelectorAll('li.member')).toHaveLength(7);
  });
});
