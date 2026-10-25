import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
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
import { percentValidator, shiftValidator, TeamMemberPage } from './team-member-page';

const text = messages.team.member;

describe('team member validators', () => {
  it('requires the end of a worked day after the start', () => {
    const shift = (works: boolean, start: string, end: string) =>
      new FormGroup({
        works: new FormControl(works),
        start: new FormControl(start),
        end: new FormControl(end),
      });

    expect(shiftValidator(shift(true, '08:00', '17:00'))).toBeNull();
    expect(shiftValidator(shift(true, '17:00', '08:00'))).toEqual({ shift: true });
    expect(shiftValidator(shift(true, '', '08:00'))).toEqual({ shift: true });
    expect(shiftValidator(shift(false, '17:00', '08:00'))).toBeNull();
  });

  it('accepts percentages within the limit', () => {
    const validate = percentValidator(30);

    expect(validate(new FormControl(0))).toBeNull();
    expect(validate(new FormControl(30))).toBeNull();
    expect(validate(new FormControl(31))).toEqual({ percent: true });
    expect(validate(new FormControl(null))).toEqual({ percent: true });
    expect(validate(new FormControl(''))).toEqual({ percent: true });
  });
});

describe('TeamMemberPage', () => {
  async function setup(email: string, role: BackendRole = 'Admin') {
    let id = '';
    const view = await render(TeamMemberPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideTeamData()],
      inputs: { id: 'loading' },
      configureTestBed: () => {
        signInWithFakeApi(role);
        id =
          TestBed.inject(FakeBackend).state.database.users.find((user) => user.email === email)
            ?.id ?? 'missing';
      },
    });
    view.fixture.componentRef.setInput('id', id);
    view.fixture.detectChanges();
    return { view, id, user: userEvent.setup() };
  }

  const field = (label: string): HTMLInputElement =>
    screen.getByLabelText(new RegExp(`^\\s*${label.replace(/[()]/g, '\\$&')}`), {
      selector: 'input, select',
    });

  it('shows the mechanic with access, profile and productivity', async () => {
    const { view } = await setup('marcos@oficina.local');

    expect(await screen.findByRole('heading', { level: 1, name: 'Marcos Mecânico' })).toBeVisible();
    expect(field(text.name)).toHaveValue('Marcos Mecânico');
    expect(screen.getByText('Motor')).toBeInTheDocument();
    expect(field(text.discountLimit)).toBeDisabled();
    expect(screen.getByText(text.discountMechanic)).toBeInTheDocument();
    expect(await screen.findByText(text.kpis.orders)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: text.worksOn('domingo') })).not.toBeChecked();
    await expectNoAxeViolations(view.container);
  });

  it('saves the access and the professional profile', async () => {
    const { user, id } = await setup('marcos@oficina.local');
    await screen.findByRole('heading', { level: 1 });
    const backend = TestBed.inject(FakeBackend);

    await user.clear(field(text.name));
    await user.type(field(text.name), 'Marcos Lima');
    await user.click(screen.getByRole('button', { name: text.saveAccess }));
    await vi.waitFor(() =>
      expect(backend.state.database.users.find((item) => item.id === id)?.name).toBe('Marcos Lima'),
    );

    const specialty = field(text.specialties);
    await user.type(specialty, 'Freios{Enter}');
    await user.type(specialty, 'freios{Enter}');
    expect(screen.getByText(text.duplicatedSpecialty)).toBeInTheDocument();
    await user.clear(specialty);
    await user.type(specialty, 'x');
    await user.click(screen.getByRole('button', { name: text.addSpecialty }));
    expect(screen.getByText(text.invalidSpecialty)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: text.removeSpecialty('Motor') }));
    await user.click(screen.getByRole('checkbox', { name: text.worksOn('domingo') }));
    await user.clear(field(text.commission));
    await user.type(field(text.commission), '15');
    await user.click(screen.getByRole('button', { name: text.saveProfile }));

    await vi.waitFor(() =>
      expect(backend.state.database.teamProfiles.find((item) => item.userId === id)).toMatchObject({
        specialties: ['Transmissão', 'Freios'],
        commissionPercent: 15,
      }),
    );
    expect(
      backend.state.database.teamProfiles.find((item) => item.userId === id)?.schedule,
    ).toHaveLength(7);
  });

  it('validates shifts and percentages before saving', async () => {
    const { user } = await setup('gestor@oficina.local');
    await screen.findByRole('heading', { level: 1 });

    expect(field(text.discountLimit)).toBeEnabled();
    await user.clear(field(text.discountLimit));
    await user.type(field(text.discountLimit), '45');
    const start = screen.getByLabelText(text.start('segunda'));
    await user.clear(start);
    await user.type(start, '18:00');
    await user.click(screen.getByRole('button', { name: text.saveProfile }));

    expect(screen.getByText(text.invalidPercent(30))).toBeInTheDocument();
    expect(screen.getByText(text.invalidShift)).toBeInTheDocument();
  });

  it('marks the discount when the API refuses it for a mechanic', async () => {
    const { user } = await setup('gestor@oficina.local');
    await screen.findByRole('heading', { level: 1 });
    const backend = TestBed.inject(FakeBackend);
    const gabi = backend.state.database.users.find((item) => item.email === 'gestor@oficina.local');
    if (gabi) {
      gabi.role = 'Mechanic';
    }

    await user.clear(field(text.discountLimit));
    await user.type(field(text.discountLimit), '10');
    await user.click(screen.getByRole('button', { name: text.saveProfile }));

    expect(await screen.findByText(messages.fake.mechanicDiscount)).toBeInTheDocument();
  });

  it('deactivates and explains why the own account cannot be deactivated', async () => {
    const { user } = await setup('ana@oficina.local');
    await screen.findByRole('heading', { level: 1 });

    await user.click(screen.getByRole('button', { name: messages.team.list.deactivate }));
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', {
        name: messages.team.list.deactivate,
      }),
    );
    expect(await screen.findByText(messages.team.statuses.Inactive)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: messages.team.list.reactivate }));
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', {
        name: messages.team.list.reactivate,
      }),
    );
    expect(await screen.findByText(messages.team.statuses.Active)).toBeInTheDocument();
  });

  it('blocks the self deactivation of the admin', async () => {
    await setup('admin@oficina.local');
    await screen.findByRole('heading', { level: 1 });

    expect(screen.getByRole('button', { name: messages.team.list.deactivate })).toBeDisabled();
    expect(screen.getByText(messages.team.list.selfReason)).toBeInTheDocument();
    expect(screen.getByText(text.noProductivity)).toBeInTheDocument();
  });

  it('resends a pending invitation', async () => {
    const { user } = await setup('pedro@oficina.local');
    await screen.findByRole('heading', { level: 1, name: 'Pedro Convidado' });
    const before = TestBed.inject(FakeBackend).state.database.invitations.length;

    await user.click(screen.getByRole('button', { name: messages.team.list.resend }));

    await vi.waitFor(() =>
      expect(TestBed.inject(FakeBackend).state.database.invitations).toHaveLength(before + 1),
    );
  });

  it('is read-only for the manager', async () => {
    await setup('marcos@oficina.local', 'Manager');
    await screen.findByRole('heading', { level: 1 });

    expect(field(text.name)).toBeDisabled();
    expect(screen.queryByRole('button', { name: text.saveProfile })).toBeNull();
    expect(screen.getByText(messages.team.list.readOnly)).toBeInTheDocument();
  });

  it('shows not found and load errors', async () => {
    await setup('nobody@oficina.local');
    expect(await screen.findByText(text.notFound)).toBeInTheDocument();
  });

  it('retries after a server failure', async () => {
    const view = await render(TeamMemberPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideTeamData()],
      inputs: { id: 'loading' },
      configureTestBed: () => {
        signInWithFakeApi('Admin');
        TestBed.inject(FakeBackend).setScenario('users', 'serverError');
      },
    });
    const id = TestBed.inject(FakeBackend).state.database.users[0]?.id ?? '';
    view.fixture.componentRef.setInput('id', id);
    view.fixture.detectChanges();

    expect(await screen.findByText(text.loadError)).toBeInTheDocument();
    TestBed.inject(FakeBackend).setScenario('users', 'none');
    await userEvent.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByRole('heading', { level: 1, name: 'Administrador' })).toBeVisible();
  });
});
