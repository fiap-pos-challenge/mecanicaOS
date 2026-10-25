import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import { signInWithFakeApi } from '../testing/auth';
import { provideFakeBreakpoints } from '../testing/layout';
import { BackendRole } from './core/auth/permissions';
import { App } from './app';
import { appConfig } from './app.config';
import { routes } from './app.routes';
import { messages, pageTitle } from './core/i18n/messages.pt-BR';
import { AuthStore } from './features/auth/state/auth.store';
import { SCREENS } from './layout/screens';

async function renderAt(url: string, role: BackendRole | null = 'Admin'): Promise<void> {
  await render(App, {
    providers: [...appConfig.providers, provideFakeBreakpoints(false).provider],
  });
  if (role) {
    signInWithFakeApi(role);
  }

  await TestBed.inject(Router).navigateByUrl(url);
}

describe('App routes', () => {
  it('redirects the root to the dashboard inside the shell', async () => {
    await renderAt('/');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.screens.dashboard }),
    ).toBeInTheDocument();
    expect(TestBed.inject(Router).url).toBe('/dashboard');
    expect(TestBed.inject(Title).getTitle()).toBe(pageTitle(messages.screens.dashboard));
    expect(
      screen.getByRole('navigation', { name: messages.shell.mainNavigation }),
    ).toBeInTheDocument();
  });

  it('opens every screen of the plan with its own title', async () => {
    await renderAt('/');
    const router = TestBed.inject(Router);
    const title = TestBed.inject(Title);

    for (const item of SCREENS) {
      await router.navigateByUrl(`/${item.path}`);
      expect(
        await screen.findByRole('heading', { level: 1, name: item.label }),
      ).toBeInTheDocument();
      expect(title.getTitle()).toBe(pageTitle(item.label));
    }
  }, 40_000);

  it('renders the customer portal outside the shell', async () => {
    await renderAt('/portal');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.screens.portal }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('navigation', { name: messages.shell.mainNavigation }),
    ).not.toBeInTheDocument();
  });

  it('shows the not found page for unknown paths', async () => {
    await renderAt('/rota-que-nao-existe');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.pages.notFound.title }),
    ).toBeInTheDocument();
  });

  it('exposes the component catalog in development builds', async () => {
    await renderAt('/dev/ui');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.screens.uiCatalog }),
    ).toBeInTheDocument();
  });

  it('sends visitors without session to the login and remembers the address', async () => {
    await renderAt('/customers', null);

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.auth.login.title }),
    ).toBeInTheDocument();
    expect(TestBed.inject(Router).url).toBe('/login');
    expect(TestBed.inject(AuthStore).returnUrl()).toBe('/customers');
  });

  it('starts the mechanic in the mechanic mode with a reduced menu', async () => {
    await renderAt('/', 'Mechanic');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.screens.mechanic }),
    ).toBeInTheDocument();
    const nav = screen.getByRole('navigation', { name: messages.shell.mainNavigation });
    expect(within(nav).queryByRole('link', { name: messages.screens.customers })).toBeNull();
    expect(within(nav).queryByRole('link', { name: messages.screens.finance })).toBeNull();
    expect(within(nav).getByRole('link', { name: messages.screens.inventory })).toBeInTheDocument();
  });

  it('shows the access denied screen for a forbidden address', async () => {
    await renderAt('/finance', 'Attendant');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.pages.forbidden.title }),
    ).toBeInTheDocument();
    expect(TestBed.inject(Router).url).toBe('/forbidden');
  });

  it('sends a signed-in user away from the login screen', async () => {
    await renderAt('/login', 'Attendant');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.screens.dashboard }),
    ).toBeInTheDocument();
  });

  it('keeps the two-factor screen closed without a pending challenge', async () => {
    await renderAt('/login/verify', null);

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.auth.login.title }),
    ).toBeInTheDocument();
  });

  it('opens the new customer form as a creation, not an edition', async () => {
    await renderAt('/customers/new');

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.customers.form.newTitle }),
    ).toBeInTheDocument();
  });

  it('declares every screen exactly once', () => {
    const paths = SCREENS.map((item) => item.path);
    expect(new Set(paths).size).toBe(paths.length);
    expect(routes.length).toBeGreaterThan(1);
  });
});
