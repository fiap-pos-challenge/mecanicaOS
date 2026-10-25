import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { PERMISSIONS } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideTeamData } from '../data/team.providers';
import { groupPermissions, PermissionsPage } from './permissions-page';

const text = messages.team.permissions;

describe('groupPermissions', () => {
  it('keeps the catalog order inside each module', () => {
    const groups = groupPermissions(PERMISSIONS);

    expect(groups[0]).toEqual({ module: 'dashboard', permissions: ['dashboard.view'] });
    expect(groups.find((group) => group.module === 'team')?.permissions).toEqual([
      'team.view',
      'team.manage',
    ]);
    expect(groups.flatMap((group) => group.permissions)).toHaveLength(PERMISSIONS.length);
  });
});

describe('PermissionsPage', () => {
  async function setup(scenario: 'none' | 'serverError' = 'none') {
    const view = await render(PermissionsPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideTeamData()],
      configureTestBed: () => {
        signInWithFakeApi('Manager');
        TestBed.inject(FakeBackend).setScenario('roles', scenario);
      },
    });
    return { view, user: userEvent.setup() };
  }

  it('shows the read-only matrix and the accordion by role', async () => {
    const { view } = await setup();

    const table = await screen.findByRole('table', { name: text.caption });
    expect(screen.getByText(text.readOnly)).toBeInTheDocument();
    expect(
      within(table)
        .getAllByRole('columnheader')
        .map((cell) => cell.textContent?.trim()),
    ).toEqual(expect.arrayContaining([messages.roles.Admin, messages.roles.Mechanic]));
    const row = within(table).getByRole('row', {
      name: new RegExp(messages.permissions.labels['team.manage']),
    });
    expect(within(row).getAllByText(text.allowed)).toHaveLength(1);
    expect(within(row).getAllByText(text.denied)).toHaveLength(4);
    expect(screen.getAllByText(text.count(5))).toHaveLength(2);
    await expectNoAxeViolations(view.container);
  }, 60_000);

  it('retries after a failure', async () => {
    const { user } = await setup('serverError');

    expect(await screen.findByText(text.loadError)).toBeInTheDocument();
    TestBed.inject(FakeBackend).setScenario('roles', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByRole('table', { name: text.caption })).toBeInTheDocument();
  });
});
