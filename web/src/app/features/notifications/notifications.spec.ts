import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Subject } from 'rxjs';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { pushFakeNotification } from '../../../testing/fake-api/notifications/notifications.fake-api';
import { messages } from '../../core/i18n/messages.pt-BR';
import { RealtimeEvent, RealtimeGateway } from '../../core/realtime/realtime.gateway';
import { ToastService } from '../../shared/ui/toast/toast';
import { provideAuthData } from '../auth/data/auth.providers';
import { NotificationCenter } from './components/notification-center';
import { provideNotificationData } from './data/notification.gateway';
import { normalizePreferences, safeLink, toNotification } from './data/notification.models';
import { NOTIFICATION_POLL_MS, NotificationsStore } from './state/notifications.store';

const text = messages.notifications;

class FakeRealtime extends RealtimeGateway {
  readonly subject = new Subject<RealtimeEvent>();
  readonly status = signal('disconnected' as const);
  readonly events$ = this.subject.asObservable();
  connect = () => Promise.resolve();
  disconnect = () => Promise.resolve();
}

describe('notification contract', () => {
  it('keeps only internal links', () => {
    expect(safeLink('/inventory')).toBe('/inventory');
    expect(safeLink('//evil.example')).toBeNull();
    expect(safeLink('https://evil.example')).toBeNull();
    expect(safeLink('/\\evil')).toBeNull();
    expect(safeLink(null)).toBeNull();
  });

  it('maps notifications and fills every preference type', () => {
    expect(
      toNotification({ id: 'n', type: 'StockLow', title: 't', createdAt: '2026-10-16T00:00:00Z' }),
    ).toEqual({
      id: 'n',
      type: 'StockLow',
      title: 't',
      body: '',
      createdAt: '2026-10-16T00:00:00Z',
      read: false,
      link: null,
      aiSuggested: false,
    });
    const preferences = normalizePreferences([{ type: 'StockLow', email: true }]);
    expect(preferences).toHaveLength(7);
    expect(preferences.find((item) => item.type === 'StockLow')).toEqual({
      type: 'StockLow',
      inApp: true,
      email: true,
      whatsApp: false,
    });
  });
});

async function show(options: { mode?: TestApiMode; poll?: number } = {}) {
  const realtime = new FakeRealtime();
  const view = await render(NotificationCenter, {
    providers: [
      ...provideTestApi(options.mode ?? 'mock', [{ path: '**', children: [] }]),
      provideAuthData(),
      provideNotificationData(),
      NotificationsStore,
      { provide: RealtimeGateway, useValue: realtime },
      { provide: NOTIFICATION_POLL_MS, useValue: options.poll ?? 0 },
    ],
    configureTestBed: () => {
      signInWithFakeApi('Admin');
    },
  });
  const store = TestBed.inject(NotificationsStore);
  await store.load();
  view.fixture.detectChanges();
  return { view, store, realtime, user: userEvent.setup() };
}

describe('NotificationCenter', () => {
  it('lists notifications, marks them read and opens the link', async () => {
    const { view, store, user } = await show();
    const list = await screen.findByRole('list', { name: text.label });
    expect(within(list).getAllByRole('listitem')).toHaveLength(3);
    expect(store.unread()).toBe(3);
    expect(within(list).getAllByText(messages.ui.ai.badge)).toHaveLength(1);

    await user.click(
      screen.getByRole('button', { name: text.markRead('Falha ao enviar WhatsApp da OS 3318') }),
    );
    expect(store.unread()).toBe(2);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    let navigated = false;
    view.fixture.componentInstance.navigated.subscribe(() => (navigated = true));
    await user.click(screen.getByRole('button', { name: /^Não lida: Reposição sugerida/ }));
    expect(navigate).toHaveBeenCalledWith('/inventory');
    expect(navigated).toBe(true);

    await user.click(screen.getByRole('button', { name: text.markAll }));
    expect(store.unread()).toBe(0);
    await vi.waitFor(() =>
      expect(
        Object.values(TestBed.inject(FakeBackend).state.database.notifications.readBy)[0],
      ).toHaveLength(3),
    );
    await expectNoAxeViolations(view.container);
  });

  it('receives notifications in real time and by polling, and announces them', async () => {
    const { store, realtime } = await show({ poll: 20 });
    realtime.subject.next({
      type: 'notification',
      notification: {
        id: 'live',
        type: 'ExportReady',
        title: 'Exportação pronta',
        createdAt: '2026-10-16T10:00:00Z',
        link: 'https://evil.example',
      },
      occurredAt: '2026-10-16T10:00:00Z',
    });
    expect(store.items()[0]).toMatchObject({ id: 'live', link: null });
    expect(store.announcement()).toBe(text.live('Exportação pronta'));
    realtime.subject.next({
      type: 'notification',
      notification: { id: 'broken' },
      occurredAt: '2026-10-16T10:00:00Z',
    });
    realtime.subject.next({ type: 'exportReady', exportId: 'x', occurredAt: '' });
    expect(store.items().some((item) => item.id === 'broken')).toBe(false);

    pushFakeNotification(TestBed.inject(FakeBackend).state, {
      type: 'QuoteAnswered',
      title: 'Orçamento aprovado pelo cliente',
      body: '',
      link: '/quotes',
      aiSuggested: false,
    });
    await vi.waitFor(() =>
      expect(store.announcement()).toBe(text.live('Orçamento aprovado pelo cliente')),
    );
  });

  it('saves the preferences by type and channel', async () => {
    const { user } = await show();
    await user.click(screen.getByRole('button', { name: text.preferences }));
    const dialog = await screen.findByRole('dialog');
    const box = await within(dialog).findByRole('checkbox', {
      name: text.toggle(text.types.StockLow, text.channels.whatsApp),
    });
    await user.click(box);
    await user.click(within(dialog).getByRole('button', { name: text.save }));
    await vi.waitFor(() =>
      expect(
        TestBed.inject(ToastService)
          .toasts()
          .map((item) => item.message),
      ).toContain(text.saved),
    );
    const saved = Object.values(
      TestBed.inject(FakeBackend).state.database.notifications.preferences,
    )[0];
    expect(saved?.find((item) => item.type === 'StockLow')?.whatsApp).toBe(true);

    TestBed.inject(FakeBackend).setScenario('notifications', 'serverError');
    await user.click(screen.getByRole('button', { name: text.preferences }));
    const failing = await screen.findByRole('dialog');
    expect(await within(failing).findByRole('alert')).toBeTruthy();
    await user.click(within(failing).getByRole('button', { name: text.cancel }));
  });

  it('shows failures and the pending API', async () => {
    const { store } = await show();
    TestBed.inject(FakeBackend).setScenario('notifications', 'serverError');
    await store.markRead('notification-stock');
    await store.markAllRead();
    expect((await store.savePreferences([])).ok).toBe(false);
    await store.load();
    expect(await screen.findByText(text.loadError)).toBeTruthy();

    TestBed.resetTestingModule();
    await show({ mode: 'pending' });
    expect(await screen.findByText(messages.pending.notifications)).toBeTruthy();
  });
});
