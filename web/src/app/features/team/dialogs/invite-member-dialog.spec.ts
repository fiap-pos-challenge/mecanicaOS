import { DialogRef } from '@angular/cdk/dialog';
import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { UI_DIALOG_TITLE_ID } from '../../../shared/ui/dialog/dialog';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideTeamData } from '../data/team.providers';
import { InviteMemberDialog } from './invite-member-dialog';

const text = messages.team.invite;

describe('InviteMemberDialog', () => {
  async function setup() {
    const close = vi.fn();
    await render(InviteMemberDialog, {
      providers: [
        ...provideTestApi('mock'),
        provideAuthData(),
        provideTeamData(),
        { provide: DialogRef, useValue: { close } },
        { provide: UI_DIALOG_TITLE_ID, useValue: 'title' },
      ],
      configureTestBed: () => {
        signInWithFakeApi('Admin');
      },
    });
    return { close, user: userEvent.setup() };
  }

  it('validates the fields before calling the API', async () => {
    const { close, user } = await setup();

    await user.type(screen.getByLabelText(new RegExp(text.email)), 'x');
    await user.click(screen.getByRole('button', { name: text.submit }));

    expect(screen.getByText(text.required)).toBeInTheDocument();
    expect(screen.getByText(text.invalidEmail)).toBeInTheDocument();
    expect(screen.getByText(text.notice)).toBeInTheDocument();
    expect(close).not.toHaveBeenCalled();
  });

  it('shows the API message when the request fails', async () => {
    const { close, user } = await setup();
    TestBed.inject(FakeBackend).setScenario('users', 'unprocessable');

    await user.type(screen.getByLabelText(new RegExp(`^\\s*${text.name}`)), 'Bruna');
    await user.type(screen.getByLabelText(new RegExp(text.email)), 'bruna@oficina.local');
    await user.click(screen.getByRole('button', { name: text.submit }));

    expect(await screen.findByRole('alert')).toHaveTextContent(messages.errors.businessRule);
    expect(close).not.toHaveBeenCalled();
  });
});
