import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen } from '@testing-library/angular';
import { provideTestApi, testEnvironment } from '../../../testing/api';
import { signInAs } from '../../../testing/auth';
import { APP_ENVIRONMENT } from '../../core/config/app-environment';
import { messages } from '../../core/i18n/messages.pt-BR';
import { provideAuthData } from '../../features/auth/data/auth.providers';
import { AuthStore } from '../../features/auth/state/auth.store';
import { SessionWatch } from './session-watch';

const text = messages.auth.inactivity;

describe('SessionWatch', () => {
  afterEach(() => vi.useRealTimers());

  async function setup() {
    vi.useFakeTimers();
    const view = await render(SessionWatch, {
      providers: [
        ...provideTestApi(),
        provideAuthData(),
        {
          provide: APP_ENVIRONMENT,
          useValue: {
            ...testEnvironment(),
            session: { idleWarningMs: 1000, idleLogoutMs: 3000, versionCheckIntervalMs: 60_000 },
          },
        },
      ],
    });
    signInAs('Admin');
    const logout = vi
      .spyOn(TestBed.inject(AuthStore), 'logout')
      .mockImplementation(() => undefined);
    return { view, logout };
  }

  async function advance(
    ms: number,
    view: Awaited<ReturnType<typeof setup>>['view'],
  ): Promise<void> {
    await vi.advanceTimersByTimeAsync(ms);
    view.fixture.detectChanges();
  }

  it('warns before the logout and keeps the session when asked', async () => {
    const { view, logout } = await setup();

    await advance(1000, view);
    expect(screen.getByRole('dialog', { name: text.title })).toHaveTextContent(text.body('0:02'));
    fireEvent.click(screen.getByRole('button', { name: text.stay }));
    await advance(500, view);

    expect(logout).not.toHaveBeenCalled();
    expect(screen.queryByRole('dialog')).toBeNull();
  });

  it('logs out at once when the user chooses to leave', async () => {
    const { view, logout } = await setup();

    await advance(1000, view);
    fireEvent.click(screen.getByRole('button', { name: text.leave }));

    expect(logout).toHaveBeenCalledWith();
  });

  it('logs out for inactivity when the time is over', async () => {
    const { view, logout } = await setup();

    await advance(3000, view);

    expect(logout).toHaveBeenCalledWith('inactivity');
    expect(screen.queryByRole('dialog')).toBeNull();
  });
});
