import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode, testEnvironment } from '../../../../testing/api';
import { APP_ENVIRONMENT } from '../../../core/config/app-environment';
import { ToastService } from '../../../shared/ui/toast/toast';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import {
  expireCharges,
  PORTAL_DONE_TOKEN,
  PORTAL_EXPIRED_TOKEN,
  PORTAL_TOKEN,
  PROVIDER_CHECKOUT,
} from '../../../../testing/fake-api/portal/portal.fake-api';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { provideDocumentsData } from '../../documents/data/documents.gateway';
import { HttpPortalGateway, PortalGateway, providePortalData } from '../data/portal.gateway';
import { PORTAL_POLL_MS } from '../state/portal.store';
import { isSafeRedirect, PortalPage } from './portal-page';

const text = messages.portal;

async function show(
  token: string,
  options: { mode?: TestApiMode; payment?: string; providers?: readonly string[] } = {},
) {
  const view = await render(PortalPage, {
    providers: [
      ...provideTestApi(options.mode ?? 'mock', [{ path: '**', children: [] }]),
      providePortalData(),
      provideDocumentsData(),
      { provide: PORTAL_POLL_MS, useValue: 5 },
      ...(options.providers
        ? [
            {
              provide: APP_ENVIRONMENT,
              useValue: { ...testEnvironment('http'), paymentProviderOrigins: options.providers },
            },
          ]
        : []),
    ],
    inputs: { token, ...(options.payment ? { payment: options.payment } : {}) },
  });
  return { view, user: userEvent.setup() };
}

function digits() {
  return screen.getByRole('textbox', { name: new RegExp(text.digits) });
}

async function enter(user: ReturnType<typeof userEvent.setup>, value = '321'): Promise<void> {
  await screen.findByRole('heading', { name: text.confirmTitle });
  await user.clear(digits());
  await user.type(digits(), value);
  await user.click(screen.getByRole('button', { name: text.confirm }));
}

function order(number: number) {
  const found = TestBed.inject(FakeBackend).state.database.serviceOrders.orders.find(
    (item) => item.number === number,
  );
  if (!found) {
    throw new Error('Missing order');
  }

  return found;
}

describe('PortalPage', () => {
  it.each(['invalid-token', PORTAL_EXPIRED_TOKEN])(
    'shows the same neutral screen for the link %s',
    async (token) => {
      await show(token);
      const screenText = await screen.findByTestId('portal-unavailable');
      expect(screenText.textContent?.replace(/\s+/g, ' ').trim()).toBe(
        `${text.unavailableTitle}${text.unavailable}`,
      );
    },
  );

  it('locks the link after three wrong confirmations', async () => {
    const { user } = await show(PORTAL_TOKEN);

    await enter(user, '12');
    expect(screen.getByText(text.digitsInvalid)).toBeVisible();
    await enter(user, '000');
    expect(await screen.findByText(text.wrongDigits(2))).toBeVisible();
    await enter(user, '111');
    expect(await screen.findByText(text.wrongDigits(1))).toBeVisible();
    await enter(user, '222');
    expect(await screen.findByTestId('portal-locked')).toBeVisible();
  });

  it('keeps the confirmation open and explains the wait when the portal is throttled', async () => {
    const { user } = await show(PORTAL_TOKEN);
    const throttled = {
      kind: 'api-error' as const,
      status: 429,
      code: 'common.tooManyRequests',
      title: 'Too Many Requests',
      detail: messages.errors.tooManyRequests,
      traceId: null,
      fieldErrors: [],
      retryAfterSeconds: 60,
    };
    vi.spyOn(TestBed.inject(PortalGateway), 'confirm')
      .mockReturnValueOnce(throwError(() => throttled))
      .mockReturnValueOnce(
        throwError(() => ({ ...throttled, status: 409, code: 'portal.tryAgain' })),
      );

    await enter(user, '000');
    expect(await screen.findByText(messages.errors.tooManyRequests)).toBeVisible();
    expect(screen.getByRole('heading', { name: text.confirmTitle })).toBeVisible();
    await enter(user, '000');
    expect(await screen.findByText(messages.errors.tooManyRequests)).toBeVisible();
    expect(screen.queryByTestId('portal-locked')).toBeNull();
  });

  it('shows the timeline, photos and quote and approves the marked items', async () => {
    const { user, view } = await show(PORTAL_TOKEN);
    await enter(user);

    expect(await screen.findByRole('heading', { name: text.hello('Joana') })).toBeVisible();
    const current = screen.getByText(text.steps.WaitingApproval).closest('li');
    expect(current).toHaveAttribute('aria-current', 'step');
    expect(await screen.findByAltText('Pastilha gasta')).toBeVisible();
    await expectNoAxeViolations(view.container);

    const total = screen.getByTestId('portal-total');
    const before = total.textContent;
    await user.click(screen.getByRole('checkbox', { name: /Alinhamento 3D/ }));
    expect(total.textContent).not.toBe(before);
    for (const box of screen.getAllByRole('checkbox')) {
      if ((box as HTMLInputElement).checked) {
        await user.click(box);
      }
    }

    await user.click(screen.getByRole('button', { name: text.approve }));
    expect(screen.getByTestId('portal-notice')).toHaveTextContent(text.approveNone);
    await user.click(screen.getByRole('checkbox', { name: /Troca de pastilhas/ }));
    await user.click(screen.getByRole('button', { name: text.approve }));
    expect(await screen.findByText(text.approved)).toBeVisible();
    expect(screen.getByText(text.answered.Approved)).toBeVisible();
    expect(order(3324).status).toBe('InProgress');
    expect(order(3324).quoteItems.map((item) => item.name)).toEqual([
      'Troca de pastilhas e discos',
    ]);
  });

  it('rejects with a mandatory reason', async () => {
    const { user } = await show(PORTAL_TOKEN);
    await enter(user);

    await user.click(await screen.findByRole('button', { name: text.reject }));
    await user.click(screen.getByRole('button', { name: text.rejectSubmit }));
    expect(screen.getByText(text.rejectRequired)).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.cancel }));
    await user.click(screen.getByRole('button', { name: text.reject }));
    await user.type(
      screen.getByRole('textbox', { name: new RegExp(text.rejectReason) }),
      'Vou fazer depois',
    );
    await user.click(screen.getByRole('button', { name: text.rejectSubmit }));
    expect(await screen.findByText(text.rejected)).toBeVisible();
    expect(order(3324).history.at(-1)?.note).toBe('Vou fazer depois');
  });

  it('tells the customer when the quote was already answered elsewhere', async () => {
    const { user } = await show(PORTAL_TOKEN);
    await enter(user);
    await screen.findByRole('heading', { name: text.hello('Joana') });

    order(3324).quoteStatus = 'Approved';
    order(3324).status = 'InProgress';
    await user.click(screen.getByRole('button', { name: text.approve }));
    expect(await screen.findByText(text.alreadyAnswered)).toBeVisible();
  });

  it('pays with Pix, copies the code and renews an expired charge', async () => {
    const { user } = await show(PORTAL_TOKEN);
    await enter(user);
    await user.click(await screen.findByRole('button', { name: text.approve }));
    await screen.findByText(text.approved);
    const writeText = vi.spyOn(navigator.clipboard, 'writeText').mockResolvedValue(undefined);

    await user.click(screen.getByRole('button', { name: text.payPix }));
    const pix = await screen.findByTestId('pix');
    const charge = TestBed.inject(FakeBackend).state.database.portal.charges[0];
    if (charge) {
      charge.polls = -50;
    }

    expect(within(pix).getByAltText(text.pixQr)).toBeVisible();
    await user.click(within(pix).getByRole('button', { name: text.pixCopy }));
    expect(writeText).toHaveBeenCalledWith(expect.stringContaining('BR.GOV.BCB.PIX'));
    expireCharges(TestBed.inject(FakeBackend).state);
    expect(await within(pix).findByText(text.pixExpired)).toBeVisible();
    await user.click(within(pix).getByRole('button', { name: text.pixRenew }));
    expect(await screen.findByText(text.pixPaid)).toBeVisible();
    expect(screen.getByText(text.paymentStatus.Paid)).toBeVisible();
  });

  it('redirects card payments and shows a declined card on return', async () => {
    const assign = vi.fn();
    Object.defineProperty(window, 'location', {
      value: { ...window.location, assign, origin: window.location.origin },
      configurable: true,
    });
    const { user } = await show(PORTAL_TOKEN, { payment: 'declined' });
    await enter(user);
    await user.click(await screen.findByRole('button', { name: text.approve }));
    await screen.findByText(text.approved);
    await user.click(screen.getByRole('button', { name: text.payCard }));
    expect(assign).toHaveBeenCalledWith(expect.stringContaining(PROVIDER_CHECKOUT));
  });

  it('refuses to redirect to a payment provider that is not allowed', async () => {
    const assign = vi.fn();
    Object.defineProperty(window, 'location', {
      value: { ...window.location, assign, origin: window.location.origin },
      configurable: true,
    });
    const { user } = await show(PORTAL_TOKEN, { providers: [] });
    await enter(user);
    await user.click(await screen.findByRole('button', { name: text.approve }));
    await screen.findByText(text.approved);
    await user.click(screen.getByRole('button', { name: text.payCard }));
    await vi.waitFor(() =>
      expect(
        TestBed.inject(ToastService)
          .toasts()
          .map((item) => item.message),
      ).toContain(text.providerBlocked),
    );
    expect(assign).not.toHaveBeenCalled();
  });

  it('opens the documents of a finished order', async () => {
    const { user } = await show(PORTAL_DONE_TOKEN);
    await enter(user);

    expect(await screen.findByText(text.paymentStatus.Paid)).toBeVisible();
    await user.click(
      screen.getByRole('button', {
        name: messages.documents.open(messages.documents.kinds.Receipt, 'REC1-3310-M2XT'),
      }),
    );
    const dialog = await screen.findByRole('dialog', { name: messages.documents.preview });
    expect(await within(dialog).findByTestId('verification-code')).toHaveTextContent(
      'REC1-3310-M2XT',
    );
    expect(within(dialog).getByText(messages.documents.noFiscal)).toBeVisible();
    expect(
      within(dialog).queryByRole('button', { name: messages.documents.sendWhatsApp }),
    ).toBeNull();
  });

  it('shows the declined card notice, the construction notice and session expiration', async () => {
    const { user } = await show(PORTAL_TOKEN, { payment: 'declined' });
    await enter(user);
    expect(await screen.findByText(text.cardDeclined)).toBeVisible();

    TestBed.inject(FakeBackend).state.database.portal.sessions.length = 0;
    await user.click(screen.getByRole('button', { name: text.approve }));
    expect(await screen.findByRole('heading', { name: text.confirmTitle })).toBeVisible();
  });

  it('shows the construction notice and server errors', async () => {
    await show(PORTAL_TOKEN, { mode: 'pending' });
    expect(await screen.findByText(messages.pending.portal)).toBeVisible();
  });

  it('retries after a server error', async () => {
    const { user } = await show(PORTAL_TOKEN, { payment: 'returned' });
    TestBed.inject(FakeBackend).setScenario('portal', 'serverError');
    await screen.findByRole('heading', { name: text.confirmTitle });
    await user.type(digits(), '321');
    await user.click(screen.getByRole('button', { name: text.confirm }));
    expect(await screen.findByText(text.loadError)).toBeVisible();
    TestBed.inject(FakeBackend).setScenario('portal', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    await enter(user);
    expect(await screen.findByText(text.cardReturned)).toBeVisible();
  });

  it('accepts only https payment redirects', () => {
    const allowed = [new URL(PROVIDER_CHECKOUT).origin];
    expect(isSafeRedirect(`${PROVIDER_CHECKOUT}1`, allowed)).toBe(true);
    expect(isSafeRedirect(`${PROVIDER_CHECKOUT}1`, [])).toBe(false);
    expect(isSafeRedirect('https://evil.example/checkout', allowed)).toBe(false);
    expect(isSafeRedirect('javascript:alert(1)', allowed)).toBe(false);
    expect(isSafeRedirect('http://pagamento.provedor.example/x', allowed)).toBe(false);
    expect(isSafeRedirect('not a url', allowed)).toBe(false);
  });

  it('locks the link when no attempts are left and treats a gone link as neutral', async () => {
    const { user } = await show(PORTAL_TOKEN);
    vi.spyOn(TestBed.inject(PortalGateway), 'confirm').mockReturnValueOnce(
      of({ kind: 'wrongDigits', attemptsLeft: 0 }),
    );
    await enter(user, '999');
    expect(await screen.findByTestId('portal-locked')).toBeTruthy();

    TestBed.resetTestingModule();
    const gone = vi.fn(() =>
      throwError(() => ({
        kind: 'api-error',
        status: 410,
        code: 'gone',
        title: '',
        detail: '',
        traceId: null,
        fieldErrors: [],
        retryAfterSeconds: null,
      })),
    );
    const spy = vi.spyOn(HttpPortalGateway.prototype, 'access').mockImplementation(gone);
    await show(PORTAL_TOKEN);
    expect(await screen.findByTestId('portal-unavailable')).toBeTruthy();
    spy.mockRestore();
  });
});
