import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../testing/api';
import { signInAs, TEST_USERS } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { provideFakeBreakpoints } from '../../../testing/layout';
import { messages } from '../../core/i18n/messages.pt-BR';
import { provideRealtime } from '../../core/realtime/realtime.providers';
import { provideAuthData } from '../../features/auth/data/auth.providers';
import { AuthStore } from '../../features/auth/state/auth.store';
import { provideNotificationData } from '../../features/notifications/data/notification.gateway';
import { NotificationsStore } from '../../features/notifications/state/notifications.store';
import { AppTopbar } from './topbar';

const text = messages.auth.user;

describe('AppTopbar account menu', () => {
  async function setup() {
    const view = await render(AppTopbar, {
      providers: [
        ...provideTestApi(),
        provideAuthData(),
        provideFakeBreakpoints(false).provider,
        provideNotificationData(),
        NotificationsStore,
        provideRealtime(),
      ],
    });
    signInAs('Attendant');
    view.fixture.detectChanges();
    const logout = vi
      .spyOn(TestBed.inject(AuthStore), 'logout')
      .mockImplementation(() => undefined);
    return { view, logout, user: userEvent.setup() };
  }

  it('shows who is signed in with the profile and logs out', async () => {
    const { view, logout, user } = await setup();
    const name = TEST_USERS.Attendant.name;

    await user.click(screen.getByRole('button', { name: text.menu(name) }));

    const panel = screen.getByRole('region', { name: text.menu(name) });
    expect(panel).toHaveTextContent(name);
    expect(panel).toHaveTextContent(messages.roles.Attendant);
    await expectNoAxeViolations(view.container);
    await user.click(screen.getByRole('button', { name: text.logout }));
    expect(logout).toHaveBeenCalled();
    expect(screen.queryByRole('region', { name: text.menu(name) })).toBeNull();
  });

  it('closes the menu with Escape and returns the focus', async () => {
    const { user } = await setup();
    const button = screen.getByRole('button', { name: text.menu(TEST_USERS.Attendant.name) });

    await user.click(button);
    await user.keyboard('{Escape}');

    expect(button).toHaveAttribute('aria-expanded', 'false');
    expect(button).toHaveFocus();
  });

  it('closes one menu when the other opens', async () => {
    const { user } = await setup();

    await user.click(screen.getByRole('button', { name: text.menu(TEST_USERS.Attendant.name) }));
    await user.click(screen.getByRole('button', { name: messages.shell.notifications }));

    expect(screen.queryByRole('region', { name: text.menu(TEST_USERS.Attendant.name) })).toBeNull();
  });
});
