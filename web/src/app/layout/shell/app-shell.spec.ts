import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { MemoryStorage, provideFakeBreakpoints } from '../../../testing/layout';
import { messages } from '../../core/i18n/messages.pt-BR';
import { PREFERENCE_STORAGE, PREFERENCE_STORAGE_KEYS } from '../../core/preferences/ui-preferences';
import { ThemeService } from '../../core/theme/theme.service';
import { App } from '../../app';
import { appConfig } from '../../app.config';

const text = messages.shell;

async function setup(mobile: boolean, storage = new MemoryStorage()) {
  const breakpoints = provideFakeBreakpoints(mobile);
  const view = await render(App, {
    providers: [
      ...appConfig.providers,
      breakpoints.provider,
      { provide: PREFERENCE_STORAGE, useValue: storage },
    ],
  });
  signInWithFakeApi('Admin');
  await TestBed.inject(Router).navigateByUrl('/dashboard');
  view.fixture.detectChanges();
  return { view, storage, breakpoints };
}

describe('AppShell on desktop', () => {
  afterEach(() => document.documentElement.removeAttribute('data-theme'));

  it('shows every menu group and marks the active screen', async () => {
    const { view } = await setup(false);

    const nav = screen.getByRole('navigation', { name: text.mainNavigation });
    for (const group of Object.values(messages.navGroups)) {
      expect(within(nav).getByRole('list', { name: group })).toBeInTheDocument();
    }
    expect(within(nav).getByRole('link', { name: messages.screens.dashboard })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(within(nav).getByRole('link', { name: messages.screens.finance })).toHaveAttribute(
      'href',
      '/finance',
    );
    expect(
      within(nav).queryByRole('link', { name: messages.screens.platform }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('navigation', { name: messages.bottomNav.label }),
    ).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: text.skipToContent })).toHaveAttribute(
      'href',
      '#main-content',
    );
    await expectNoAxeViolations(view.container);
  });

  it('collapses the menu to icons and remembers it', async () => {
    const { storage } = await setup(false);
    const collapse = screen.getByRole('button', { name: text.collapseMenu });

    expect(collapse).toHaveAttribute('aria-expanded', 'true');
    await userEvent.click(collapse);

    const expand = screen.getByRole('button', { name: text.expandMenu });
    expect(expand).toHaveAttribute('aria-expanded', 'false');
    expect(screen.getByRole('link', { name: messages.screens.catalog })).toHaveAttribute(
      'title',
      messages.screens.catalog,
    );
    expect(screen.queryByRole('link', { name: text.home })).not.toBeInTheDocument();
    expect(storage.getItem(PREFERENCE_STORAGE_KEYS.sidebar)).toBe('collapsed');
  });

  it('focuses the search with Ctrl K', async () => {
    await setup(false);

    await userEvent.keyboard('{Control>}k{/Control}');

    expect(screen.getByRole('searchbox', { name: text.searchLabel })).toHaveFocus();
  });

  it('opens and closes the notifications panel', async () => {
    await setup(false);
    const bell = screen.getByRole('button', { name: text.notifications });

    await userEvent.click(bell);
    expect(bell).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('heading', { name: messages.notifications.title })).toBeInTheDocument();

    await userEvent.keyboard('{Escape}');
    expect(bell).toHaveAttribute('aria-expanded', 'false');
    expect(bell).toHaveFocus();

    await userEvent.click(bell);
    await userEvent.click(bell);
    expect(
      screen.queryByRole('heading', { name: messages.notifications.title }),
    ).not.toBeInTheDocument();
  });

  it('cycles the theme from the top bar', async () => {
    await setup(false);
    const theme = TestBed.inject(ThemeService);

    await userEvent.click(
      screen.getByRole('button', { name: text.theme.change(text.theme.system, text.theme.light) }),
    );

    expect(theme.mode()).toBe('light');
    expect(
      screen.getByRole('button', { name: text.theme.change(text.theme.light, text.theme.dark) }),
    ).toBeInTheDocument();
  });

  it('ignores Escape when the drawer is not open', async () => {
    await setup(false);

    await userEvent.keyboard('{Escape}');

    expect(screen.getByRole('navigation', { name: text.mainNavigation })).toBeInTheDocument();
  });
});

describe('AppShell on mobile', () => {
  it('hides the menu until the drawer opens and shows the bottom bar', async () => {
    const { view } = await setup(true);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    const bottom = screen.getByRole('navigation', { name: messages.bottomNav.label });
    expect(within(bottom).getByRole('link', { name: messages.bottomNav.quote })).toHaveAttribute(
      'href',
      '/quotes',
    );
    expect(
      within(bottom).getByRole('link', { name: messages.bottomNav.dashboard }),
    ).toHaveAttribute('aria-current', 'page');
    await expectNoAxeViolations(view.container);
  });

  it('opens the drawer from the top bar and closes it with the close button', async () => {
    const { view } = await setup(true);

    const open = screen.getByRole('button', { name: text.openMenu });
    await userEvent.click(open);
    const drawer = screen.getByRole('dialog', { name: text.mainNavigation });
    expect(open).toHaveAttribute('aria-expanded', 'true');
    await expectNoAxeViolations(view.container);

    await userEvent.click(within(drawer).getByRole('button', { name: text.closeMenu }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('closes the drawer with Escape, with the backdrop and after navigating', async () => {
    const { view } = await setup(true);
    const menu = within(
      screen.getByRole('navigation', { name: messages.bottomNav.label }),
    ).getByRole('button', {
      name: messages.bottomNav.menu,
    });

    await userEvent.click(menu);
    await userEvent.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await userEvent.click(menu);
    await userEvent.click(screen.getByTestId('drawer-backdrop'));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await userEvent.click(menu);
    await userEvent.click(
      within(screen.getByRole('dialog')).getByRole('link', { name: messages.screens.catalog }),
    );
    await view.fixture.whenStable();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(TestBed.inject(Router).url).toBe('/catalog/services');
  });
});
